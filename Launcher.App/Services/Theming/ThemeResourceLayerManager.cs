/*
 * MCL Launcher
 * Copyright (C) 2026 Quan Zhou
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Windows;
using Launcher.Domain.Models;

namespace Launcher.App.Services;

internal sealed class ThemeResourceLayerManager
{
    internal const string PageBackgroundOpacityResourceKey = "Opacity.Page.Background";

    private const string DarkThemeSource =
        "pack://application:,,,/mccsjs_launcher;component/Resources/Themes/Dark.xaml";

    private const string LightThemeSource =
        "pack://application:,,,/mccsjs_launcher;component/Resources/Themes/Light.xaml";

    private const string ImageBackgroundSource =
        "pack://application:,,,/mccsjs_launcher;component/Resources/Themes/Backgrounds/Image.xaml";

    private const string ImageBlurSource =
        "pack://application:,,,/mccsjs_launcher;component/Resources/Themes/Backgrounds/ImageBlur.xaml";

    private readonly Func<string, ResourceDictionary> dictionaryFactory;
    private ResourceDictionary? themeLayer;
    private string? themeLayerSource;
    private ResourceDictionary? imageLayer;
    private ResourceDictionary? imageBlurLayer;

    public ThemeResourceLayerManager()
        : this(source => new ResourceDictionary { Source = new Uri(source, UriKind.Absolute) })
    {
    }

    internal ThemeResourceLayerManager(Func<string, ResourceDictionary> dictionaryFactory)
    {
        this.dictionaryFactory = dictionaryFactory ?? throw new ArgumentNullException(nameof(dictionaryFactory));
    }

    public void ApplyLayers(
        ResourceDictionary applicationResources,
        EffectiveTheme theme,
        bool imageBackgroundEnabled,
        bool imageControlBlurEnabled)
    {
        ArgumentNullException.ThrowIfNull(applicationResources);

        var dictionaries = applicationResources.MergedDictionaries;
        RemoveLayer(dictionaries, themeLayer);
        RemoveLayer(dictionaries, imageLayer);
        RemoveLayer(dictionaries, imageBlurLayer);

        var nextThemeSource = GetThemeSource(theme);
        themeLayer = GetOrCreateLayer(themeLayer, ref themeLayerSource, nextThemeSource);

        dictionaries.Add(themeLayer);
        if (imageBackgroundEnabled)
        {
            imageLayer ??= dictionaryFactory(ImageBackgroundSource);
            dictionaries.Add(imageLayer);
            if (imageControlBlurEnabled)
            {
                imageBlurLayer ??= dictionaryFactory(ImageBlurSource);
                dictionaries.Add(imageBlurLayer);
            }
        }
    }

    public static void ApplyPageBackgroundOpacity(
        ResourceDictionary applicationResources,
        int opacityPercent)
    {
        ArgumentNullException.ThrowIfNull(applicationResources);
        applicationResources[PageBackgroundOpacityResourceKey] = Math.Clamp(opacityPercent, 0, 100) / 100d;
    }

    internal static string GetThemeSource(EffectiveTheme theme) =>
        theme is EffectiveTheme.Light ? LightThemeSource : DarkThemeSource;

    private ResourceDictionary GetOrCreateLayer(
        ResourceDictionary? currentLayer,
        ref string? currentSource,
        string nextSource)
    {
        if (currentLayer is not null
            && string.Equals(currentSource, nextSource, StringComparison.OrdinalIgnoreCase))
            return currentLayer;

        currentSource = nextSource;
        return dictionaryFactory(nextSource);
    }

    private static void RemoveLayer(
        ICollection<ResourceDictionary> dictionaries,
        ResourceDictionary? layer)
    {
        if (layer is null)
            return;

        while (dictionaries.Remove(layer))
        {
        }
    }
}
