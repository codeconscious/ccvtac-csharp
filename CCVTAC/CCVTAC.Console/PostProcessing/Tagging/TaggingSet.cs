using System.Text.RegularExpressions;

namespace CCVTAC.Console.PostProcessing.Tagging;

/// <summary>
/// Contains all of the data necessary for tagging a related set of files.
/// </summary>
/// <remarks>
/// Files are "related" if they share the same resource ID. Generally, only a single downloaded video
/// has a certain video ID, but if the split-chapter option is used, all the child videos that were
/// split out will also have the same resource ID.
/// </remarks>
internal readonly record struct TaggingSet
{
    /// <summary>
    /// The ID of a single video and perhaps its child video (if "split chapters" was used).
    /// </summary>
    internal string ResourceId { get; init; }

    /// <summary>
    /// All audio files for the associated resource ID. Several indicate
    /// that the original video was split into several audio files.
    /// </summary>
    internal ImmutableHashSet<string> AudioFilePaths { get; init; }

    /// <summary>
    /// The path to the JSON file containing metadata related to the source video.
    /// </summary>
    internal string JsonFilePath { get; init; }

    /// <summary>
    /// The path to the image file containing the thumbnail related to the source video.
    /// </summary>
    internal string ImageFilePath { get; init; }

    /// <summary>
    /// A regex that finds all files whose filename includes a video ID.
    /// Group 1 contains the video ID itself.
    /// </summary>
    internal static Regex FileNamesWithVideoIdsRegex = new(@".+\[([\w_\-]{11})\](?:.*)?\.(\w+)");

    internal TaggingSet(string              resourceId,
                        IEnumerable<string> audioFilePaths,
                        string              jsonFilePath,
                        string              imageFilePath)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
            throw new ArgumentException("The resource ID must be provided.");
        if (string.IsNullOrWhiteSpace(jsonFilePath))
            throw new ArgumentException("The JSON file path must be provided.");
        if (string.IsNullOrWhiteSpace(imageFilePath))
            throw new ArgumentException("The image file path must be provided.");
        if (audioFilePaths?.Any() != true)
            throw new ArgumentException("At least one audio file path must be provided.", nameof(audioFilePaths));

        ResourceId = resourceId.Trim();
        AudioFilePaths = audioFilePaths.ToImmutableHashSet();
        JsonFilePath = jsonFilePath.Trim();
        ImageFilePath = imageFilePath.Trim();
    }

    /// <summary>
    /// Create a collection of TaggingSets from a collection of filePaths
    /// related to several video IDs.
    /// </summary>
    /// <param name="filePaths">
    /// A collection of file paths. Expected to contain 1 JSON file and
    /// 1 image file for each unique video ID.
    /// </param>
    internal static ImmutableList<TaggingSet> CreateTaggingSets(IEnumerable<string> filePaths)
    {
        if (filePaths is null || !filePaths.Any())
            return Enumerable.Empty<TaggingSet>().ToImmutableList();

        const string jsonFileExtension = ".json";
        const string audioFileExtension = ".m4a"; // TODO: Support multiple formats (maybe).
        const string imageFileExtension = ".jpg";

        return filePaths
                    .Select(f => FileNamesWithVideoIdsRegex.Match(f))
                    .Where(m => m.Success)
                    .Select(m => m.Captures.OfType<Match>().First())
                    .GroupBy(m => m.Groups[1].Value, // Video ID
                             m => m.Groups[0].Value) // Full filename
                    .Where(gr => // Ensure the correct count of image and JSON files.
                        gr.Count(f => f.EndsWith(jsonFileExtension, StringComparison.OrdinalIgnoreCase)) == 1 &&
                        gr.Count(f => f.EndsWith(imageFileExtension, StringComparison.OrdinalIgnoreCase)) == 1)
                    .Select(gr => {
                        return new TaggingSet(
                            gr.Key,
                            gr.Where(f => f.EndsWith(audioFileExtension)),
                            gr.Where(f => f.EndsWith(jsonFileExtension)).First(),
                            gr.Where(f => f.EndsWith(imageFileExtension)).First()
                        );
                    })
                    .ToImmutableList();
    }
}