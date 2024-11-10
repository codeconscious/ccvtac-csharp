namespace CCVTAC.Console.PostProcessing;

public static class YouTubeMetadataExtensionMethods
{
    /// <summary>
    /// Returns a string summarizing video uploader information.
    /// </summary>
    private static string UploaderSummary(this VideoMetadata videoData)
    {
        string uploaderLinkOrIdOrEmpty =
            videoData.UploaderUrl.HasText() ? videoData.UploaderUrl
            : videoData.UploaderId.HasText() ? videoData.UploaderId
            : string.Empty;

        return videoData.Uploader
            + (uploaderLinkOrIdOrEmpty.HasText() ? $" ({uploaderLinkOrIdOrEmpty})" : string.Empty);
    }

    /// <summary>
    /// Returns a formatted MM/DD/YYYY version of the upload date (e.g., "08/27/2023") from the
    /// plain YYYYMMDD version (e.g., "20230827") within the parsed JSON file data.
    /// </summary>
    private static string FormattedUploadDate(this VideoMetadata videoData)
    {
        return $"{videoData.UploadDate[4..6]}/{videoData.UploadDate[6..8]}/{videoData.UploadDate[0..4]}";
    }

    /// <summary>
    /// Returns a formatted comment using data parsed from the JSON file.
    /// </summary>
    public static string GenerateComment(
        this VideoMetadata videoData,
        CollectionMetadata? maybeCollectionData
    )
    {
        System.Text.StringBuilder sb = new();

        sb.AppendLine("CCVTAC SOURCE DATA:");
        sb.AppendLine($"■ Downloaded: {DateTime.Now}");
        // sb.AppendLine($"■ Service: {videoData.ExtractorKey}"); // "Youtube"
        sb.AppendLine($"■ URL: {videoData.WebpageUrl}");
        sb.AppendLine($"■ Title: {videoData.Fulltitle}");
        sb.AppendLine($"■ Uploader: {videoData.UploaderSummary()}");
        if (videoData.Creator != videoData.Uploader && videoData.Creator.HasText())
        {
            sb.AppendLine($"■ Creator: {videoData.Creator}");
        }
        if (videoData.Artist.HasText())
        {
            sb.AppendLine($"■ Artist: {videoData.Artist}");
        }
        if (videoData.Album.HasText())
        {
            sb.AppendLine($"■ Album: {videoData.Album}");
        }
        if (videoData.Title.HasText() && videoData.Title != videoData.Fulltitle)
        {
            sb.AppendLine($"■ Track Title: {videoData.Title}");
        }
        sb.AppendLine($"■ Uploaded: {videoData.FormattedUploadDate()}");
        var description = string.IsNullOrWhiteSpace(videoData.Description)
            ? "None."
            : videoData.Description;
        sb.AppendLine($"■ Video description: {description}");

        if (maybeCollectionData is { } collectionData)
        {
            sb.AppendLine();
            sb.AppendLine($"■ Playlist name: {collectionData.Title}");
            sb.AppendLine($"■ Playlist URL: {collectionData.WebpageUrl}");
            if (videoData.PlaylistIndex is { } index)
            {
                sb.AppendLine($"■ Playlist index: {index}");
            }
            if (collectionData.Description.HasText())
            {
                sb.AppendLine($"■ Playlist description: {collectionData.Description}");
            }
        }

        return sb.ToString();
    }
}
