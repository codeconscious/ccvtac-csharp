using System.Text.RegularExpressions;

namespace CCVTAC.Console;

public static partial class InputHelper
{
    // TODO: Refactor this?
    internal static string Prompt =
        $"Enter one or more YouTube media URLs, {Commands._history[0]}, or {Commands._quit[0]} ({Commands._quit[1]}):\n▶︎";

    /// <summary>
    /// A regular expression that detects where input commands and URLs begin in input strings.
    /// </summary>
    [GeneratedRegex("""(?:https:|\\)""")]
    private static partial Regex UserInputRegex();

    private record IndexPair(int Start, int End);

    /// <summary>
    /// Takes a user input string and splits it into a collection of inputs based
    /// upon substrings detected by the class's regular expression pattern.
    /// </summary>
    public static ImmutableArray<string> SplitInput(string input)
    {
        var matches = UserInputRegex()
            .Matches(input)
            .OfType<Match>()
            .ToImmutableArray();

        if (matches.Length == 0)
        {
            return [];
        }

        if (matches.Length == 1)
        {
            return [input];
        }

        var startIndices = matches
            .Select(m => m.Index)
            .ToImmutableArray();

        var indexPairs = startIndices
            .Select((startIndex, iterIndex) =>
                {
                    int endIndex = iterIndex == startIndices.Length - 1
                        ? input.Length
                        : startIndices[iterIndex + 1];

                    return new IndexPair(startIndex, endIndex);
                });

        var splitInputs = indexPairs
            .Select(p => input[p.Start..p.End].Trim())
            .Distinct();

        return [.. splitInputs];
    }

    internal enum InputType { Url, Command }

    internal record CategorizedInput(string Text, InputType InputType);

    internal static ImmutableArray<CategorizedInput> CategorizeInputs(ICollection<string> splitInputs)
    {
        return
            [
                ..splitInputs
                    .Select(input =>
                        new CategorizedInput(
                            input,
                            input.StartsWith(Commands.Prefix)
                                ? InputType.Command
                                : InputType.Url))
            ];
    }

    internal static Dictionary<InputType, int> CountCategories(ICollection<CategorizedInput> inputs)
    {
        return
            inputs
                .GroupBy(i => i.InputType)
                .ToDictionary(gr => gr.Key, gr => gr.Count());
    }
}
