/*
 * MCL Launcher
 * Copyright (C) 2026 Quan Zhou
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System;
using System.Text.RegularExpressions;

namespace Launcher.Infrastructure.Multiplayer;

/// <summary>
/// 红石客户端把公网地址以 <c>endpoint=host:port</c> 打在标准输出里。
/// </summary>
internal static partial class RedstoneEndpoint
{
    public static bool TryParse(string? line, out string endpoint)
    {
        endpoint = string.Empty;
        if (string.IsNullOrWhiteSpace(line))
            return false;

        var cleaned = AnsiPattern().Replace(line, string.Empty);
        foreach (var field in cleaned.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            if (!field.StartsWith("endpoint=", StringComparison.Ordinal))
                continue;

            var value = field["endpoint=".Length..];
            if (!IsValid(value))
                return false;

            endpoint = value;
            return true;
        }

        return false;
    }

    private static bool IsValid(string value)
    {
        var separator = value.LastIndexOf(':');
        if (separator <= 0 || separator == value.Length - 1)
            return false;

        var host = value[..separator];
        if (host.Contains('/') || host.Contains('\\') || host.Contains('?') || host.Contains('@'))
            return false;

        return int.TryParse(value[(separator + 1)..], out var port) && port is > 0 and <= 65535;
    }

    [GeneratedRegex(@"\x1B\[[0-9;]*[A-Za-z]")]
    private static partial Regex AnsiPattern();
}
