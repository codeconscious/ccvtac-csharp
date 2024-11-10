using CCVTAC.Console.ExternalTools;

namespace CCVTAC.Console.PostProcessing;

internal static class ImageProcessor
{
    internal static void Run(string workingDirectory, Printer printer)
    {
        ExternalTool imageProgram =
            new("mogrify", "https://imagemagick.org/script/mogrify.php", "image cropping");

        ToolSettings imageEditToolSettings =
            new(imageProgram, "-trim -fuzz 10% *.jpg", workingDirectory);

        Runner.Run(imageEditToolSettings, [], printer);
    }
}
