namespace CCVTAC.Console;

internal static class Commands
{
    internal const char Prefix = '\\';

    internal static string[] QuitOptions { get; } =
        [MakeCommand("quit"), MakeCommand("q"), MakeCommand("exit")];

    internal static string SummaryCommand { get; } = MakeCommand("commands");

    internal static string[] SettingsSummary { get; } = [MakeCommand("settings")];

    internal static string[] History { get; } = [MakeCommand("history")];

    internal static string[] SplitChapterToggles { get; } =
        [MakeCommand("split"), MakeCommand("toggle-split")];

    internal static string[] EmbedImagesToggles { get; } =
        [MakeCommand("images"), MakeCommand("toggle-images")];

    internal static string[] QuietModeToggles { get; } =
        [MakeCommand("quiet"), MakeCommand("toggle-quiet")];

    internal static string UpdateAudioFormatPrefix { get; } = MakeCommand("format-");

    internal static string UpdateAudioQualityPrefix { get; } = MakeCommand("quality-");

    internal static Dictionary<string, string> Summary { get; } =
        new()
        {
            { History[0], "See the most recently entered URLs" },
            { SplitChapterToggles[0], "Toggles chapter splitting for the current session only" },
            { EmbedImagesToggles[0], "Toggles image embedding for the current session only" },
            { QuietModeToggles[0], "Toggles quiet mode for the current session only" },
            {
                UpdateAudioFormatPrefix,
                $"Followed by a supported audio format (e.g., {UpdateAudioFormatPrefix}m4a), changes the audio format for the current session only"
            },
            {
                UpdateAudioQualityPrefix,
                $"Followed by a supported audio quality (e.g., {UpdateAudioQualityPrefix}0), changes the audio quality for the current session only"
            },
            { $"{QuitOptions[0]} or {QuitOptions[1]}", "Quit the application" },
        };

    private static string MakeCommand(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("The text cannot be null or white space.", nameof(text));

        if (text.Contains(' '))
            throw new ArgumentException(
                "The text should not contain any white space.",
                nameof(text)
            );

        return $"{Prefix}{text}";
    }
}
