using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Serilog;
using DiscUtils.Iso9660;
using GameTranslationTool.Utils;
using GameTranslationTool.Translation;

namespace GameTranslationTool.ISO
{
    public static class IsoExtractor
    {
        // Logger scoped to this class
        private static readonly ILogger Log = Serilog.Log.ForContext(typeof(IsoExtractor));

        /// <summary>
        /// Extracts an ISO to disk, logs translatable files, and opens the log in Notepad.
        /// </summary>
        public static void ExtractIso(string isoPath, string outputFolder, string translatedFolder)
        {
            if (!File.Exists(isoPath))
            {
                Log.Error("ISO file not found: {Path}", isoPath);
                throw new FileNotFoundException($"ISO file not found: {isoPath}", isoPath);
            }

            Log.Information("Starting ISO extraction from {Path}", isoPath);

            using var isoStream = File.OpenRead(isoPath);
            var cdReader = new CDReader(isoStream, true);

            var translatableFiles = new List<string>();
            ExtractDirectory(cdReader, @"\", outputFolder, translatedFolder, translatableFiles);

            Log.Information("Finished extracting all files to {Folder}", outputFolder);

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
        /// Recursively extracts files and logs/copies those that are translatable.
        /// </summary>
        private static void ExtractDirectory(
            CDReader cdReader,
            string sourcePath,
            string outputFolder,
            string translatedRoot,
            List<string> translatableFiles)
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

                Log.Debug("Extracted file: {Path}", destPath);

                if (FileHelpers.IsTranslatableFile(destPath))
                {
                    translatableFiles.Add(relativePath);
                    Log.Information("Detected translatable file: {Path}", relativePath);

                    string translatedPath = Path.Combine(translatedRoot, relativePath);
                    string? tdir = Path.GetDirectoryName(translatedPath);
                    if (!string.IsNullOrEmpty(tdir))
                        Directory.CreateDirectory(tdir);

                    Translator.TranslateFile(destPath, translatedPath);
                    Log.Debug("Created translated placeholder: {Path}", translatedPath);
                }
            }

            foreach (var dir in cdReader.GetDirectories(sourcePath))
            {
                ExtractDirectory(cdReader, dir, outputFolder, translatedRoot, translatableFiles);
            }
        }
    }
}
