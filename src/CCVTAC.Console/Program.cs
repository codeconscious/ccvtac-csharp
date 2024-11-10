using CCVTAC.Console.IoUtilities;
using CCVTAC.Console.Settings;
using Spectre.Console;

namespace CCVTAC.Console;

internal static class Program
{
    private static readonly string[] _helpFlags = ["-h", "--help"];
    private static readonly string[] _settingsFileFlags = ["-s", "--settings"];
    private const string _defaultSettingsFileName = "settings.json";

    private static void Main(string[] args)
    {
        Printer printer = new(showDebug: true);

        if (args.Length > 0 && _helpFlags.CaseInsensitiveContains(args[0]))
        {
            Help.Print(printer);
            return;
        }

        string maybeSettingsPath =
            args.Length >= 2 && _settingsFileFlags.CaseInsensitiveContains(args[0])
                ? args[1] // Expected to be a settings file path.
                : _defaultSettingsFileName;

        var settingsResult = SettingsAdapter.ProcessSettings(maybeSettingsPath, printer);
        if (settingsResult.IsFailed)
        {
            printer.Errors(settingsResult.Errors.Select(e => e.Message).ToList());
            return;
        }
        if (settingsResult.Value is null) // If a new settings file was created.
        {
            return;
        }

        var settings = settingsResult.Value;
        SettingsAdapter.PrintSummary(settings, printer, header: "Settings loaded OK.");

        printer.ShowDebug(!settings.QuietMode);

        // Catch Ctrl-C (SIGINT).
        System.Console.CancelKeyPress += delegate
        {
            printer.Warning("\nQuitting at user's request.");

            var warnResult = IoUtilities.Directories.WarnIfAnyFiles(settings.WorkingDirectory, 10);

            if (warnResult.IsSuccess)
            {
                return;
            }

            printer.FirstError(warnResult);

            var deleteResult = Directories.AskToDeleteAllFiles(settings.WorkingDirectory, printer);
            if (deleteResult.IsSuccess)
            {
                printer.Info($"{deleteResult.Value} file(s) deleted.");
            }
            else
            {
                printer.FirstError(deleteResult);
            }
        };

        // Top-level `try` block to catch and pretty-print unexpected exceptions.
        try
        {
            Orchestrator.Start(settings, printer);
        }
        catch (Exception topException)
        {
            printer.Critical($"Fatal error: {topException.Message}");
            AnsiConsole.WriteException(topException);
            printer.Info(
                "Please help improve this tool by reporting this error and any relevant URLs at https://github.com/codeconscious/ccvtac/issues."
            );
        }
    }
}
