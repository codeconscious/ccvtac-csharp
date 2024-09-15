using CCVTAC.Console.ExternalTools;
using MediaType = CCVTAC.FSharp.Downloading.MediaType;
using UserSettings = CCVTAC.FSharp.Settings.UserSettings;

namespace CCVTAC.Console.Downloading;

internal static class Downloader
{
    internal static ExternalTool ExternalTool = new(
        "yt-dlp",
        "https://github.com/yt-dlp/yt-dlp/",
        "YouTube downloads and audio extraction"
    );

    internal static Result<MediaType> GetMediaType(string url)
    {
        var result = FSharp.Downloading.mediaTypeWithIds(url);

        return result.IsOk
            ? Result.Ok(result.ResultValue)
            : Result.Fail(result.ErrorValue);
    }

    /// <summary>
    /// Completes the actual download process.
    /// </summary>
    /// <returns>A `Result` that, if successful, contains the name of the successfully downloaded format.</returns>
    internal static Result<string?> Run(MediaType mediaType, UserSettings settings, Printer printer)
    {
        Watch watch = new();

        if (!mediaType.IsVideo && !mediaType.IsPlaylistVideo)
        {
            printer.Info("Please wait for the multiple videos to be downloaded...");
        }

        var urls = FSharp.Downloading.downloadUrls(mediaType);

        Result downloadResult = new();
        string? successfulFormat = null;

        foreach (string format in settings.AudioFormats)
        {
            string combinedArgs = GenerateDownloadArgs(format, settings, mediaType, urls[0]);
            var downloadSettings = new ToolSettings(ExternalTool, combinedArgs, settings.WorkingDirectory!);

            downloadResult = Runner.Run(downloadSettings, printer);

            if (downloadResult.IsSuccess)
            {
                successfulFormat = format;
                break;
            }

            printer.Debug($"Failure downloading \"{format}\" format.");
        }

        Result<int> supplementaryDownloadResult = new();
        if (downloadResult.IsFailed)
        {
            downloadResult.Errors.ForEach(e => printer.Error(e.Message));
            printer.Info("Post-processing will still be attempted."); // For any partial downloads
        }
        else if (urls.Length > 1) // Meaning there's a supplementary URL for downloading playlist metadata.
        {
            // Since only metadata is downloaded, the format is irrelevant, so "best" is used as a placeholder.
            string supplementaryArgs = GenerateDownloadArgs("best", settings, null, urls[1]);

            var supplementaryDownloadSettings = new ToolSettings(
                ExternalTool,
                supplementaryArgs,
                settings.WorkingDirectory!);

            supplementaryDownloadResult = Runner.Run(supplementaryDownloadSettings, printer);

            if (supplementaryDownloadResult.IsSuccess)
            {
                printer.Info("Supplementary download completed OK.");
            }
            else
            {
                printer.Error("Supplementary download failed.");
                supplementaryDownloadResult.Errors.ForEach(e => printer.Error(e.Message));
            }
        }

        var errors = downloadResult.Errors
            .Select(e => e.Message)
            .Concat(supplementaryDownloadResult.Errors.Select(e => e.Message));

        var combinedErrors = string.Join(" / ", errors);

        return combinedErrors.Length > 0
            ? Result.Fail(combinedErrors)
            : Result.Ok();
    }

    /// <summary>
    /// Generate the entire argument string for the download tool.
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="mediaType">A `MediaType` or null (which indicates a metadata-only supplementary download).</param>
    /// <param name="additionalArgs"></param>
    /// <returns>A string of arguments that can be passed directly to the download tool.</returns>
    private static string GenerateDownloadArgs(
        string audioFormat,
        UserSettings settings,
        MediaType? mediaType,
        params string[]? additionalArgs)
    {
        const string writeJson = "--write-info-json";
        const string trimFileNames = "--trim-filenames 250";

        // yt-dlp warning: "-f best" selects the best pre-merged format which is often not the best option.
        // To let yt-dlp download and merge the best available formats, simply do not pass any format selection."
        var formatArg = audioFormat == "best" ? string.Empty : $"-f {audioFormat}";

        HashSet<string> args = mediaType switch
        {
            // For metadata-only downloads
            null => [ $"--flat-playlist {writeJson} {trimFileNames}" ],

            // For video(s) with their respective metadata files (JSON and artwork).
            _ => [
                    "--extract-audio",
                    formatArg,
                    $"--audio-quality {settings.AudioQuality}",
                    "--write-thumbnail --convert-thumbnails jpg", // For album art
                    writeJson, // Contains metadata
                    trimFileNames,
                    "--retries 2", // Default is 10, which seems like overkill
                 ]
        };

        // yt-dlp has a `--verbose` option too, but that's too much data.
        // It might be worth incorporating it in the future as a third option.
        args.Add(settings.QuietMode ? "--quiet --no-warnings" : string.Empty);

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
