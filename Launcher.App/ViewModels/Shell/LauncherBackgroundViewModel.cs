/*
 * MCL Launcher
 * Copyright (C) 2026 Quan Zhou
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Launcher.App.ViewModels.Shell;

/// <summary>固定内嵌的启动器背景图，不再提供切换或自定义。</summary>
public sealed partial class LauncherBackgroundViewModel : ObservableObject
{
    private const string EmbeddedBackgroundSource =
        "pack://application:,,,/mccsjs_launcher;component/Assets/Backgrounds/LauncherBackground.jpg";

    [ObservableProperty]
    private ImageSource? imageSource;

    [ObservableProperty]
    private bool isActive = true;

    public void Activate()
    {
        if (ImageSource is not null)
            return;

        ImageSource = new BitmapImage(new Uri(EmbeddedBackgroundSource, UriKind.Absolute));
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
        ImageSource = null;
    }
}
