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

using Launcher.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Win32;

namespace Launcher.App.Services;

/// <summary>
/// Maintains launcher appearance preferences and publishes effective theme and background-effect changes.
/// Resource ordering and progressive-blur capability handling are delegated to dedicated collaborators.
/// </summary>
public sealed class ThemeService : IThemeService, IDisposable
{
    private readonly IUiDispatcher uiDispatcher;
    private readonly ILogger<ThemeService> logger;
    private readonly ThemeResourceLayerManager resourceLayerManager;
    private readonly ProgressiveBlurController progressiveBlurController;
    private string preferredTheme = LauncherDefaults.DefaultTheme;
    private const string backgroundEffect = LauncherBackgroundEffects.Image;
    private bool followSystem = true;
    private bool hasAppliedTheme;
    private bool isDisposed;

    public ThemeService(IUiDispatcher uiDispatcher, ILogger<ThemeService>? logger = null)
        : this(
            uiDispatcher,
            logger,
            new WpfProgressiveBlurSupport(),
            new ThemeResourceLayerManager())
    {
    }

    internal ThemeService(
        IUiDispatcher uiDispatcher,
        ILogger<ThemeService>? logger,
        IProgressiveBlurSupport progressiveBlurSupport,
        ThemeResourceLayerManager resourceLayerManager)
    {
        this.uiDispatcher = uiDispatcher ?? throw new ArgumentNullException(nameof(uiDispatcher));
        this.logger = logger ?? NullLogger<ThemeService>.Instance;
        this.resourceLayerManager = resourceLayerManager
            ?? throw new ArgumentNullException(nameof(resourceLayerManager));
        progressiveBlurController = new ProgressiveBlurController(
            this.uiDispatcher,
            progressiveBlurSupport ?? throw new ArgumentNullException(nameof(progressiveBlurSupport)),
            this.logger);
        SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
    }

    public EffectiveTheme EffectiveTheme { get; private set; } = EffectiveTheme.Dark;

    public string BackgroundEffect => backgroundEffect;

    public event EventHandler<EffectiveThemeChangedEventArgs>? EffectiveThemeChanged;

    public event EventHandler<BackgroundEffectChangedEventArgs>? BackgroundEffectChanged;

    public void ApplyPreference(string? theme, bool followSystem)
    {
        preferredTheme = NormalizeTheme(theme);
        this.followSystem = followSystem;

        var oldTheme = EffectiveTheme;
        var nextTheme = ResolveEffectiveTheme(preferredTheme, followSystem);
        EffectiveTheme = nextTheme;
        uiDispatcher.Invoke(() =>
        {
            ApplyAppearanceResourcesCore();
            progressiveBlurController.Initialize();
        });

        if (!hasAppliedTheme)
        {
            hasAppliedTheme = true;
            return;
        }

        if (oldTheme != nextTheme)
            EffectiveThemeChanged?.Invoke(this, new EffectiveThemeChangedEventArgs(oldTheme, nextTheme));
    }

    public void Dispose()
    {
        if (isDisposed)
            return;

        SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
        progressiveBlurController.Dispose();
        isDisposed = true;
    }

    private void ApplyAppearanceResourcesCore()
    {
        var application = global::System.Windows.Application.Current;
        if (application is null)
            return;

        var presentation = LauncherBackgroundPresentationPolicy.Resolve(
            backgroundEffect,
            LauncherDefaults.DefaultLauncherBackgroundOpacityPercent,
            enableImageControlBlur: true);
        resourceLayerManager.ApplyLayers(
            application.Resources,
            EffectiveTheme,
            presentation.IsImageBackgroundEnabled,
            presentation.IsImageControlBlurEnabled);
        ThemeResourceLayerManager.ApplyPageBackgroundOpacity(
            application.Resources,
            presentation.PageBackgroundOpacityPercent);
        logger.LogDebug(
            "Launcher appearance resources applied. Theme={Theme} BackgroundEffect={BackgroundEffect} ImageLayerEnabled={ImageLayerEnabled} ImageControlBlurEnabled={ImageControlBlurEnabled}",
            EffectiveTheme,
            presentation.Effect,
            presentation.IsImageBackgroundEnabled,
            presentation.IsImageControlBlurEnabled);
    }

    private EffectiveTheme ResolveEffectiveTheme(string theme, bool useSystemTheme)
    {
        if (useSystemTheme)
            return ResolveSystemTheme();

        return string.Equals(theme, "Light", StringComparison.OrdinalIgnoreCase)
            ? EffectiveTheme.Light
            : EffectiveTheme.Dark;
    }

    private static EffectiveTheme ResolveSystemTheme()
    {
        try
        {
            var value = Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "AppsUseLightTheme",
                0);
            return value is int appsUseLightTheme && appsUseLightTheme > 0
                ? EffectiveTheme.Light
                : EffectiveTheme.Dark;
        }
        catch
        {
            return EffectiveTheme.Dark;
        }
    }

    private static string NormalizeTheme(string? theme)
    {
        return string.Equals(theme, "Light", StringComparison.OrdinalIgnoreCase)
            ? "Light"
            : LauncherDefaults.DefaultTheme;
    }

    private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (!followSystem)
            return;

        if (e.Category is not UserPreferenceCategory.General
            and not UserPreferenceCategory.VisualStyle
            and not UserPreferenceCategory.Color)
        {
            return;
        }

        uiDispatcher.Post(() => ApplyPreference(preferredTheme, followSystem));
    }
}
