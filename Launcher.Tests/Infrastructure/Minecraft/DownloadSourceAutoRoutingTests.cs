/*
 * MCL Launcher
 * Copyright (C) 2026 Quan Zhou
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Launcher.Domain.Models;
using Launcher.Infrastructure.Minecraft;

namespace Launcher.Tests.Infrastructure.Minecraft;

/// <summary>
/// 自动模式按 PCL 的默认顺序优先使用镜像，失败后回退原始官方源。
/// </summary>
public sealed class DownloadSourceAutoRoutingTests
{
    [Theory]
    [InlineData("https://piston-meta.mojang.com/mc/game/version_manifest_v2.json")]
    [InlineData("https://piston-meta.mojang.com/v1/packages/test/1.21.8.json")]
    [InlineData("https://libraries.minecraft.net/com/example/lib.jar")]
    [InlineData("https://meta.fabricmc.net/v2/versions/loader/1.20.1")]
    [InlineData("https://meta.fabricmc.net/v2/versions/loader/1.20.2/0.19.3/profile/json")]
    [InlineData("https://files.minecraftforge.net/net/minecraftforge/forge/index_1.20.1.html")]
    [InlineData("https://maven.neoforged.net/releases/net/neoforged/neoforge/maven-metadata.xml")]
    public void AutoPrefersPclMirrorForKnownMinecraftSources(string url)
    {
        var requests = MinecraftDownloadSourceResolver
            .EnumerateRequests(url, DownloadSourcePreference.Auto)
            .ToList();

        Assert.Equal("bmclapi2.bangbang93.com", new Uri(requests[0].ActualUrl).Host);
        Assert.Equal(url, requests[^1].ActualUrl);
    }

    [Theory]
    [InlineData("https://repo.maven.apache.org/maven2/com/example/library/1.0/library-1.0.jar")]
    public void AutoLeavesUnsupportedSourcesOnTheirOriginalHost(string url)
    {
        var first = MinecraftDownloadSourceResolver
            .EnumerateRequests(url, DownloadSourcePreference.Auto)
            .First();

        Assert.NotEqual("bmclapi2.bangbang93.com", new Uri(first.ActualUrl).Host);
        Assert.Equal(new Uri(url).Host, new Uri(first.ActualUrl).Host);
    }

    [Fact]
    public void OfficialOnlyYieldsSingleOfficialRequest()
    {
        var requests = MinecraftDownloadSourceResolver
            .EnumerateRequests("https://libraries.minecraft.net/com/example/lib.jar", DownloadSourcePreference.OfficialOnly)
            .ToList();

        Assert.Single(requests);
        Assert.Equal("libraries.minecraft.net", new Uri(requests[0].ActualUrl).Host);
    }
}
