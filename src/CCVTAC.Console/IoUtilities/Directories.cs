using System.IO;
using System.Text;
using CCVTAC.Console.PostProcessing;

namespace CCVTAC.Console.IoUtilities;

internal static class Directories
{
    private const string AllFilesSearchPattern = "*";
    private static readonly EnumerationOptions EnumerationOptions = new();

    internal static int AudioFileCount(string directory)
    {
        return new DirectoryInfo(directory)
            .EnumerateFiles()
            .Count(f => PostProcessor.AudioExtensions.CaseInsensitiveContains(f.Extension));
    }

    internal static Result WarnIfAnyFiles(string directory, int showMax)
    {
        var fileNames = GetDirectoryFileNames(directory);
        var fileCount = fileNames.Length;

        if (fileNames.IsEmpty)
        {
            return Result.Ok();
        }

        var fileLabel = fileCount == 1 ? "file" : "files";
        var report = new StringBuilder(
            $"Found {fileCount} {fileLabel} in working directory \"{directory}\":{Environment.NewLine}"
        );

        foreach (string fileName in fileNames.Take(showMax))
        {
            report.AppendLine($"• {fileName}");
        }

        if (fileCount > showMax)
        {
            report.AppendLine($"... plus {fileCount - showMax} more.");
        }

        return Result.Fail(report.ToString());
    }

    private static Result<int> DeleteAllFiles(string workingDirectory, int showMaxErrors)
    {
        var fileNames = GetDirectoryFileNames(workingDirectory);

        int successCount = 0;
        var errors = new List<string>();

        foreach (var fileName in fileNames)
        {
            try
            {
                File.Delete(fileName);
                successCount++;
            }
            catch (Exception ex)
            {
                errors.Add(ex.Message);
            }
        }

        if (errors.Count == 0)
        {
            return Result.Ok(successCount);
        }

        var output = new StringBuilder(
            $"While {successCount} files were deleted successfully, some files could not be deleted:"
        );
        foreach (string error in errors.Take(showMaxErrors))
        {
            output.AppendLine($"• {error}");
        }

        if (errors.Count > showMaxErrors)
        {
            output.AppendLine($"... plus {errors.Count - showMaxErrors} more.");
        }

        return Result.Fail(output.ToString());
    }

    internal static Result<int> AskToDeleteAllFiles(string workingDirectory, Printer printer)
    {
        bool doDelete = printer.AskToBool("Delete all temporary files?", "Yes", "No");

        return doDelete
            ? DeleteAllFiles(workingDirectory, 10)
            : Result.Fail("Will not delete the files.");
    }

    /// <summary>
    /// Returns the filenames in a given directory, optionally ignoring specific filenames.
    /// </summary>
    /// <param name="directoryName"></param>
    /// <param name="customIgnoreFiles">An optional list of files to be excluded.</param>
    private static ImmutableArray<string> GetDirectoryFileNames(
        string directoryName,
        IEnumerable<string>? customIgnoreFiles = null
    )
    {
        var ignoreFiles = customIgnoreFiles?.Distinct() ?? [];

        return
        [
            .. Directory
                .GetFiles(directoryName, AllFilesSearchPattern, EnumerationOptions)
                .Where(filePath => ignoreFiles.None(filePath.EndsWith)),
        ];
    }
}
