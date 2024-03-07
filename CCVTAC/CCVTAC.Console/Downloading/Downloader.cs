using CCVTAC.Console.ExternalTools;
using CCVTAC.Console.Settings;
using MediaType = CCVTAC.FSharp.Downloading.MediaType;

namespace CCVTAC.Console.Downloading;

internal static class Downloader
{
    internal static ExternalTool ExternalTool = new(
        "yt-dlp",
        "https://github.com/yt-dlp/yt-dlp/",
        "YouTube media and metadata downloads, plus audio extraction"
    );

    /// <summary>
    /// All known error codes returned by yt-dlp with their meanings.
    /// </summary>
    /// <remarks>Source: https://github.com/yt-dlp/yt-dlp/issues/4262#issuecomment-1173133105</remarks>
    internal static Dictionary<int, string> ExitCodes = new()
    {
        { 0, "Success" },
        { 1, "Unspecified error" },
        { 2, "Error in provided options" },
        { 100, "yt-dlp must restart for update to complete" },
        { 101, "Download cancelled by --max-downloads, etc." },
    };

    internal static Result<string> Run(string url, UserSettings settings, Printer printer)
    {
        Watch watch = new();

        var mediaTypeOrError = FSharp.Downloading.mediaTypeWithIds(url);
        if (mediaTypeOrError.IsError)
        {
            return Result.Fail(mediaTypeOrError.ErrorValue);
        }

        var mediaType = mediaTypeOrError.ResultValue;
        printer.Print($"{mediaType.GetType().Name} URL '{url}' detected.");

        var urls = FSharp.Downloading.downloadUrls(mediaType);

        string args = GenerateDownloadArgs(settings, mediaType, urls[0]);
        var downloadSettings = new ToolSettings(ExternalTool, args, settings.WorkingDirectory!, ExitCodes);
        var downloadResult = Runner.Run(downloadSettings, printer);

        if (downloadResult.IsFailed)
        {
            downloadResult.Errors.ForEach(e => printer.Error(e.Message));
            printer.Warning("However, post-processing will still be attempted."); // For any partial downloads
        }
        else if (urls.Length > 1) // Meaning there's a supplementary URL for downloading playlist metadata.
        {
            string supplementaryArgs = GenerateDownloadArgs(settings, null, urls[1]);

            var supplementaryDownloadSettings = new ToolSettings(
                ExternalTool,
                supplementaryArgs,
                settings.WorkingDirectory!,
                ExitCodes);

            Result<int> supplementaryDownloadResult = Runner.Run(supplementaryDownloadSettings, printer);

            if (supplementaryDownloadResult.IsSuccess)
            {
                printer.Print("Supplementary download completed OK.");
            }
            else
            {
                printer.Error("Supplementary download failed.");
                supplementaryDownloadResult.Errors.ForEach(e => printer.Error(e.Message));
            }
        }

        return Result.Ok($"Downloading done in {watch.ElapsedFriendly}.");
    }

    /// <summary>
    /// Generate the entire argument string for the download tool.
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="mediaType">A `MediaType` or null (which indicates a metadata-only supplementary download).</param>
    /// <param name="additionalArgs"></param>
    /// <returns>A string of arguments that can be passed directly to the download tool.</returns>
    private static string GenerateDownloadArgs(
        UserSettings settings,
        MediaType? mediaType,
        params string[]? additionalArgs)
    {
        const string writeJson = "--write-info-json";
        const string trimFileNames = "--trim-filenames 250";

        HashSet<string> args = mediaType switch
        {
            null => [ $"--flat-playlist {writeJson} {trimFileNames}" ], // Metadata-only download
            _ => [
                     $"--extract-audio -f {settings.AudioFormat}",
                     "--write-thumbnail --convert-thumbnails jpg", // For album art
                     writeJson, // Contains metadata
                     trimFileNames,
                     "--retries 3", // Default is 10, which seems more than necessary
                 ]
        };

        // `--verbose` is a yt-dlp option too, but maybe that's too much data.
        // It might be worth incorporating it in the future as a third option.
        args.Add(settings.VerboseOutput ? string.Empty : "--quiet --progress");

        if (mediaType is not null)
        {
            if (settings.SplitChapters)
            {
                args.Add("--split-chapters");
            }

            if (!mediaType.IsVideo && !mediaType.IsPlaylistVideo)
            {
                args.Add($"--sleep-interval {settings.SleepSecondsBetweenDownloads}");
            }

            // The numbering of regular playlists should be reversed because the newest items are
            // always placed at the top of the list at position #1. Instead, the oldest items
            // (at the end of the list) should begin at #1.
            if (mediaType.IsStandardPlaylist)
            {
                // The digits followed by `B` induce trimming to the specified number of bytes.
                // Use `s` instead of `B` to trim to a specified number of characters.
                // Reference: https://github.com/yt-dlp/yt-dlp/issues/1136#issuecomment-1114252397
                // Also, it's possible this trimming should be applied to `ReleasePlaylist`s too.
                args.Add("""-o "%(uploader).80B - %(playlist).80B - %(playlist_autonumber)s - %(title).150B [%(id)s].%(ext)s" --playlist-reverse""");
            }
        }

        return string.Join(" ", args.Concat(additionalArgs ?? []));
    }
}
