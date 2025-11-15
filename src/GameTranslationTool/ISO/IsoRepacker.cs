using System;
using Serilog;
using System.IO;
using System.Linq;
using DiscUtils.Iso9660;

namespace GameTranslationTool.ISO
{
    public static class IsoRepacker
    {
        /// <summary>
        /// Progress callback for repacking operations
        /// </summary>
        public delegate void ProgressCallback(int current, int total, string currentFile);

        /// <summary>
        /// Repacks a folder into an ISO file with optional progress reporting
        /// </summary>
        public static void RepackIso(
            string sourceFolder,
            string outputIsoPath,
            string volumeLabel,
            ProgressCallback? progressCallback = null)
        {
            if (!Directory.Exists(sourceFolder))
            {
                Log.Error("Source folder does not exist: {Path}", sourceFolder);
                throw new DirectoryNotFoundException($"Source folder does not exist: {sourceFolder}");
            }

            try
            {
                Log.Information("Starting ISO repacking from {Folder} to {Path}", sourceFolder, outputIsoPath);

                using FileStream isoStream = File.Create(outputIsoPath);
                var builder = new CDBuilder
                {
                    UseJoliet = true,
                    VolumeIdentifier = volumeLabel
                };

                var allFiles = Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories);
                int totalFiles = allFiles.Length;
                int processedFiles = 0;

                Log.Information("Found {Count} files to add to ISO", totalFiles);

                foreach (string filePath in allFiles)
                {
                    string relativePath = Path.GetRelativePath(sourceFolder, filePath).Replace('\\', '/');
                    builder.AddFile(relativePath, filePath);

                    processedFiles++;
                    progressCallback?.Invoke(processedFiles, totalFiles, relativePath);

                    if (processedFiles % 100 == 0)
                    {
                        Log.Debug("Added {Count}/{Total} files to ISO", processedFiles, totalFiles);
                    }
                }

                Log.Information("Building ISO file...");
                builder.Build(isoStream);

                Log.Information("ISO repacked successfully to: {Path}", outputIsoPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to repack ISO");
                throw;
            }
        }
    }
}
