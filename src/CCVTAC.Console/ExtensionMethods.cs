using System.IO;
using System.Text;

namespace CCVTAC.Console;

public static class ExtensionMethods
{
    /// <summary>
    /// Determines whether a string contains any text.
    /// </summary>
    /// <param name="maybeText"></param>
    /// <param name="allowWhiteSpace">Specifies whether whitespace characters should be considered as text.</param>
    /// <returns>Returns true if the string contains text; otherwise, false.</returns>
    public static bool HasText(this string? maybeText, bool allowWhiteSpace = false)
    {
        return allowWhiteSpace
            ? !string.IsNullOrEmpty(maybeText)
            : !string.IsNullOrWhiteSpace(maybeText);
    }

    /// <summary>
    /// Determines whether a collection is empty.
    /// </summary>
    public static bool None<T>(this IEnumerable<T> collection) => !collection.Any();

    /// <summary>
    /// Determines whether no elements of a sequence satisfy a given condition.
    /// </summary>
    public static bool None<T>(this IEnumerable<T> collection, Func<T, bool> predicate) =>
        !collection.Any(predicate);

    public static bool CaseInsensitiveContains(this IEnumerable<string> collection, string text) =>
        collection.Contains(text, new Comparers.CaseInsensitiveStringComparer());

    /// <summary>
    /// Returns a new string in which all invalid path characters for the current OS
    /// have been replaced by specified replacement character.
    /// Throws if the replacement character is an invalid path character.
    /// </summary>
    /// <param name="sourceText"></param>
    /// <param name="replaceWith"></param>
    /// <param name="customInvalidChars">Optional additional characters to consider invalid.</param>
    public static string ReplaceInvalidPathChars(
        this string sourceText,
        char replaceWith = '_',
        char[]? customInvalidChars = null
    )
    {
        var invalidChars = Path.GetInvalidFileNameChars()
            .Concat(Path.GetInvalidPathChars())
            .Concat(
                new[]
                {
                    Path.PathSeparator,
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar,
                    Path.VolumeSeparatorChar,
                }
            )
            .Concat(customInvalidChars ?? Enumerable.Empty<char>())
            .ToFrozenSet();

        if (invalidChars.Contains(replaceWith))
            throw new ArgumentException(
                $"The replacement char ('{replaceWith}') must be a valid path character."
            );

        return invalidChars.Aggregate(
            new StringBuilder(sourceText),
            (workingText, ch) => workingText.Replace(ch, replaceWith),
            (workingText) => workingText.ToString()
        );
    }

    public static string TrimTerminalLineBreak(this string text) =>
        text.HasText() ? text.TrimEnd(Environment.NewLine.ToCharArray()) : text;
}
