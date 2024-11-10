using System.Text.RegularExpressions;
using static CCVTAC.FSharp.Settings;

namespace CCVTAC.Console.PostProcessing.Tagging;

/// <summary>
/// Detection of specific text within video metadata files.
/// </summary>
internal static class Detectors
{
    /// <summary>
    /// Finds and returns the first instance of text matching a given detection scheme pattern,
    /// parsing into T if necessary.
    /// </summary>
    /// <returns>A match of type T if there was a match; otherwise, the default value provided.</returns>
    internal static T? DetectSingle<T>(
        VideoMetadata videoMetadata,
        IEnumerable<TagDetectionPattern> patterns,
        T? defaultValue
    )
    {
        foreach (TagDetectionPattern pattern in patterns)
        {
            string fieldText = ExtractMetadataText(videoMetadata, pattern.SearchField);

            // TODO: Instantiate regexes during settings deserialization.
            var match = new Regex(pattern.RegexPattern).Match(fieldText);

            if (!match.Success)
            {
                continue;
            }

            string matchedText = match.Groups[pattern.MatchGroup].Value.Trim();
            return Cast(matchedText, defaultValue);
        }

        return defaultValue; // No matches were found.
    }

    /// <summary>
    /// Finds and returns all instances of text matching a given detection scheme pattern,
    /// concatenating them into a single string (using a custom separator), then casting
    /// to type T if necessary.
    /// </summary>
    /// <returns>A match of type T if there were any matches; otherwise, the default value provided.</returns>
    internal static T? DetectMultiple<T>(
        VideoMetadata data,
        IEnumerable<TagDetectionPattern> patterns,
        T? defaultValue,
        string separator
    )
    {
        HashSet<string> matchedValues = [];

        foreach (TagDetectionPattern pattern in patterns)
        {
            string fieldText = ExtractMetadataText(data, pattern.SearchField);

            // TODO: Instantiate regexes during settings deserialization.
            var matches = new Regex(pattern.RegexPattern).Matches(fieldText);

            foreach (Match match in matches.Where(m => m.Success))
            {
                matchedValues.Add(match.Groups[pattern.MatchGroup].Value.Trim());
            }
        }

        if (matchedValues.Count == 0)
        {
            return defaultValue;
        }

        string joinedMatchedText = string.Join(separator, matchedValues);
        return Cast(joinedMatchedText, defaultValue);
    }

    /// <summary>
    /// Attempts casting the input text to type T and returning it.
    /// If casting fails, the default value is returned instead.
    /// </summary>
    private static T? Cast<T>(string? text, T? defaultValue)
    {
        if (text is T)
        {
            return (T)(object)text;
        }

        try
        {
            return (T?)Convert.ChangeType(text, typeof(T));
        }
        catch (InvalidCastException)
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Extracts the value of the specified tag field from the given data.
    /// </summary>
    /// <param name="metadata"></param>
    /// <param name="fieldName">The name of the field within the video metadata to read.</param>
    /// <returns>The text content of the requested field of the video metadata.</returns>
    private static string ExtractMetadataText(VideoMetadata metadata, string fieldName)
    {
        return fieldName switch
        {
            "title" => metadata.Title,
            "description" => metadata.Description,

            // TODO: It would be best to check for invalid entries upon settings deserialization.
            _ => throw new ArgumentException(
                $"\"{fieldName}\" is an invalid video metadata field name."
            ),
        };
    }
}
