using System.Diagnostics;

namespace CCVTAC.Console.ExternalTools;

internal record ExternalTool
{
    /// <summary>
    /// The name of the program. This should be the exact text used to call it
    /// on the command line, excluding any arguments.
    /// </summary>
    internal string Name { get; }

    /// <summary>
    /// The URL of the program, from which users should install it if needed.
    /// </summary>
    internal string Url { get; }

    /// <summary>
    /// A brief summary of the purpose of the program within the context of this program.
    /// Should be phrased as a noun (e.g., "image processing" or "audio normalization").
    /// </summary>
    internal string Purpose { get; }

    /// <summary>
    ///
    /// </summary>
    /// <param name="name">The name of the program. This should be the exact text used to call it
    /// on the command line, excluding any arguments.</param>
    /// <param name="url">The URL of the program, from which users should install it if needed.</param>
    /// <param name="purpose">A brief summary of the purpose of the program within the context of this program.
    /// Should be phrased as a noun (e.g., "image processing" or "audio normalization").</param>
    internal ExternalTool(string name, string url, string purpose)
    {
        Name = name.Trim();
        Url = url.Trim();
        Purpose = purpose.Trim();
    }

    /// <summary>
    /// Attempts a dry run of the program to determine if it is installed and available on this system.
    /// </summary>
    /// <returns>A Result indicating whether the program is available or not.</returns>
    internal Result ProgramExists()
    {
        ProcessStartInfo processStartInfo =
            new()
            {
                FileName = Name,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

        try
        {
            using Process? process = Process.Start(processStartInfo);

            if (process is null)
            {
                return Result.Fail(
                    $"The program \"{Name}\" was not found. (The process was null.)"
                );
            }

            process.WaitForExit();
            return Result.Ok();
        }
        catch (Exception)
        {
            return Result.Fail($"The program \"{Name}\" was not found.");
        }
    }
}
