/*
 * MCL Launcher
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

namespace Launcher.Domain.Models;

public enum DownloadSourcePreference
{
    /// <summary>自动：已知下载源优先使用 PCL 同款镜像，失败后回退原始源。</summary>
    Auto = 0,

    /// <summary>官方源优先，失败回退镜像。</summary>
    Official = 1,

    /// <summary>镜像源优先，失败回退官方。</summary>
    BmclApi = 2,

    /// <summary>仅官方源，不回退。</summary>
    OfficialOnly = 3
}
