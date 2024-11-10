using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UserSettings = CCVTAC.FSharp.Settings.UserSettings;

namespace CCVTAC.Console.PostProcessing;

internal static class Renamer
{
    public static void Run(UserSettings settings, string workingDirectory, Printer printer)
    {
        Watch watch = new();

        var workingDirInfo = new DirectoryInfo(workingDirectory);
        var audioFiles = workingDirInfo
            .EnumerateFiles()
            .Where(f => PostProcessor.AudioExtensions.CaseInsensitiveContains(f.Extension))
            .ToImmutableList();

        if (audioFiles.None())
        {
            printer.Warning("No audio files to rename were found.");
            return;
        }

        printer.Debug($"Renaming {audioFiles.Count} audio file(s)...");

        Regex regex;
        List<Match> matches;
        string matchedPatternSummary;

        foreach (FileInfo file in audioFiles)
        {
            var newFileName = settings.RenamePatterns.Aggregate(
                new StringBuilder(file.Name),
                (newNameSb, renamePattern) =>
                {
                    regex = new Regex(renamePattern.RegexPattern);

                    // A match is generated for each instance of the matched substring.
                    matches = regex
                        .Matches(newNameSb.ToString())
                        .Where(m => m.Success)
                        .Reverse() // Avoids index errors.
                        .ToList();

                    if (matches.Count == 0)
                    {
                        return newNameSb;
                    }

                    if (!settings.QuietMode)
                    {
                        matchedPatternSummary = renamePattern.Summary is null
                            ? $"`{renamePattern.RegexPattern}` (no description)"
                            : $"\"{renamePattern.Summary}\"";

                        printer.Debug(
                            $"Rename pattern {matchedPatternSummary} matched × {matches.Count}."
                        );
                    }

                    foreach (Match match in matches)
                    {
                        // Delete the matched substring from the filename by index.
                        newNameSb.Remove(match.Index, match.Length);

                        // Generate replacement text to be inserted at the same starting index
                        // using the matches and the replacement patterns from the settings.
                        var replacementText = match
                            .Groups.OfType<Group>()
                            // `Select()` indexing begins at 0, but usable regex matches begin at 1,
                            // so add 1 to both the match group and replacement placeholder indices.
                            .Select(
                                (_, i) =>
                                    (
                                        SearchFor: $"%<{i + 1}>s", // Start with placeholder #1 because...
                                        ReplaceWith: match.Groups[i + 1].Value.Trim() // ...we start with regex group #1.
                                    )
                            )
                            // Starting with the placeholder text from the settings, replace each
                            // individual placeholder with the correlated match text, then
                            // return the final string.
                            .Aggregate(
                                new StringBuilder(renamePattern.ReplaceWithPattern),
                                (workingText, replacementParts) =>
                                    workingText.Replace(
                                        replacementParts.SearchFor,
                                        replacementParts.ReplaceWith
                                    ),
                                workingText => workingText.ToString()
                            );

                        newNameSb.Insert(match.Index, replacementText);
                    }

                    return newNameSb;
                },
                newFileNameSb => newFileNameSb.ToString()
            );

            try
            {
                File.Move(file.FullName, Path.Combine(workingDirectory, newFileName));

                printer.Debug($"• From: \"{file.Name}\"");
                printer.Debug($"    To: \"{newFileName}\"");
            }
            catch (Exception ex)
            {
                printer.Error($"• Error renaming \"{file.Name}\": {ex.Message}");
            }
        }

        printer.Info($"Renaming done in {watch.ElapsedFriendly}.");
    }
}
