using System.IO;
using System.Text.Json;
using Spectre.Console;

namespace CCVTAC.Console;

/// <summary>
/// Handles storing, retrieving, and (eventually) analyzing data relating
/// to URLs that the user has entered.
/// </summary>
public class History
{
    private const char Separator = ';';
    private string FilePath { get; init; }
    private byte DisplayCount { get; init; }

    public History(string filePath, byte displayCount)
    {
        FilePath = filePath;
        DisplayCount = displayCount;
    }

    /// <summary>
    /// Add a URL and related data to the history file.
    /// </summary>
    public void Append(string url, DateTime entryTime, Printer printer)
    {
        try
        {
            string serializedEntryTime = JsonSerializer.Serialize(entryTime).Replace("\"", "");
            File.AppendAllText(
                FilePath,
                serializedEntryTime + Separator + url + Environment.NewLine
            );

            printer.Debug($"Added \"{url}\" to the history log.");
        }
        catch (Exception ex)
        {
            printer.Error($"Could not append URL(s) to history log: " + ex.Message);
        }
    }

    public void ShowRecent(Printer printer)
    {
        try
        {
            IEnumerable<IGrouping<DateTime, string>> historyData = File.ReadAllLines(FilePath)
                .TakeLast(DisplayCount)
                .Select(line => line.Split(Separator))
                .Where(lineItems => lineItems.Length == 2) // Only lines with date-times
                .GroupBy(line => DateTime.Parse(line[0]), line => line[1]);

            Table table = new();
            table.Border(TableBorder.None);
            table.AddColumns("Time", "URL");
            table.Columns[0].PadRight(3);

            foreach (IGrouping<DateTime, string> thisDate in historyData)
            {
                var formattedTime = $"{thisDate.Key:yyyy-MM-dd HH:mm:ss}";
                var urls = string.Join(Environment.NewLine, thisDate);
                table.AddRow(formattedTime, urls);
            }

            printer.PrintTable(table);
        }
        catch (Exception ex)
        {
            printer.Error($"Could not display recent history: {ex.Message}");
        }
    }
}
