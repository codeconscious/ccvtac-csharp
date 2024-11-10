using Spectre.Console;

namespace CCVTAC.Console;

public sealed class Printer
{
    private enum Level
    {
        Critical,
        Error,
        Warning,
        Info,
        Debug,
    }

    private record ColorFormat(string? Foreground, string? Background, bool Bold = false);

    /// <summary>
    /// Color reference: https://spectreconsole.net/appendix/colors
    /// </summary>
    private static readonly Dictionary<Level, ColorFormat> Colors =
        new()
        {
            { Level.Critical, new("white", "red3", true) },
            { Level.Error, new("red", null) },
            { Level.Warning, new("yellow", null) },
            { Level.Info, new(null, null) },
            { Level.Debug, new("grey70", null) },
        };

    private Level MinimumLogLevel { get; set; }

    public Printer(bool showDebug)
    {
        MinimumLogLevel = showDebug ? Level.Debug : Level.Info;
    }

    public void ShowDebug(bool show)
    {
        MinimumLogLevel = show ? Level.Debug : Level.Info;
    }

    /// <summary>
    ///
    /// </summary>
    private static string EscapeText(string text) =>
        text.Replace("{", "{{").Replace("}", "}}").Replace("[", "[[").Replace("]", "]]");

    private static string AddMarkup(string message, ColorFormat colors)
    {
        if (colors.Foreground is null && colors.Background is null && !colors.Bold)
        {
            return message;
        }

        var bold = colors.Bold ? "bold " : string.Empty;
        var fg = colors.Foreground ?? "default";
        var bg = colors.Background is null ? string.Empty : $" on {colors.Background}";
        var markUp = $"{bold}{fg}{bg}";

        return $"[{markUp}]{message}[/]";
    }

    private void Print(
        Level logLevel,
        string message,
        bool appendLineBreak = true,
        byte prependLines = 0,
        byte appendLines = 0,
        bool processMarkup = true
    )
    {
        if (logLevel > MinimumLogLevel)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentNullException(nameof(message), "Message cannot be empty.");
        }

        EmptyLines(prependLines);

        var escapedMessage = EscapeText(message);
        if (processMarkup)
        {
            var markedUpMessage = AddMarkup(escapedMessage, Colors[logLevel]);
            AnsiConsole.Markup(markedUpMessage);
        }
        else
        {
            // `AnsiConsole.Write()` calls an internal function that uses format strings,
            // so we must duplicate any curly brackets to safely escape the message text.
            // See https://github.com/spectreconsole/spectre.console/issues/1495.
            AnsiConsole.Write(escapedMessage);
        }

        if (appendLineBreak)
        {
            AnsiConsole.WriteLine();
        }

        EmptyLines(appendLines);
    }

    public void PrintTable(Table table)
    {
        AnsiConsole.Write(table);
    }

    public void Critical(
        string message,
        bool appendLineBreak = true,
        byte prependLines = 0,
        byte appendLines = 0,
        bool processMarkup = true
    )
    {
        Print(Level.Critical, message, appendLineBreak, prependLines, appendLines, processMarkup);
    }

    public void Error(
        string message,
        bool appendLineBreak = true,
        byte prependLines = 0,
        byte appendLines = 0,
        bool processMarkup = true
    )
    {
        Print(Level.Error, message, appendLineBreak, prependLines, appendLines, processMarkup);
    }

    public void Errors(ICollection<string> errors, byte appendLines = 0)
    {
        if (errors.Count == 0)
            throw new ArgumentException("No errors were provided!", nameof(errors));

        foreach (var error in errors.Where(e => e.HasText()))
        {
            Error(error);
        }

        EmptyLines(appendLines);
    }

    private void Errors(string headerMessage, IEnumerable<string> errors)
    {
        Errors([headerMessage, .. errors]);
    }

    public void Errors<T>(Result<T> failResult, byte appendLines = 0)
    {
        Errors(failResult.Errors.Select(e => e.Message).ToList(), appendLines);
    }

    public void Errors<T>(string headerMessage, Result<T> failingResult)
    {
        Errors(headerMessage, failingResult.Errors.Select(e => e.Message));
    }

    public void FirstError(IResultBase failResult, string? prepend = null)
    {
        string pre = prepend is null ? string.Empty : $"{prepend} ";
        string message = failResult.Errors?.FirstOrDefault()?.Message ?? string.Empty;

        Error($"{pre}{message}");
    }

    public void Warning(
        string message,
        bool appendLineBreak = true,
        byte prependLines = 0,
        byte appendLines = 0,
        bool processMarkup = true
    )
    {
        Print(Level.Warning, message, appendLineBreak, prependLines, appendLines, processMarkup);
    }

    public void Info(
        string message,
        bool appendLineBreak = true,
        byte prependLines = 0,
        byte appendLines = 0,
        bool processMarkup = true
    )
    {
        Print(Level.Info, message, appendLineBreak, prependLines, appendLines, processMarkup);
    }

    public void Debug(
        string message,
        bool appendLineBreak = true,
        byte prependLines = 0,
        byte appendLines = 0,
        bool processMarkup = true
    )
    {
        Print(Level.Debug, message, appendLineBreak, prependLines, appendLines, processMarkup);
    }

    /// <summary>
    /// Prints the requested number of blank lines.
    /// </summary>
    /// <param name="count"></param>
    public void EmptyLines(byte count)
    {
        if (count == 0)
            return;

        AnsiConsole.WriteLine(string.Concat(Enumerable.Repeat(Environment.NewLine, count - 1)));
    }

    public string GetInput(string prompt)
    {
        EmptyLines(1);
        return AnsiConsole.Ask<string>($"[skyblue1]{prompt}[/]");
    }

    private string Ask(string title, string[] options)
    {
        return AnsiConsole.Prompt(new SelectionPrompt<string>().Title(title).AddChoices(options));
    }

    public bool AskToBool(string title, string trueAnswer, string falseAnswer) =>
        Ask(title, [trueAnswer, falseAnswer]) == trueAnswer;
}
