/*
 * MCL Launcher
 * Copyright (C) 2026 Quan Zhou
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Launcher.Application.Services.Multiplayer;
using Launcher.Infrastructure.Minecraft;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Launcher.Infrastructure.Multiplayer;

/// <summary>
/// 红石联机：按需下载官方客户端，把本地游戏端口挂到中转节点，输出可直接分享的公网地址。
/// </summary>
internal sealed class RedstoneOnlineService : IRedstoneOnlineService, IDisposable
{
    internal const string NodeListUrl = "https://hongshi.site/api/server/list";
    internal const string ClientDownloadUrl =
        "https://hongshi.site/api/download/client?platform=windows&arch=amd64";

    private const long MaximumClientBytes = 32L * 1024 * 1024;
    private const string ClientFileName = "redstone.exe";
    private static readonly TimeSpan NodeCacheTtl = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan StartTimeout = TimeSpan.FromSeconds(20);
    private static readonly string[] RequiredHelpFlags =
        ["-t", "--game-port", "--game-host", "--control-port", "--data-port"];

    private readonly HttpClient httpClient;
    private readonly string clientRoot;
    private readonly ILogger<RedstoneOnlineService> logger;
    private readonly SemaphoreSlim gate = new(1, 1);
    private Process? session;
    private IReadOnlyList<RedstoneNode>? cachedNodes;
    private DateTimeOffset cacheExpiresAt;

    public RedstoneOnlineService(
        LauncherPathProvider pathProvider,
        ILogger<RedstoneOnlineService>? logger = null)
        : this(
            LauncherHttpUserAgent.Apply(MinecraftHttpClientFactory.CreateTransportClient()),
            Path.Combine(pathProvider.DefaultDataDirectory, "tools", "redstone"),
            logger)
    {
    }

    internal RedstoneOnlineService(
        HttpClient httpClient,
        string clientRoot,
        ILogger<RedstoneOnlineService>? logger = null)
    {
        this.httpClient = httpClient;
        this.clientRoot = Path.GetFullPath(clientRoot);
        this.logger = logger ?? NullLogger<RedstoneOnlineService>.Instance;
    }

    public async Task<IReadOnlyList<RedstoneNode>> GetNodesAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        if (!forceRefresh && cachedNodes is not null && DateTimeOffset.UtcNow < cacheExpiresAt)
            return cachedNodes;

        using var response = await httpClient.GetAsync(NodeListUrl, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var nodes = RedstoneNodeCatalog.Parse(json);
        if (nodes.Count == 0)
            throw new InvalidDataException("The RedStone node list is empty.");

        cachedNodes = nodes;
        cacheExpiresAt = DateTimeOffset.UtcNow + NodeCacheTtl;
        logger.LogInformation("RedStone nodes loaded. Count={Count}", nodes.Count);
        return nodes;
    }

    public async Task<RedstoneSession> StartAsync(
        int gamePort,
        RedstoneNode node,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (gamePort is <= 0 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(gamePort));

        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            StopCore();
            var executable = await EnsureClientAsync(cancellationToken).ConfigureAwait(false);
            var process = new Process
            {
                StartInfo = RedstoneCommandLine.CreateStartInfo(executable, node, gamePort)
            };
            if (!process.Start())
                throw new InvalidOperationException("The RedStone client did not start.");

            _ = DrainErrorsAsync(process);
            try
            {
                var endpoint = await WaitForEndpointAsync(process, cancellationToken).ConfigureAwait(false);
                session = process;
                logger.LogInformation(
                    "RedStone tunnel opened. Node={Node} GamePort={GamePort} Endpoint={Endpoint}",
                    node.Address,
                    gamePort,
                    endpoint);
                return new RedstoneSession(endpoint, gamePort, node.Address);
            }
            catch
            {
                KillProcess(process);
                throw;
            }
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            StopCore();
        }
        finally
        {
            gate.Release();
        }
    }

    public void Dispose()
    {
        StopCore();
        gate.Dispose();
    }

    private void StopCore()
    {
        var process = session;
        session = null;
        if (process is not null)
            KillProcess(process);
    }

    private static void KillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // 已经退出。
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // 无权结束，留给系统回收。
        }
        finally
        {
            process.Dispose();
        }
    }

    private static async Task DrainErrorsAsync(Process process)
    {
        try
        {
            while (await process.StandardError.ReadLineAsync().ConfigureAwait(false) is not null)
            {
            }
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException or InvalidOperationException)
        {
            // 进程结束时流会关闭。
        }
    }

    private static async Task<string> WaitForEndpointAsync(Process process, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(StartTimeout);
        try
        {
            while (true)
            {
                var line = await process.StandardOutput.ReadLineAsync(timeout.Token).ConfigureAwait(false);
                if (line is null)
                    throw new InvalidOperationException("The RedStone client exited before publishing an endpoint.");

                if (RedstoneEndpoint.TryParse(line, out var endpoint))
                    return endpoint;
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("The RedStone client did not publish an endpoint in time.");
        }
    }

    private async Task<string> EnsureClientAsync(CancellationToken cancellationToken)
    {
        var executable = Path.Combine(clientRoot, ClientFileName);
        if (File.Exists(executable) && await IsCompatibleAsync(executable, cancellationToken).ConfigureAwait(false))
            return executable;

        Directory.CreateDirectory(clientRoot);
        var temporary = Path.Combine(clientRoot, $".{ClientFileName}.{Guid.NewGuid():N}.download");
        try
        {
            using var response = await httpClient.GetAsync(ClientDownloadUrl, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength is <= 0 or > MaximumClientBytes)
                throw new InvalidDataException("The RedStone client package size is invalid.");

            await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
            await using (var destination = new FileStream(
                temporary,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                FileOptions.Asynchronous))
            {
                await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
                if (destination.Length > MaximumClientBytes)
                    throw new InvalidDataException("The RedStone client package is too large.");
            }

            if (!await HasExecutableHeaderAsync(temporary, cancellationToken).ConfigureAwait(false))
                throw new InvalidDataException("The RedStone client package is not a Windows executable.");

            File.Move(temporary, executable, overwrite: true);
            logger.LogInformation("RedStone client downloaded to {Path}", executable);
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }

        if (!await IsCompatibleAsync(executable, cancellationToken).ConfigureAwait(false))
            throw new InvalidDataException("The RedStone client does not support the required options.");

        return executable;
    }

    private static async Task<bool> HasExecutableHeaderAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var header = new byte[2];
        var read = await stream.ReadAsync(header, cancellationToken).ConfigureAwait(false);
        return read == 2 && header[0] == 'M' && header[1] == 'Z';
    }

    private static async Task<bool> IsCompatibleAsync(string executable, CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo(executable)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(executable) ?? AppContext.BaseDirectory
            };
            startInfo.ArgumentList.Add("--print-help");

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("The RedStone client did not start.");

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));
            var help = await process.StandardOutput.ReadToEndAsync(timeout.Token).ConfigureAwait(false);
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            return RequiredHelpFlags.All(flag => help.Contains(flag, StringComparison.Ordinal));
        }
        catch (Exception exception) when (exception is IOException
            or InvalidOperationException
            or OperationCanceledException
            or System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }
}
