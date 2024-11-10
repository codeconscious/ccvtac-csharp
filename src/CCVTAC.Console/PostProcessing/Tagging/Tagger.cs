using System.IO;
using System.Text.Json;
using static CCVTAC.FSharp.Downloading;
using TaggedFile = TagLib.File;
using UserSettings = CCVTAC.FSharp.Settings.UserSettings;

namespace CCVTAC.Console.PostProcessing.Tagging;

internal static class Tagger
{
    internal static Result<string> Run(
        UserSettings settings,
        IEnumerable<TaggingSet> taggingSets,
        CollectionMetadata? collectionJson,
        MediaType mediaType,
        Printer printer
    )
    {
        printer.Debug("Adding file tags...");

        Watch watch = new();

        bool embedImages = settings.EmbedImages && mediaType.IsVideo || mediaType.IsPlaylistVideo;

        foreach (var taggingSet in taggingSets)
        {
            ProcessSingleTaggingSet(settings, taggingSet, collectionJson, embedImages, printer);
        }

        return Result.Ok($"Tagging done in {watch.ElapsedFriendly}.");
    }

    private static void ProcessSingleTaggingSet(
        UserSettings settings,
        TaggingSet taggingSet,
        CollectionMetadata? collectionJson,
        bool embedImages,
        Printer printer
    )
    {
        printer.Debug(
            $"{taggingSet.AudioFilePaths.Count} audio file(s) with resource ID \"{taggingSet.ResourceId}\""
        );

        var parsedJsonResult = ParseVideoJson(taggingSet);
        if (parsedJsonResult.IsFailed)
        {
            printer.Errors(
                $"Error deserializing video metadata from \"{taggingSet.JsonFilePath}\":",
                parsedJsonResult
            );
            return;
        }

        TaggingSet finalTaggingSet = DeleteSourceFile(taggingSet, printer);

        // If a single video was split, the tagging set will have multiple audio file paths.
        // In this case, we will NOT embed the image file (with the assumption that
        // the standalone image file will be available in the move-to directory).
        string? maybeImagePath =
            embedImages && finalTaggingSet.AudioFilePaths.Count == 1
                ? finalTaggingSet.ImageFilePath
                : null;

        foreach (string audioPath in finalTaggingSet.AudioFilePaths)
        {
            try
            {
                TagSingleFile(
                    settings,
                    parsedJsonResult.Value,
                    audioPath,
                    maybeImagePath,
                    collectionJson,
                    printer
                );
            }
            catch (Exception ex)
            {
                printer.Error($"Error tagging file: {ex.Message}");
            }
        }
    }

    static void TagSingleFile(
        UserSettings settings,
        VideoMetadata videoData,
        string audioFilePath,
        string? imageFilePath,
        CollectionMetadata? collectionData,
        Printer printer
    )
    {
        string audioFileName = Path.GetFileName(audioFilePath);

        printer.Debug($"Current audio file: \"{audioFileName}\"");

        using var taggedFile = TaggedFile.Create(audioFilePath);
        TagDetector tagDetector = new(settings.TagDetectionPatterns);

        if (videoData.Track is { } metadataTitle)
        {
            printer.Debug($"• Using metadata title \"{metadataTitle}\"");
            taggedFile.Tag.Title = metadataTitle;
        }
        else
        {
            var title = tagDetector.DetectTitle(videoData, videoData.Title);
            printer.Debug($"• Found title \"{title}\"");
            taggedFile.Tag.Title = title;
        }

        if (videoData.Artist is { } metadataArtists)
        {
            var firstArtist = metadataArtists.Split(", ").First();
            var diffSummary =
                firstArtist == metadataArtists
                    ? string.Empty
                    : $" (extracted from \"{metadataArtists}\")";
            taggedFile.Tag.Performers = [firstArtist];

            printer.Debug($"• Using metadata artist \"{firstArtist}\"{diffSummary}");
        }
        else if (tagDetector.DetectArtist(videoData) is { } artist)
        {
            printer.Debug($"• Found artist \"{artist}\"");
            taggedFile.Tag.Performers = [artist];
        }

        if (videoData.Album is { } metadataAlbum)
        {
            printer.Debug($"• Using metadata album \"{metadataAlbum}\"");
            taggedFile.Tag.Album = metadataAlbum;
        }
        else if (tagDetector.DetectAlbum(videoData, collectionData?.Title) is { } album)
        {
            printer.Debug($"• Found album \"{album}\"");
            taggedFile.Tag.Album = album;
        }

        if (tagDetector.DetectComposers(videoData) is { } composers)
        {
            printer.Debug($"• Found composer(s) \"{composers}\"");
            taggedFile.Tag.Composers = [composers];
        }

        if (videoData.PlaylistIndex is { } trackNo)
        {
            printer.Debug($"• Using playlist index of {trackNo} for track number");
            taggedFile.Tag.Track = trackNo;
        }

        if (videoData.ReleaseYear is { } releaseYear)
        {
            printer.Debug($"• Using metadata release year \"{releaseYear}\"");
            taggedFile.Tag.Year = releaseYear;
        }
        else
        {
            ushort? maybeDefaultYear = GetAppropriateReleaseDateIfAny(settings, videoData);

            if (tagDetector.DetectReleaseYear(videoData, maybeDefaultYear) is { } year)
            {
                printer.Debug($"• Found year \"{year}\"");
                taggedFile.Tag.Year = year;
            }
        }

        taggedFile.Tag.Comment = videoData.GenerateComment(collectionData);

        if (
            settings.EmbedImages
            && !settings.DoNotEmbedImageUploaders.Contains(videoData.Uploader)
            && imageFilePath is not null
        )
        {
            printer.Info("Embedding artwork.");
            WriteImage(taggedFile, imageFilePath, printer);
        }
        else
        {
            printer.Debug("Skipping artwork embedding.");
        }

        taggedFile.Save();
        printer.Debug($"Wrote tags to \"{audioFileName}\".");
        return;

        /// <summary>
        /// If the supplied video uploader is specified in the settings, returns the video's upload year.
        /// Otherwise, returns null.
        /// </summary>
        static ushort? GetAppropriateReleaseDateIfAny(
            UserSettings settings,
            VideoMetadata videoData
        )
        {
            if (
                settings.IgnoreUploadYearUploaders?.Contains(
                    videoData.Uploader,
                    StringComparer.OrdinalIgnoreCase
                ) == true
            )
            {
                return null;
            }

            return ushort.TryParse(videoData.UploadDate[..4], out var parsedYear)
                ? parsedYear
                : null;
        }
    }

    private static Result<VideoMetadata> ParseVideoJson(TaggingSet taggingSet)
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
            return Result.Fail($"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Deletes the pre-split source audio for split videos (if any), each of which will have the same resource ID.
    /// Returns a modified TaggingSet with the deleted files removed.
    /// </summary>
    /// <param name="taggingSet"></param>
    /// <param name="printer"></param>
    private static TaggingSet DeleteSourceFile(TaggingSet taggingSet, Printer printer)
    {
        // If there is only one file, then there are no child files, so no action is necessary.
        if (taggingSet.AudioFilePaths.Count <= 1)
        {
            return taggingSet;
        }

        // The largest audio file must be the source file.
        var largestFileInfo = taggingSet
            .AudioFilePaths.Select(fileName => new FileInfo(fileName))
            .OrderByDescending(fi => fi.Length)
            .First();

        try
        {
            File.Delete(largestFileInfo.FullName);
            printer.Debug($"Deleted pre-split source file \"{largestFileInfo.Name}\"");

            return taggingSet with
            {
                AudioFilePaths = taggingSet.AudioFilePaths.Remove(largestFileInfo.FullName),
            };
        }
        catch (Exception ex)
        {
            printer.Error(
                $"Error deleting pre-split source file \"{largestFileInfo.Name}\": {ex.Message}"
            );
            return taggingSet;
        }
    }

    /// <summary>
    /// Write the video thumbnail to the file tags.
    /// </summary>
    /// <remarks>Heavily inspired by https://stackoverflow.com/a/61264720/11767771.</remarks>
    private static void WriteImage(TaggedFile taggedFile, string imageFilePath, Printer printer)
    {
        if (string.IsNullOrWhiteSpace(imageFilePath))
        {
            printer.Error("No image file path was provided, so cannot add an image to the file.");
            return;
        }

        try
        {
            var pics = new TagLib.IPicture[1];
            pics[0] = new TagLib.Picture(imageFilePath);
            taggedFile.Tag.Pictures = pics;

            printer.Debug("Image written to file tags OK.");
        }
        catch (Exception ex)
        {
            printer.Error($"Error writing image to the audio file: {ex.Message}");
        }
    }
}
