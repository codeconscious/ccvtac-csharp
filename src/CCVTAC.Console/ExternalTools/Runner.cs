using System.Diagnostics;

namespace CCVTAC.Console.ExternalTools;

internal static class Runner
{
    private const int AuthenticSuccessExitCode = 0;

    private static bool IsSuccessExitCode(HashSet<int> otherSuccessExitCodes, int exitCode) =>
        otherSuccessExitCodes.Append(AuthenticSuccessExitCode).Contains(exitCode);

    /// <summary>
    /// Calls an external application.
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="otherSuccessExitCodes">Additional exit codes, other than 0, that can be treated as non-failures.</param>
    /// <param name="printer"></param>
    /// <returns>A `Result` containing the exit code, if successful, or else an error message.</returns>
    internal static Result<(int SuccessExitCode, string Warnings)> Run(
        ToolSettings settings,
        HashSet<int> otherSuccessExitCodes,
        Printer printer
    )
    {
        Watch watch = new();

        printer.Info($"Starting {settings.Program.Name} for {settings.Program.Purpose}...");
        printer.Debug($"Running command: {settings.Program.Name} {settings.Args}");

        ProcessStartInfo processStartInfo =
            new()
            {
                FileName = settings.Program.Name,
                Arguments = settings.Args,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = settings.WorkingDirectory,
            };

        using Process? process = Process.Start(processStartInfo);

        if (process is null)
        {
            return Result.Fail(
                $"Could not locate {settings.Program.Name}. If it's not installed, please install from {settings.Program.Url}."
            );
        }

        string errors = process.StandardError.ReadToEnd(); // Must precede `WaitForExit()`
        process.WaitForExit();
        printer.Info($"Completed {settings.Program.Purpose} in {watch.ElapsedFriendly}.");

        var trimmedErrors = errors.TrimTerminalLineBreak();
        return IsSuccessExitCode(otherSuccessExitCodes, process.ExitCode)
            ? Result.Ok((process.ExitCode, trimmedErrors)) // Errors will be considered warnings.
            : Result.Fail(
                $"[{settings.Program.Name}] Exit code {process.ExitCode}: {trimmedErrors}."
            );
    }
}
