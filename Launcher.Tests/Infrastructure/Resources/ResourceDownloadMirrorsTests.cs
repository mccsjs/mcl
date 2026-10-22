/*
 * BlockHelm Launcher
 * Copyright (C) 2026 Quan Zhou
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Launcher.Infrastructure.Resources;

namespace Launcher.Tests.Infrastructure.Resources;

/// <summary>
/// 镜像只做兜底，路径规则写错就连续试无效地址，这里按来源逐项锁住。
/// </summary>
public sealed class ResourceDownloadMirrorsTests
{
    [Theory]
    [InlineData(
        "https://cdn.modrinth.com/data/AABBCCDD/versions/1.2.3/sodium-1.2.3.jar",
        "https://mod.mcimirror.top/data/AABBCCDD/versions/1.2.3/sodium-1.2.3.jar")]
    [InlineData(
        "https://edge.forgecdn.net/files/1234/567/example.jar",
        "https://mod.mcimirror.top/files/1234/567/example.jar")]
    [InlineData(
        "https://mediafilez.forgecdn.net/files/1234/567/example.jar",
        "https://mod.mcimirror.top/files/1234/567/example.jar")]
    [InlineData(
        "https://media.forgecdn.net/files/1234/567/example.jar",
        "https://mod.mcimirror.top/files/1234/567/example.jar")]
    public void EnumerateMirrorsRewritesKnownCdnHosts(string officialUrl, string expectedMirror)
    {
        var mirrors = ResourceDownloadMirrors.EnumerateMirrors(officialUrl);

        Assert.Equal(new[] { expectedMirror }, mirrors);
    }

    [Fact]
    public void EnumerateMirrorsKeepsQueryString()
    {
        var mirrors = ResourceDownloadMirrors.EnumerateMirrors(
            "https://cdn.modrinth.com/data/AABBCCDD/versions/1.2.3/file.jar?download=1");

        Assert.Equal(
            new[] { "https://mod.mcimirror.top/data/AABBCCDD/versions/1.2.3/file.jar?download=1" },
            mirrors);
    }

    [Theory]
    [InlineData("https://api.modrinth.com/v2/project/sodium")]
    [InlineData("https://api.curseforge.com/v1/mods/238222")]
    [InlineData("https://bmclapi2.bangbang93.com/maven/example.jar")]
    [InlineData("not a url")]
    [InlineData("")]
    [InlineData(null)]
    public void EnumerateMirrorsIgnoresNonCdnUrls(string? officialUrl)
    {
        Assert.Empty(ResourceDownloadMirrors.EnumerateMirrors(officialUrl));
    }
}
