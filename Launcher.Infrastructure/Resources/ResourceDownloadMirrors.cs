/*
 * BlockHelm Launcher
 * Copyright (C) 2026 Quan Zhou
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, version 3.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <https://www.gnu.org/licenses/>.
 *
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System;

namespace Launcher.Infrastructure.Resources;

/// <summary>
/// 社区资源文件的镜像候选，只做官方 CDN 之后的兜底（下载层会校验大小与哈希）。
/// </summary>
internal static class ResourceDownloadMirrors
{
    private const string McimMirrorHost = "mod.mcimirror.top";

    private static readonly string[] CurseForgeCdnHosts =
    [
        "edge.forgecdn.net",
        "mediafilez.forgecdn.net",
        "media.forgecdn.net"
    ];

    // 按官方地址推导镜像候选，顺序即尝试顺序。
    public static IReadOnlyList<string> EnumerateMirrors(string? officialUrl)
    {
        if (string.IsNullOrWhiteSpace(officialUrl)
            || !Uri.TryCreate(officialUrl, UriKind.Absolute, out var uri))
        {
            return [];
        }

        if (uri.Host.Equals("cdn.modrinth.com", StringComparison.OrdinalIgnoreCase)
            || IsCurseForgeCdnHost(uri.Host))
        {
            // MCIM 直接按原始路径服务，Modrinth 的 /data/... 与 CurseForge 的 /files/... 都适用。
            return [$"https://{McimMirrorHost}{uri.PathAndQuery}"];
        }

        return [];
    }

    private static bool IsCurseForgeCdnHost(string host)
    {
        foreach (var candidate in CurseForgeCdnHosts)
        {
            if (host.Equals(candidate, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
