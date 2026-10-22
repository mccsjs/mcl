/*
 * BlockHelm Launcher
 * Copyright (C) 2026 Quan Zhou
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Launcher.Application.Services.Multiplayer;

/// <summary>红石联机节点。</summary>
public sealed record RedstoneNode(string Name, string Address);

/// <summary>已建立的红石隧道会话。</summary>
public sealed record RedstoneSession(string PublicAddress, int GamePort, string NodeAddress);

/// <summary>
/// 红石联机：按需下载官方客户端二进制，把本地游戏端口挂到中转节点上。
/// </summary>
public interface IRedstoneOnlineService
{
    Task<IReadOnlyList<RedstoneNode>> GetNodesAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default);

    Task<RedstoneSession> StartAsync(
        int gamePort,
        RedstoneNode node,
        CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
}
