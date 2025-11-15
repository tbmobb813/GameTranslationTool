using System;
using System.IO;
using System.Threading.Tasks;
using DiscUtils.Iso9660;            // For CDReader
using GameTranslationTool.ISO;      // IsoExtractor, IsoRepacker
using GameTranslationTool.Translation; // TranslationFileScanner, FakeTranslator
using Serilog;
using GameTranslationTool;

namespace GameTranslationTool.ConsoleTest
{
    class Program
    {
        static async Task Main()
        {
            // make sure our log folder exists
            Directory.CreateDirectory("logs");

            // Configure Serilog (requires Serilog.Sinks.Console & .File)
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Information("=== ISO Extractor Test ===");

            Console.Write("Enter full path to ISO file: ");
            string? isoPath = Console.ReadLine();

            Console.Write("Enter output folder: ");
            string? outputFolder = Console.ReadLine();

            Console.Write("Enter translated folder (optional): ");
            string? translatedFolder = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(isoPath) || string.IsNullOrWhiteSpace(outputFolder))
            {
                Log.Error("Error: Both ISO path and output folder must be provided.");
                return;
            }

            // read volume label
            string volumeLabel = "TRANSLATD_DISK";
            try
            {
                using var isoStream = File.OpenRead(isoPath);
                using var cdReader = new CDReader(isoStream, true);
                volumeLabel = cdReader.VolumeLabel ?? volumeLabel;
                Log.Information("Volume Label Detected: {Label}", volumeLabel);
            }
            catch (Exception ex)
            {
                Log.Warning("Could not read volume label; using default. ({Message})", ex.Message);
            }

            try
            {
                // 1) extract ISO
                IsoExtractor.ExtractIso(isoPath, outputFolder, translatedFolder ?? string.Empty);
                Log.Information("Extraction complete!");

                // 2) find all files we can translate
                var files = TranslationFileScanner.FindTranslatableFiles(outputFolder);
                if (files.Count == 0)
                {
                    Log.Information("No translatable files found.");
                }
                else
                {
                    Log.Information("Translatable files found:");
                    files.ForEach(f => Log.Information(" - {File}", f));

                    if (!string.IsNullOrWhiteSpace(translatedFolder))
                    {
                        Log.Information("Starting fake translation…");
                        foreach (var file in files)
                        {
                            // compute destination path under translatedFolder
                            var relative = Path.GetRelativePath(outputFolder, file);
                            var destPath = Path.Combine(translatedFolder, relative);
                            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

                            await FakeTranslator.TranslateFileAsync(file, destPath);
                            Log.Information("Translated: {File}", relative);
                        }
                        Log.Information("Fake translation complete.");
                    }
                }

                // 3) repack
                var repackSource = !string.IsNullOrWhiteSpace(translatedFolder)
                    ? translatedFolder!
                    : outputFolder!;

                var repackIsoPath = Path.Combine(outputFolder, "Repacked.iso");
                IsoRepacker.RepackIso(repackSource, repackIsoPath, volumeLabel);
                Log.Information("Repacked ISO created at: {Path}", repackIsoPath);
            }
            catch (Exception ex)
            {
                Log.Error("Unhandled error: {Message}", ex.Message);
            }
            finally
            {
                Log.Information("Press any key to exit…");
                Console.ReadKey();
                Log.CloseAndFlush();
            }
        }
    }
}
