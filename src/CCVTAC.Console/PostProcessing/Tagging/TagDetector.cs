using static CCVTAC.FSharp.Settings;

namespace CCVTAC.Console.PostProcessing.Tagging;

/// <summary>
/// Provides methods to search for specific tag field data (artist, album, etc.) within video metadata.
/// </summary>
internal sealed class TagDetector
{
    private TagDetectionPatterns Patterns { get; init; }

    internal TagDetector(TagDetectionPatterns tagDetectionPatterns)
    {
        Patterns = tagDetectionPatterns;
    }

    internal string? DetectTitle(VideoMetadata videoData, string? defaultTitle = null)
    {
        return Detectors.DetectSingle<string>(videoData, Patterns.Title, null) ?? defaultTitle;
    }

    internal string? DetectArtist(VideoMetadata videoData, string? defaultArtist = null)
    {
        return Detectors.DetectSingle<string>(videoData, Patterns.Artist, null) ?? defaultArtist;
    }

    internal string? DetectAlbum(VideoMetadata videoData, string? defaultAlbum = null)
    {
        return Detectors.DetectSingle<string>(videoData, Patterns.Album, null) ?? defaultAlbum;
    }

    internal string? DetectComposers(VideoMetadata videoData)
    {
        return Detectors.DetectMultiple<string>(videoData, Patterns.Composer, null, "; ");
    }

    internal ushort? DetectReleaseYear(VideoMetadata videoData, ushort? defaultYear)
    {
        ushort detectedYear = Detectors.DetectSingle<ushort>(videoData, Patterns.Year, default);

        return detectedYear is default(ushort) // The default ushort value indicates no match was found.
            ? defaultYear
            : detectedYear;
    }
}
