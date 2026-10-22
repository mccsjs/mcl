/*
 * MCL Launcher
 * Copyright (C) 2026 Quan Zhou
 *
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.IO;
using System.Text.Json;

namespace Launcher.Infrastructure.Persistence;

internal static class PendingInstanceDeletionDirectory
{
    public const string Prefix = ".mcl-delete-pending-";
    private const string LegacyPrefix = ".bhl-delete-pending-";
    public const string MarkerFileName = ".mcl-delete-pending.json";
    private const string LegacyMarkerFileName = ".bhl-delete-pending-.json";
    public const string AbortedMarkerFileName = ".mcl-delete-aborted.json";
    private const string LegacyAbortedMarkerFileName = ".bhl-delete-aborted.json";
    public static readonly JsonSerializerOptions MarkerJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static bool IsPending(string path)
    {
        var name = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
               || name.StartsWith(LegacyPrefix, StringComparison.OrdinalIgnoreCase);
    }

    public static string GetMarkerPath(string directory)
    {
        var currentPath = Path.Combine(directory, MarkerFileName);
        return File.Exists(currentPath) || !DirectoryNameIsLegacy(directory)
            ? currentPath
            : Path.Combine(directory, LegacyMarkerFileName);
    }

    public static string GetAbortedMarkerPath(string directory) => Path.Combine(
        directory,
        DirectoryNameIsLegacy(directory) ? LegacyAbortedMarkerFileName : AbortedMarkerFileName);

    private static bool DirectoryNameIsLegacy(string directory) =>
        Path.GetFileName(directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            .StartsWith(LegacyPrefix, StringComparison.OrdinalIgnoreCase);

    public static bool TryReadValidMarker(string directory, out PendingInstanceDeletionMarker marker)
    {
        marker = default!;
        try
        {
            var markerPath = GetMarkerPath(directory);
            if (!File.Exists(markerPath))
                return false;
            var parsed = JsonSerializer.Deserialize<PendingInstanceDeletionMarker>(
                File.ReadAllText(markerPath),
                MarkerJsonOptions);
            if (parsed is null
                || parsed.SchemaVersion is not (1 or 2)
                || string.IsNullOrWhiteSpace(parsed.VersionName)
                || (parsed.SchemaVersion == 2 && string.IsNullOrWhiteSpace(parsed.InstanceId))
                || !Guid.TryParseExact(parsed.TransactionId, "N", out _))
                return false;
            var prefix = DirectoryNameIsLegacy(directory) ? LegacyPrefix : Prefix;
            var expectedName = $"{prefix}{parsed.VersionName}-{parsed.TransactionId[..8].ToLowerInvariant()}";
            if (!string.Equals(Path.GetFileName(directory), expectedName, StringComparison.OrdinalIgnoreCase))
                return false;
            marker = parsed;
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return false;
        }
    }
}

internal sealed record PendingInstanceDeletionMarker(
    int SchemaVersion,
    string TransactionId,
    string VersionName,
    string? InstanceId,
    DateTimeOffset CreatedAtUtc);
