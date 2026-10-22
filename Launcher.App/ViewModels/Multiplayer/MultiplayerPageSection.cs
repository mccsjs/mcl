/*
 * MCL Launcher
 * Copyright (C) 2026 Quan Zhou
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, version 3.
 *
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Launcher.App.ViewModels.Multiplayer;

public enum MultiplayerPageSection
{
    CreateLobby,
    JoinLobby,
    Redstone
}

/// <summary>顶部胶囊切换的联机方式。</summary>
public enum MultiplayerTransportKind
{
    Terracotta,
    Redstone
}
