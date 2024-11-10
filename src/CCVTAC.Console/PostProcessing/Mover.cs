using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using CCVTAC.Console.PostProcessing.Tagging;
using UserSettings = CCVTAC.FSharp.Settings.UserSettings;

namespace CCVTAC.Console.PostProcessing;

internal static class Mover
{
    private static readonly Regex _playlistImageRegex = new(@"\[[OP]L[\w\d_-]{12,}\]");
    private const string _imageFileWildcard = "*.jp*";

    internal static void Run(
        IEnumerable<TaggingSet> taggingSets,
        CollectionMetadata? maybeCollectionData,
        UserSettings settings,
        bool overwrite,
        Printer printer
    )
    {
        Watch watch = new();

        var workingDirInfo = new DirectoryInfo(settings.WorkingDirectory);

        string subFolderName = GetSafeSubDirectoryName(maybeCollectionData, taggingSets.First());
        string collectionName = maybeCollectionData?.Title ?? string.Empty;
        string fullMoveToDir = Path.Combine(
            settings.MoveToDirectory,
            subFolderName,
            collectionName
        );

        var dirResult = EnsureDirectoryExists(fullMoveToDir, printer);
        if (dirResult.IsFailed)
        {
            return; // The error message is printed within the above method.
        }

        var audioFileNames = workingDirInfo
            .EnumerateFiles()
            .Where(f => PostProcessor.AudioExtensions.CaseInsensitiveContains(f.Extension))
            .ToImmutableList();

        if (audioFileNames.IsEmpty)
        {
            printer.Error("No audio filenames to move found.");
            return;
        }

        printer.Debug($"Moving {audioFileNames.Count} audio file(s) to \"{fullMoveToDir}\"...");

        var (successCount, failureCount) = MoveAudioFiles(
            audioFileNames,
            fullMoveToDir,
            overwrite,
            printer
        );

        MoveImageFile(
            collectionName,
            subFolderName,
            workingDirInfo,
            fullMoveToDir,
            audioFileNames.Count,
            overwrite: false,
            printer
        );

        var fileLabel = successCount == 1 ? "file" : "files";
        printer.Info($"Moved {successCount} audio {fileLabel} in {watch.ElapsedFriendly}.");

        if (failureCount > 0)
        {
            fileLabel = failureCount == 1 ? "file" : "files";
            printer.Warning($"However, {failureCount} audio {fileLabel} could not be moved.");
        }
    }

    private static bool IsPlaylistImage(string fileName)
    {
        return _playlistImageRegex.IsMatch(fileName);
    }

    private static FileInfo? GetCoverImage(DirectoryInfo workingDirInfo, int audioFileCount)
    {
        var images = workingDirInfo.EnumerateFiles(_imageFileWildcard).ToImmutableArray();
        if (images.IsEmpty)
        {
            return null;
        }

        var playlistImages = images.Where(i => IsPlaylistImage(i.FullName));
        if (playlistImages.Any())
        {
            return playlistImages.First();
        }

        return audioFileCount > 1 && images.Length == 1 ? images.First() : null;
    }

    private static Result EnsureDirectoryExists(string moveToDir, Printer printer)
    {
        try
        {
            if (Path.Exists(moveToDir))
            {
                printer.Debug($"Found move-to directory \"{moveToDir}\".");

                return Result.Ok();
            }

            printer.Debug(
                $"Creating move-to directory \"{moveToDir}\" (based on playlist metadata)... ",
                appendLineBreak: false
            );

            Directory.CreateDirectory(moveToDir);
            printer.Debug("OK.");
            return Result.Ok();
        }
        catch (Exception ex)
        {
            printer.Error($"Error creating move-to directory \"{moveToDir}\": {ex.Message}");
            return Result.Fail(string.Empty);
        }
    }

    private static (uint successCount, uint failureCount) MoveAudioFiles(
        ICollection<FileInfo> audioFiles,
        string moveToDir,
        bool overwrite,
        Printer printer
    )
    {
        uint successCount = 0;
        uint failureCount = 0;

        foreach (FileInfo file in audioFiles)
        {
            try
            {
                File.Move(file.FullName, Path.Combine(moveToDir, file.Name), overwrite);

                successCount++;

                printer.Debug($"• Moved \"{file.Name}\"");
            }
            catch (Exception ex)
            {
                failureCount++;
                printer.Error($"• Error moving file \"{file.Name}\": {ex.Message}");
            }
        }

        return (successCount, failureCount);
    }

    private static void MoveImageFile(
        string maybeCollectionName,
        string subFolderName,
        DirectoryInfo workingDirInfo,
        string moveToDir,
        int audioFileCount,
        bool overwrite,
        Printer printer
    )
    {
        try
        {
            var baseFileName = string.IsNullOrWhiteSpace(maybeCollectionName)
                ? subFolderName
                : $"{subFolderName} - {maybeCollectionName.ReplaceInvalidPathChars()}";

            if (GetCoverImage(workingDirInfo, audioFileCount) is not FileInfo image)
            {
                return;
            }

            image.MoveTo(
                Path.Combine(moveToDir, $"{baseFileName.Trim()}.jpg"),
                overwrite: overwrite
            );

            printer.Info("Moved image file.");
        }
        catch (Exception ex)
        {
            printer.Warning($"Error copying the image file: {ex.Message}");
        }
    }

    private static string GetSafeSubDirectoryName(
        CollectionMetadata? collectionData,
        TaggingSet taggingSet
    )
    {
        string workingName;

        if (
            collectionData is CollectionMetadata metadata
            && metadata.Uploader.HasText()
            && metadata.Title.HasText()
        )
        {
            workingName = metadata.Uploader;
        }
        else
        {
            var jsonResult = GetParsedVideoJson(taggingSet);

            workingName = jsonResult.IsSuccess ? jsonResult.Value.Uploader : string.Empty;
        }

        var safeName = workingName.ReplaceInvalidPathChars().Trim();

        const string topicSuffix = " - Topic"; // Official channels append this to uploader names.
        return safeName.EndsWith(topicSuffix)
            ? safeName.Replace(topicSuffix, string.Empty)
            : safeName;
    }

    private static Result<VideoMetadata> GetParsedVideoJson(TaggingSet taggingSet)
    {
        string json;
        try
        {
            json = File.ReadAllText(taggingSet.JsonFilePath);
        }
        catch (Exception ex)
        {
            return Result.Fail(
                $"Error reading JSON file \"{taggingSet.JsonFilePath}\": {ex.Message}."
            );
        }

        try
        {
            var videoData = JsonSerializer.Deserialize<VideoMetadata>(json);
            return Result.Ok(videoData);
        }
        catch (JsonException ex)
        {
            return Result.Fail(
                $"Error deserializing JSON from file \"{taggingSet.JsonFilePath}\": {ex.Message}"
            );
        }
    }
}
