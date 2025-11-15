using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DiscUtils.Iso9660;
using GameTranslationTool.Utils;
using Serilog;

namespace GameTranslationTool.Services
{
    /// <summary>
    /// Service for extracting ISO disc images with path traversal protection
    /// </summary>
    public class IsoExtractorService : IIsoExtractor
    {
        private readonly IFileSystemService _fileSystem;
        private readonly ILogger _logger;

        public IsoExtractorService(IFileSystemService fileSystem, ILogger logger)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ExtractAsync(
            string isoPath,
            string outputFolder,
            string translatedFolder,
            IProgress<IsoProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (!_fileSystem.FileExists(isoPath))
            {
                _logger.Error("ISO file not found: {Path}", isoPath);
                throw new FileNotFoundException($"ISO file not found: {isoPath}", isoPath);
            }

            if (!_fileSystem.DirectoryExists(outputFolder))
            {
                _fileSystem.CreateDirectory(outputFolder);
            }

            _logger.Information("Starting ISO extraction from {Path}", isoPath);

            await Task.Run(async () =>
            {
                using var isoStream = File.OpenRead(isoPath);
                var cdReader = new CDReader(isoStream, true);

                // First pass: count total files
                int totalFiles = CountFiles(cdReader, @"\");
                int processedFiles = 0;

                var translatableFiles = new List<string>();
                await ExtractDirectoryAsync(
                    cdReader,
                    @"\",
                    outputFolder,
                    translatedFolder,
                    translatableFiles,
                    ref processedFiles,
                    totalFiles,
                    progress,
                    cancellationToken);

                _logger.Information("Finished extracting {Count} files to {Folder}", processedFiles, outputFolder);

                string logFilePath = _fileSystem.SafeCombinePath(outputFolder, "TranslatableFiles.txt");
                await _fileSystem.WriteAllLinesAsync(logFilePath, translatableFiles, cancellationToken);
                _logger.Information("Wrote translatable file list to: {Path}", logFilePath);

                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "notepad.exe",
                        Arguments = logFilePath,
                        UseShellExecute = true
                    });
                    _logger.Information("Opened TranslatableFiles.txt in Notepad.");
                }
                catch (Exception ex)
                {
                    _logger.Warning("Could not open log file in Notepad: {Message}", ex.Message);
                }
            }, cancellationToken);
        }

        private int CountFiles(CDReader cdReader, string sourcePath)
        {
            int count = 0;
            count += cdReader.GetFiles(sourcePath).Length;

            foreach (var dir in cdReader.GetDirectories(sourcePath))
            {
                count += CountFiles(cdReader, dir);
            }

            return count;
        }

        private async Task ExtractDirectoryAsync(
            CDReader cdReader,
            string sourcePath,
            string outputFolder,
            string translatedRoot,
            List<string> translatableFiles,
            ref int processedFiles,
            int totalFiles,
            IProgress<IsoProgress>? progress,
            CancellationToken cancellationToken)
        {
            foreach (var file in cdReader.GetFiles(sourcePath))
            {
                cancellationToken.ThrowIfCancellationRequested();

                string relativePath = file.TrimStart('\\');

                // Use SafeCombinePath to prevent path traversal attacks
                string destPath;
                try
                {
                    destPath = _fileSystem.SafeCombinePath(outputFolder, relativePath);
                }
                catch (System.Security.SecurityException ex)
                {
                    _logger.Warning(ex, "Skipping file due to path traversal attempt: {File}", file);
                    continue;
                }

                string? dir = _fileSystem.GetDirectoryName(destPath);
                if (!string.IsNullOrEmpty(dir) && !_fileSystem.DirectoryExists(dir))
                {
                    _fileSystem.CreateDirectory(dir);
                }

                using var src = cdReader.OpenFile(file, FileMode.Open);
                using var dst = File.Create(destPath);
                await src.CopyToAsync(dst, cancellationToken);

                processedFiles++;
                progress?.Report(new IsoProgress
                {
                    Current = processedFiles,
                    Total = totalFiles,
                    CurrentFile = relativePath
                });

                _logger.Debug("Extracted file: {Path}", destPath);

                if (FileHelpers.IsTranslatableFile(destPath))
                {
                    translatableFiles.Add(relativePath);
                    _logger.Information("Detected translatable file: {Path}", relativePath);
                }
            }

            foreach (var dir in cdReader.GetDirectories(sourcePath))
            {
                await ExtractDirectoryAsync(
                    cdReader,
                    dir,
                    outputFolder,
                    translatedRoot,
                    translatableFiles,
                    ref processedFiles,
                    totalFiles,
                    progress,
                    cancellationToken);
            }
        }
    }
}
