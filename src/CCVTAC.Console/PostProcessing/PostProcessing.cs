using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using CCVTAC.Console.PostProcessing.Tagging;
using static CCVTAC.FSharp.Downloading;
using UserSettings = CCVTAC.FSharp.Settings.UserSettings;

namespace CCVTAC.Console.PostProcessing;

internal static partial class PostProcessor
{
    internal static readonly string[] AudioExtensions =
    [
        ".aac",
        ".alac",
        ".flac",
        ".m4a",
        ".mp3",
        ".ogg",
        ".vorbis",
        ".opus",
        ".wav",
    ];

    internal static void Run(UserSettings settings, MediaType mediaType, Printer printer)
    {
        Watch watch = new();
        string workingDirectory = settings.WorkingDirectory;

        printer.Info("Starting post-processing...");

        var taggingSetsResult = GenerateTaggingSets(workingDirectory);
        if (taggingSetsResult.IsFailed)
        {
            printer.Error("No tagging sets were generated, so tagging cannot be done.");
            return;
        }
        var taggingSets = taggingSetsResult.Value;

        var collectionJsonResult = GetCollectionJson(workingDirectory);
        CollectionMetadata? collectionJson;
        if (collectionJsonResult.IsFailed)
        {
            printer.Debug(
                $"No playlist or channel metadata found: {collectionJsonResult.Errors.First().Message}"
            );
            collectionJson = null;
        }
        else
        {
            printer.Debug("Found playlist/channel metadata.");
            collectionJson = collectionJsonResult.Value;
        }

        if (settings.EmbedImages)
        {
            ImageProcessor.Run(workingDirectory, printer);
        }

        var tagResult = Tagger.Run(settings, taggingSets, collectionJson, mediaType, printer);
        if (tagResult.IsSuccess)
        {
            printer.Info(tagResult.Value);

            Renamer.Run(settings, workingDirectory, printer);

            Mover.Run(taggingSets, collectionJson, settings, true, printer);

            var taggingSetFileNames = taggingSets.SelectMany(set => set.AllFiles).ToList();
            Deleter.Run(taggingSetFileNames, collectionJson, workingDirectory, printer);

            IoUtilities.Directories.WarnIfAnyFiles(workingDirectory, 10);
        }
        else
        {
            printer.Errors("Tagging error(s) preventing further post-processing: ", tagResult);
        }

        printer.Info($"Post-processing done in {watch.ElapsedFriendly}.");
    }

    private static Result<CollectionMetadata> GetCollectionJson(string workingDirectory)
    {
        try
        {
            var fileNames = Directory
                .GetFiles(workingDirectory)
                .Where(f => CollectionMetadataRegex().IsMatch(f))
                .ToImmutableHashSet();

            if (fileNames.Count == 0)
            {
                return Result.Fail("No relevant files found.");
            }

            if (fileNames.Count > 1)
            {
                return Result.Fail(
                    "Unexpectedly found more than one relevant file, so none will be processed."
                );
            }

            string fileName = fileNames.Single();
            string json = File.ReadAllText(fileName);
            var collectionData = JsonSerializer.Deserialize<CollectionMetadata>(json);

            return Result.Ok(collectionData);
        }
        catch (Exception ex)
        {
            return Result.Fail($"{ex.Message}");
        }
    }

    private static Result<ImmutableList<TaggingSet>> GenerateTaggingSets(string directory)
    {
        try
        {
            string[] files = Directory.GetFiles(directory);
            var taggingSets = TaggingSet.CreateSets(files);

            return taggingSets.Any()
                ? Result.Ok(taggingSets)
                : Result.Fail(
                    $"No tagging sets were created using working directory \"{directory}\"."
                );
        }
        catch (DirectoryNotFoundException)
        {
            return Result.Fail($"Directory \"{directory}\" does not exist.");
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error reading working directory files: {ex.Message}");
        }
    }

    /// <summary>
    /// A regular expression that detects metadata files for collections.
    /// </summary>
    [GeneratedRegex("""(?<=\[)[\w\-]{17,}(?=\]\.info.json)""")]
    private static partial Regex CollectionMetadataRegex();
}
