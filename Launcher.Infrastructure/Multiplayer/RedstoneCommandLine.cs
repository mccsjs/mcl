/*
 * BlockHelm Launcher
 * Copyright (C) 2026 Quan Zhou
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Launcher.Application.Services.Multiplayer;

namespace Launcher.Infrastructure.Multiplayer;

/// <summary>红石客户端的启动参数。</summary>
internal static class RedstoneCommandLine
{
    internal const int ControlPort = 8080;
    internal const int DataPort = 8000;

    public static IReadOnlyList<string> BuildArguments(RedstoneNode node, int gamePort) =>
    [
        "-t", node.Address,
        "-p", gamePort.ToString(),
        "--game-host", "127.0.0.1",
        "--control-port", ControlPort.ToString(),
        "--data-port", DataPort.ToString()
    ];

    public static ProcessStartInfo CreateStartInfo(string executablePath, RedstoneNode node, int gamePort)
    {
        var startInfo = new ProcessStartInfo(executablePath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(executablePath) ?? AppContext.BaseDirectory
        };

        foreach (var argument in BuildArguments(node, gamePort))
            startInfo.ArgumentList.Add(argument);

        startInfo.Environment["RUST_LOG"] = "info";
        startInfo.Environment["NO_COLOR"] = "1";
        return startInfo;
    }
}
