using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Serilog;
using DiscUtils.Iso9660;
using GameTranslationTool.Utils;

namespace GameTranslationTool.ISO
{
    public static class IsoExtractor
    {
        // Logger scoped to this class
        private static readonly ILogger Log = Serilog.Log.ForContext(typeof(IsoExtractor));

        /// <summary>
        /// Progress callback for extraction operations
        /// </summary>
        public delegate void ProgressCallback(int current, int total, string currentFile);

        /// <summary>
        /// Extracts an ISO to disk with optional progress reporting
        /// </summary>
        public static void ExtractIso(
            string isoPath,
            string outputFolder,
            string translatedFolder,
            ProgressCallback? progressCallback = null)
        {
            if (!File.Exists(isoPath))
            {
                Log.Error("ISO file not found: {Path}", isoPath);
                throw new FileNotFoundException($"ISO file not found: {isoPath}", isoPath);
            }

            Log.Information("Starting ISO extraction from {Path}", isoPath);

            using var isoStream = File.OpenRead(isoPath);
            var cdReader = new CDReader(isoStream, true);

            // First pass: count total files
            int totalFiles = CountFiles(cdReader, @"\");
            int processedFiles = 0;

            var translatableFiles = new List<string>();
            ExtractDirectory(
                cdReader,
                @"\",
                outputFolder,
                translatedFolder,
                translatableFiles,
                ref processedFiles,
                totalFiles,
                progressCallback);

            Log.Information("Finished extracting {Count} files to {Folder}", processedFiles, outputFolder);

            string logFilePath = Path.Combine(outputFolder, "TranslatableFiles.txt");
            File.WriteAllLines(logFilePath, translatableFiles);
            Log.Information("Wrote translatable file list to: {Path}", logFilePath);

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "notepad.exe",
                    Arguments = logFilePath,
                    UseShellExecute = true
                });
                Log.Information("Opened TranslatableFiles.txt in Notepad.");
            }
            catch (Exception ex)
            {
                Log.Warning("Could not open log file in Notepad: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// Counts total files in the ISO for progress reporting
        /// </summary>
        private static int CountFiles(CDReader cdReader, string sourcePath)
        {
            int count = 0;

            count += cdReader.GetFiles(sourcePath).Length;

            foreach (var dir in cdReader.GetDirectories(sourcePath))
            {
                count += CountFiles(cdReader, dir);
            }

            return count;
        }

        /// <summary>
        /// Recursively extracts files and logs those that are translatable
        /// </summary>
        private static void ExtractDirectory(
            CDReader cdReader,
            string sourcePath,
            string outputFolder,
            string translatedRoot,
            List<string> translatableFiles,
            ref int processedFiles,
            int totalFiles,
            ProgressCallback? progressCallback)
        {
            foreach (var file in cdReader.GetFiles(sourcePath))
            {
                string relativePath = file.TrimStart('\\');
                string destPath = Path.Combine(outputFolder, relativePath);
                string? dir = Path.GetDirectoryName(destPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                using var src = cdReader.OpenFile(file, FileMode.Open);
                using var dst = File.Create(destPath);
                src.CopyTo(dst);

                processedFiles++;
                progressCallback?.Invoke(processedFiles, totalFiles, relativePath);

                Log.Debug("Extracted file: {Path}", destPath);

                if (FileHelpers.IsTranslatableFile(destPath))
                {
                    translatableFiles.Add(relativePath);
                    Log.Information("Detected translatable file: {Path}", relativePath);

                    // Note: Removed automatic translation - should be done manually
                    // to avoid accidental translations during extraction
                }
            }

            foreach (var dir in cdReader.GetDirectories(sourcePath))
            {
                ExtractDirectory(
                    cdReader,
                    dir,
                    outputFolder,
                    translatedRoot,
                    translatableFiles,
                    ref processedFiles,
                    totalFiles,
                    progressCallback);
            }
        }
    }
}
