﻿using System;
using System.IO;

namespace CCVTAC.Console.PostProcessing;

internal static class Mover
{
    internal static void Run(string workingDirectory, string moveToDirectory, Printer printer)
    {
        uint movedCount = 0;
        var dir = new DirectoryInfo(workingDirectory);
        printer.Print($"Moving audio files to \"{moveToDirectory}\"...");
        foreach (var file in dir.EnumerateFiles("*.m4a"))
        {
            try
            {
                File.Move(
                    file.FullName,
                    $"{Path.Combine(moveToDirectory, file.Name)}",
                    true);
                printer.Print($"- Moved \"{file.Name}\"");
                movedCount++;
            }
            catch (Exception ex)
            {
                printer.Error($"- Error moving file \"{file.Name}\": {ex.Message}");
            }
        }
        printer.Print($"{movedCount} file(s) moved.");
    }
}
