using System.Text.RegularExpressions;

namespace CCVTAC.Console;

public static partial class InputHelper
{
    internal static readonly string Prompt =
        $"Enter one or more YouTube media URLs or commands (or \"{Commands.SummaryCommand}\"):\n▶︎";

    /// <summary>
    /// A regular expression that detects where commands and URLs begin in input strings.
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
        var matches = UserInputRegex().Matches(input).OfType<Match>().ToImmutableArray();

        if (matches.Length == 0)
        {
            return [];
        }

        if (matches.Length == 1)
        {
            return [input];
        }

        var startIndices = matches.Select(m => m.Index).ToImmutableArray();

        var indexPairs = startIndices.Select(
            (startIndex, iterIndex) =>
            {
                int endIndex =
                    iterIndex == startIndices.Length - 1
                        ? input.Length
                        : startIndices[iterIndex + 1];

                return new IndexPair(startIndex, endIndex);
            }
        );

        var splitInputs = indexPairs.Select(p => input[p.Start..p.End].Trim()).Distinct();

        return [.. splitInputs];
    }

    internal enum InputCategory
    {
        Url,
        Command,
    }

    internal record CategorizedInput(string Text, InputCategory Category);

    internal static ImmutableArray<CategorizedInput> CategorizeInputs(ICollection<string> inputs)
    {
        return
        [
            .. inputs.Select(input => new CategorizedInput(
                input,
                input.StartsWith(Commands.Prefix) ? InputCategory.Command : InputCategory.Url
            )),
        ];
    }

    internal class CategoryCounts
    {
        private readonly Dictionary<InputCategory, int> _counts;

        internal CategoryCounts(Dictionary<InputCategory, int> counts)
        {
            _counts = counts;
        }

        public int this[InputCategory category] => _counts.GetValueOrDefault(category, 0);
    }

    internal static CategoryCounts CountCategories(ICollection<CategorizedInput> inputs)
    {
        var counts = inputs.GroupBy(i => i.Category).ToDictionary(gr => gr.Key, gr => gr.Count());

        return new CategoryCounts(counts);
    }
}
