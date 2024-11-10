using System.IO;
using System.Text.RegularExpressions;

namespace CCVTAC.Console.PostProcessing.Tagging;

/// <summary>
/// Contains all the data necessary for tagging a related set of files.
/// </summary>
/// <remarks>
/// Files are "related" if they share the same resource ID. Generally, only a single downloaded video
/// has a certain video ID, but if the split-chapter option is used, all the child videos that were
/// split out will also have the same resource ID.
/// </remarks>
internal readonly record struct TaggingSet
{
    /// <summary>
    /// The ID of a single video and perhaps its child videos (if "split chapters" was used).
    /// Used to locate all the related files (whose filenames will contain the same ID).
    /// </summary>
    internal string ResourceId { get; init; }

    /// <summary>
    /// All audio files for the associated resource ID. Several files with identical IDs indicates
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

    internal IReadOnlyList<string> AllFiles => [.. AudioFilePaths, JsonFilePath, ImageFilePath];

    /// <summary>
    /// A regex that finds all files whose filename includes a video ID.
    /// Group 1 contains the video ID itself.
    /// </summary>
    private static readonly Regex FileNamesWithVideoIdsRegex =
        new(@".+\[([\w_\-]{11})\](?:.*)?\.(\w+)");

    private TaggingSet(
        string resourceId,
        ICollection<string> audioFilePaths,
        string jsonFilePath,
        string imageFilePath
    )
    {
        if (string.IsNullOrWhiteSpace(resourceId))
            throw new ArgumentException("The resource ID must be provided.");
        if (string.IsNullOrWhiteSpace(jsonFilePath))
            throw new ArgumentException("The JSON file path must be provided.");
        if (string.IsNullOrWhiteSpace(imageFilePath))
            throw new ArgumentException("The image file path must be provided.");
        if (audioFilePaths.Count == 0)
            throw new ArgumentException(
                "At least one audio file path must be provided.",
                nameof(audioFilePaths)
            );

        ResourceId = resourceId.Trim();
        AudioFilePaths = audioFilePaths.ToImmutableHashSet();
        JsonFilePath = jsonFilePath.Trim();
        ImageFilePath = imageFilePath.Trim();
    }

    /// <summary>
    ///     Create a collection of TaggingSets from a collection of file paths
    ///     related to several video IDs. Files that don't match the requirements
    ///     will be ignored.
    /// </summary>
    /// <param name="filePaths">
    ///     A collection of file paths. Expected to contain all related audio files (>=1),
    ///     1 JSON file, and 1 image file for each distinct video ID.
    /// </param>
    /// <remarks>Does not include collection (playlist or channel) metadata files.</remarks>
    internal static ImmutableList<TaggingSet> CreateSets(ICollection<string> filePaths)
    {
        if (filePaths.None())
        {
            return Enumerable.Empty<TaggingSet>().ToImmutableList();
        }

        const string jsonFileExt = ".json";
        const string imageFileExt = ".jpg";

        return filePaths
            // First, get regex matches of all files whose filenames contain a video ID regex.
            .Select(f => FileNamesWithVideoIdsRegex.Match(f))
            .Where(m => m.Success)
            .Select(m => m.Captures.OfType<Match>().First())
            // Then, group those files as key-value pairs using the video ID as the key.
            .GroupBy(
                m => m.Groups[1].Value, // Video ID
                m => m.Groups[0].Value
            ) // Full filenames (1 or more for each video ID)
            // Next, ensure the correct count of image and JSON files, ignoring those that don't match.
            // (For thought: It might be an option to track and report the invalid ones as well.)
            .Where(gr =>
                gr.Any(f =>
                    PostProcessor.AudioExtensions.CaseInsensitiveContains(Path.GetExtension(f))
                )
                && gr.Count(f => f.EndsWith(jsonFileExt, StringComparison.OrdinalIgnoreCase)) == 1
                && gr.Count(f => f.EndsWith(imageFileExt, StringComparison.OrdinalIgnoreCase)) == 1
            )
            // Lastly, group everything into new TaggingSets.
            .Select(gr =>
            {
                return new TaggingSet(
                    gr.Key, // Video ID
                    gr.Where(f =>
                            PostProcessor.AudioExtensions.CaseInsensitiveContains(
                                Path.GetExtension(f)
                            )
                        )
                        .ToList(),
                    gr.Single(f => f.EndsWith(jsonFileExt)),
                    gr.Single(f => f.EndsWith(imageFileExt))
                );
            })
            .ToImmutableList();
    }
}
