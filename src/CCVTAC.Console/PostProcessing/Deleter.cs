using System.IO;

namespace CCVTAC.Console.PostProcessing;

internal static class Deleter
{
    internal static void Run(
        IReadOnlyCollection<string> taggingSetFileNames,
        CollectionMetadata? collectionMetadata,
        string workingDirectory,
        Printer printer
    )
    {
        ImmutableList<string> collectionFileNames;
        var getFileResult = GetCollectionFiles(collectionMetadata, workingDirectory);
        if (getFileResult.IsSuccess)
        {
            var files = getFileResult.Value;
            collectionFileNames = files;

            printer.Debug($"Found {files.Count} collection files.");
        }
        else
        {
            collectionFileNames = [];
            printer.Warning(getFileResult.Errors.First().Message);
        }

        var allFileNames = taggingSetFileNames.Concat(collectionFileNames).ToImmutableList();

        if (allFileNames.IsEmpty)
        {
            printer.Warning("No files to delete were found.");
            return;
        }

        printer.Debug($"Deleting {allFileNames.Count} temporary files...");

        DeleteAll(allFileNames, printer);

        printer.Info("Deleted temporary files.");
    }

    private static Result<ImmutableList<string>> GetCollectionFiles(
        CollectionMetadata? collectionMetadata,
        string workingDirectory
    )
    {
        if (collectionMetadata is null)
            return Result.Ok(ImmutableList<string>.Empty);

        try
        {
            var id = collectionMetadata.Value.Id;
            return Directory.GetFiles(workingDirectory, $"*{id}*").ToImmutableList();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error collecting filenames: {ex.Message}");
        }
    }

    private static void DeleteAll(IEnumerable<string> fileNames, Printer printer)
    {
        foreach (var fileName in fileNames)
        {
            try
            {
                File.Delete(fileName);

                printer.Debug($"• Deleted \"{fileName}\"");
            }
            catch (Exception ex)
            {
                printer.Error($"• Deletion error: {ex.Message}");
            }
        }
    }
}
