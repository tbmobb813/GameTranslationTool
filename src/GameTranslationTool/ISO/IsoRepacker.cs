using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using System.IO;
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
        /// Repacks a folder into an ISO file asynchronously with optional progress reporting and cancellation support
        /// </summary>
        public static async Task RepackIsoAsync(
            string sourceFolder,
            string outputIsoPath,
            string volumeLabel,
            ProgressCallback? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(sourceFolder))
            {
                Log.Error("Source folder does not exist: {Path}", sourceFolder);
                throw new DirectoryNotFoundException($"Source folder does not exist: {sourceFolder}");
            }

            try
            {
                Log.Information("Starting ISO repacking from {Folder} to {Path}", sourceFolder, outputIsoPath);

                await Task.Run(() =>
                {
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
                        cancellationToken.ThrowIfCancellationRequested();

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
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to repack ISO");
                throw;
            }
        }

        /// <summary>
        /// Repacks a folder into an ISO file with optional progress reporting (synchronous version for backward compatibility)
        /// </summary>
        public static void RepackIso(
            string sourceFolder,
            string outputIsoPath,
            string volumeLabel,
            ProgressCallback? progressCallback = null)
        {
            RepackIsoAsync(sourceFolder, outputIsoPath, volumeLabel, progressCallback, CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }
    }
}
