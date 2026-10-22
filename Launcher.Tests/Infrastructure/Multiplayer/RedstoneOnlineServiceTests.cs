/*
 * MCL Launcher
 * Copyright (C) 2026 Quan Zhou
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Launcher.Application.Services.Multiplayer;
using Launcher.Infrastructure.Multiplayer;

namespace Launcher.Tests.Infrastructure.Multiplayer;

/// <summary>
/// 红石客户端的输出与节点清单格式直接来自线上，解析错了用户就拿不到地址。
/// </summary>
public sealed class RedstoneOnlineServiceTests
{
    // 真实客户端输出（已脱敏 uuid）。
    private const string TunnelLine =
        "2026-10-22T05:20:42.422379Z  INFO hongshi::client: tunnel created, hand the endpoint above to the players uuid=2c72800c-8f28-47de-a075-c90a5354d14a port=44812 endpoint=nj.hongshi.site:44812";

    [Fact]
    public void EndpointIsParsedFromRealClientOutput()
    {
        Assert.True(RedstoneEndpoint.TryParse(TunnelLine, out var endpoint));

        Assert.Equal("nj.hongshi.site:44812", endpoint);
    }

    [Fact]
    public void EndpointIsParsedFromAnsiColouredLine()
    {
        const string line = "\u001B[32mINFO\u001B[0m tunnel created endpoint=cd.hongshi.site:51234";

        Assert.True(RedstoneEndpoint.TryParse(line, out var endpoint));

        Assert.Equal("cd.hongshi.site:51234", endpoint);
    }

    [Theory]
    [InlineData("INFO tunnel created, no endpoint yet")]
    [InlineData("endpoint=")]
    [InlineData("endpoint=nj.hongshi.site")]
    [InlineData("endpoint=nj.hongshi.site:0")]
    [InlineData("endpoint=nj.hongshi.site:70000")]
    [InlineData(null)]
    public void InvalidLinesAreRejected(string? line)
    {
        Assert.False(RedstoneEndpoint.TryParse(line, out _));
    }

    [Fact]
    public void NodesAreParsedFromOfficialList()
    {
        var nodes = RedstoneNodeCatalog.Parse(
            """{"南京":"nj.hongshi.site","成都":"cd.hongshi.site","广州":"gz.hongshi.site"}""");

        Assert.Equal(3, nodes.Count);
        Assert.Contains(nodes, node => node is { Name: "南京", Address: "nj.hongshi.site" });
    }

    [Fact]
    public void InvalidNodeHostsAreSkipped()
    {
        var nodes = RedstoneNodeCatalog.Parse(
            """{"ok":"nj.hongshi.site","bad-path":"http://evil/x","empty":"","bad-space":"a b"}""");

        Assert.Single(nodes);
        Assert.Equal("nj.hongshi.site", nodes[0].Address);
    }

    [Fact]
    public void ArgumentsMatchTheClientContract()
    {
        var arguments = RedstoneCommandLine.BuildArguments(new RedstoneNode("南京", "nj.hongshi.site"), 25565);

        Assert.Equal(
            new[]
            {
                "-t", "nj.hongshi.site",
                "-p", "25565",
                "--game-host", "127.0.0.1",
                "--control-port", "8080",
                "--data-port", "8000"
            },
            arguments);
    }
}
