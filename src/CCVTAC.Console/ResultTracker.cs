namespace CCVTAC.Console;

/// <summary>
/// Tracks the successes and failures of various operations.
/// Successes are only counted; failure error messages are tracked.
/// </summary>
internal sealed class ResultTracker<T>
{
    private nuint _successCount;
    private readonly Dictionary<string, string> _failures = [];
    private readonly Printer _printer;

    private static string CombineErrors(Result<T> result) =>
        string.Join(" / ", result.Errors.Select(e => e.Message));

    internal ResultTracker(Printer printer)
    {
        ArgumentNullException.ThrowIfNull(printer);
        _printer = printer;
    }

    /// <summary>
    /// Logs the result for a specific corresponding input.
    /// </summary>
    internal void RegisterResult(string input, Result<T> result)
    {
        if (result.IsSuccess)
        {
            _successCount++;
            return;
        }

        string errors = CombineErrors(result);
        if (!_failures.TryAdd(input, errors))
        {
            // Keep only the latest error for each input.
            _failures[input] = errors;
        }
    }

    /// <summary>
    /// Prints any failures for the current batch.
    /// </summary>
    internal void PrintBatchFailures()
    {
        if (_failures.Count == 0)
        {
            _printer.Debug("No failures in batch.");
            return;
        }

        var failureLabel = _failures.Count == 1 ? "failure" : "failures";
        _printer.Info($"{_failures.Count} {failureLabel} in this batch:");

        foreach (var (url, error) in _failures)
        {
            _printer.Warning($"- {url}: {error}");
        }
    }

    /// <summary>
    /// Prints the output for the current application session.
    /// Expected to be used upon quitting.
    /// </summary>
    internal void PrintSessionSummary()
    {
        var successLabel = _successCount == 1 ? "success" : "successes";
        var failureLabel = _failures.Count == 1 ? "failure" : "failures";

        _printer.Info(
            $"Quitting with {_successCount} {successLabel} and {_failures.Count} {failureLabel}."
        );

        foreach (var (url, error) in _failures)
        {
            _printer.Warning($"- {url}: {error}");
        }
    }
}
