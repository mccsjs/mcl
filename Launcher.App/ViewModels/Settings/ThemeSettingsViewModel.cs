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

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launcher.App.Resources;
using Launcher.App.Services;
using Launcher.Domain.Models;

namespace Launcher.App.ViewModels.Settings;

public sealed partial class ThemeSettingsViewModel : SettingsSectionViewModelBase
{
    private readonly IThemeService themeService;

    internal ThemeSettingsViewModel(
        SettingsPersistenceCoordinator persistence,
        IThemeService themeService)
        : base(persistence)
    {
        this.themeService = themeService;
        ThemeOptions =
        [
            new(LauncherDefaults.DefaultTheme, Strings.Settings_ThemeDarkTitle),
            new("Light", Strings.Settings_ThemeLightTitle)
        ];
        selectedThemeOption = ThemeOptions[0];
    }

    public ObservableCollection<SettingsThemeOption> ThemeOptions { get; }

    [ObservableProperty] private SettingsThemeOption? selectedThemeOption;
    [ObservableProperty] private bool followSystemTheme = true;

    public bool IsThemeSelectionVisible => !FollowSystemTheme;
    public void Load(LauncherSettings settings)
    {
        LoadState(() =>
        {
            FollowSystemTheme = settings.ThemeFollowSystem;
            SelectedThemeOption = ThemeOptions.FirstOrDefault(option =>
                string.Equals(option.Id, settings.Theme, StringComparison.OrdinalIgnoreCase)) ?? ThemeOptions[0];
        });
    }

    partial void OnFollowSystemThemeChanged(bool value)
    {
        OnPropertyChanged(nameof(IsThemeSelectionVisible));
        ApplyPreferenceAndPersist();
    }

    partial void OnSelectedThemeOptionChanged(SettingsThemeOption? oldValue, SettingsThemeOption? newValue)
    {
        if (newValue is null)
        {
            LoadState(() => SelectedThemeOption = oldValue ?? ThemeOptions[0]);
            return;
        }

        ApplyPreferenceAndPersist();
    }

    private void ApplyPreferenceAndPersist()
    {
        if (!CanPersist || SelectedThemeOption is null)
            return;
        var theme = SelectedThemeOption.Id;
        themeService.ApplyPreference(theme, FollowSystemTheme);
        Persist(settings =>
        {
            settings.Theme = theme;
            settings.ThemeFollowSystem = FollowSystemTheme;
        });
    }
}
