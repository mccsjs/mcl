/*
 * MCL Launcher
 * Copyright (C) 2026 Quan Zhou
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Launcher.Application.Services.Multiplayer;

namespace Launcher.Infrastructure.Multiplayer;

/// <summary>
/// 官方节点列表是 <c>{"南京":"nj.hongshi.site"}</c> 这种名字到地址的映射。
/// </summary>
internal static class RedstoneNodeCatalog
{
    public static IReadOnlyList<RedstoneNode> Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind is not JsonValueKind.Object)
            throw new InvalidDataException("The RedStone node list is not an object.");

        var nodes = new List<RedstoneNode>();
        foreach (var property in document.RootElement.EnumerateObject())
        {
            var address = property.Value.ValueKind is JsonValueKind.String
                ? property.Value.GetString()?.Trim()
                : null;
            if (string.IsNullOrEmpty(address) || !IsValidHost(address))
                continue;

            nodes.Add(new RedstoneNode(property.Name, address));
        }

        return nodes;
    }

    private static bool IsValidHost(string host) =>
        host.Length <= 253
        && !host.Contains('/')
        && !host.Contains('\\')
        && !host.Contains(':')
        && !host.Contains('?')
        && !host.Contains('@')
        && !host.Any(char.IsWhiteSpace);
}
