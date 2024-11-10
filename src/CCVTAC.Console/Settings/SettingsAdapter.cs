using Microsoft.FSharp.Core;
using Spectre.Console;
using UserSettings = CCVTAC.FSharp.Settings.UserSettings;

namespace CCVTAC.Console.Settings;

/// <summary>
/// Settings are managed by the corresponding F# library. This class acts as
/// the bridge between this project and the F# library.
/// </summary>
/// <remarks>
/// It would be easier to do everything here in C#, but I incorporated F# in
/// PR #38 as functional programming practice.
/// </remarks>
public static class SettingsAdapter
{
    private const string DefaultFileName = "settings.json";

    /// <summary>
    /// Reads settings or creates a new default settings file.
    /// </summary>
    /// <param name="maybeSettingsPath"></param>
    /// <param name="printer"></param>
    /// <returns>
    ///     A Result indicating three possible conditions:
    ///     1. `Ok` with successfully parsed user settings from the disk.
    ///     2. `OK` with no value, indicating that a new default file was created.
    ///     3. `Fail`, indicating a failure in the read or write process or in settings validation.
    /// </returns>
    /// <remarks>This is intended to be a temporary solution until more code is moved to F#.</remarks>
    internal static Result<UserSettings> ProcessSettings(string? maybeSettingsPath, Printer printer)
    {
        var path = FSharp.Settings.FilePath.NewFilePath(maybeSettingsPath);

        if (FSharp.Settings.IO.FileExists(path) is { IsOk: true })
        {
            try
            {
                var result = FSharp.Settings.IO.Read(path);
                if (result is { IsError: true })
                {
                    return Result.Fail($"Settings validation error: {result.ErrorValue}");
                }

                return Result.Ok(result.ResultValue);
            }
            catch (Exception ex)
            {
                return Result.Fail($"Error reading settings: {ex.Message}");
            }
        }

        try
        {
            var result = FSharp.Settings.IO.WriteDefaultFile(path, DefaultFileName);
            if (result is { IsError: true })
            {
                return Result.Fail(
                    $"Unexpected error writing the default settings: {result.ErrorValue}"
                );
            }

            printer.Info(result.ResultValue); // The new-file message.
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error writing default settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Returns a new Settings instance with the Split Chapters value toggled.
    /// This only affects the current session; the settings file is not updated.
    /// </summary>
    internal static UserSettings ToggleSplitChapters(UserSettings settings) =>
        FSharp.Settings.LiveUpdating.ToggleSplitChapters(settings);

    /// <summary>
    /// Returns a new Settings instance with the Embed Images value toggled.
    /// This only affects the current session; the settings file is not updated.
    /// </summary>
    internal static UserSettings ToggleEmbedImages(UserSettings settings) =>
        FSharp.Settings.LiveUpdating.ToggleEmbedImages(settings);

    /// <summary>
    /// Returns a new Settings instance with the Quiet Mode value toggled.
    /// This only affects the current session; the settings file is not updated.
    /// </summary>
    internal static UserSettings ToggleQuietMode(UserSettings settings) =>
        FSharp.Settings.LiveUpdating.ToggleQuietMode(settings);

    /// <summary>
    /// Returns a Result containing a new Settings instance with the Audio Format value updated,
    /// or else an Error. This only affects the current session; the settings file is not updated.
    /// </summary>
    internal static FSharpResult<UserSettings, string> UpdateAudioFormat(
        UserSettings settings,
        string newFormat
    ) => FSharp.Settings.LiveUpdating.UpdateAudioFormat(settings, newFormat.Split(','));

    /// <summary>

    /// Returns a Result containing a new Settings instance with the Audio Quality value updated,
    /// or else an Error. This only affects the current session; the settings file is not updated.
    /// </summary>
    internal static FSharpResult<UserSettings, string> UpdateAudioQuality(
        UserSettings settings,
        byte newQuality
    ) => FSharp.Settings.LiveUpdating.UpdateAudioQuality(settings, newQuality);

    /// <summary>
    /// Prints a summary of the given settings.
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="printer"></param>
    /// <param name="header">An optional line of text to appear above the settings.</param>
    internal static void PrintSummary(UserSettings settings, Printer printer, string? header = null)
    {
        if (header.HasText())
        {
            printer.Info(header!);
        }

        var table = new Table();
        table.Expand();
        table.Border(TableBorder.HeavyEdge);
        table.BorderColor(Color.Grey27);
        table.AddColumns("Name", "Value");
        table.HideHeaders();
        table.Columns[1].Width = 100; // Ensure maximum width.

        var settingPairs = FSharp.Settings.Summarize(settings);
        foreach (var pair in settingPairs)
        {
            table.AddRow(pair.Item1, pair.Item2);
        }

        printer.PrintTable(table);
    }
}
