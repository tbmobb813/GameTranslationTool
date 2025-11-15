using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DiscUtils.Iso9660;
using Serilog;

namespace GameTranslationTool.Services
{
    /// <summary>
    /// Service for repacking folders into ISO disc images
    /// </summary>
    public class IsoRepackerService : IIsoRepacker
    {
        private readonly IFileSystemService _fileSystem;
        private readonly ILogger _logger;

        public IsoRepackerService(IFileSystemService fileSystem, ILogger logger)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task RepackAsync(
            string sourceFolder,
            string outputIsoPath,
            string volumeLabel,
            IProgress<IsoProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (!_fileSystem.DirectoryExists(sourceFolder))
            {
                _logger.Error("Source folder does not exist: {Path}", sourceFolder);
                throw new DirectoryNotFoundException($"Source folder does not exist: {sourceFolder}");
            }

            try
            {
                _logger.Information("Starting ISO repacking from {Folder} to {Path}", sourceFolder, outputIsoPath);

                await Task.Run(() =>
                {
                    using FileStream isoStream = File.Create(outputIsoPath);
                    var builder = new CDBuilder
                    {
                        UseJoliet = true,
                        VolumeIdentifier = volumeLabel
                    };

                    var allFiles = _fileSystem.GetFiles(sourceFolder, "*", SearchOption.AllDirectories);
                    int totalFiles = allFiles.Length;
                    int processedFiles = 0;

                    _logger.Information("Found {Count} files to add to ISO", totalFiles);

                    foreach (string filePath in allFiles)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        string relativePath = Path.GetRelativePath(sourceFolder, filePath).Replace('\\', '/');
                        builder.AddFile(relativePath, filePath);

                        processedFiles++;
                        progress?.Report(new IsoProgress
                        {
                            Current = processedFiles,
                            Total = totalFiles,
                            CurrentFile = relativePath
                        });

                        if (processedFiles % 100 == 0)
                        {
                            _logger.Debug("Added {Count}/{Total} files to ISO", processedFiles, totalFiles);
                        }
                    }

                    _logger.Information("Building ISO file...");
                    builder.Build(isoStream);

                    _logger.Information("ISO repacked successfully to: {Path}", outputIsoPath);
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to repack ISO");
                throw;
            }
        }
    }
}
