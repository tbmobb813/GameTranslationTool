using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using GameTranslationTool.MVVM;
using GameTranslationTool.Services;
using Serilog;

namespace GameTranslationTool.ViewModels
{
    /// <summary>
    /// ViewModel for ISO extraction and repacking operations
    /// </summary>
    public class IsoViewModel : ViewModelBase
    {
        // ─── Services (Injected via DI) ──────────────────────────────
        private readonly IIsoExtractor _isoExtractor;
        private readonly IIsoRepacker _isoRepacker;
        private readonly IFileSystemService _fileSystem;
        private readonly ILogger _logger;

        // ─── Fields ──────────────────────────────────────────────────
        private CancellationTokenSource? _cts;

        // ─── Bound Properties ────────────────────────────────────────
        private string _isoPath = string.Empty;
        public string IsoPath
        {
            get => _isoPath;
            set => SetProperty(ref _isoPath, value);
        }

        private string _extractPath = string.Empty;
        public string ExtractPath
        {
            get => _extractPath;
            set => SetProperty(ref _extractPath, value);
        }

        private string _translatedFolder = string.Empty;
        public string TranslatedFolder
        {
            get => _translatedFolder;
            set => SetProperty(ref _translatedFolder, value);
        }

        private string _sourceFolder = string.Empty;
        public string SourceFolder
        {
            get => _sourceFolder;
            set => SetProperty(ref _sourceFolder, value);
        }

        private string _outputIsoPath = string.Empty;
        public string OutputIsoPath
        {
            get => _outputIsoPath;
            set => SetProperty(ref _outputIsoPath, value);
        }

        private string _volumeLabel = "GAME_DISC";
        public string VolumeLabel
        {
            get => _volumeLabel;
            set => SetProperty(ref _volumeLabel, value);
        }

        private int _progressValue = 0;
        public int ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        private string _progressText = "0%";
        public string ProgressText
        {
            get => _progressText;
            set => SetProperty(ref _progressText, value);
        }

        private bool _isExtracting = false;
        public bool IsExtracting
        {
            get => _isExtracting;
            set
            {
                if (SetProperty(ref _isExtracting, value))
                {
                    OnPropertyChanged(nameof(CanStartExtraction));
                    OnPropertyChanged(nameof(CanCancelOperation));
                }
            }
        }

        private bool _isRepacking = false;
        public bool IsRepacking
        {
            get => _isRepacking;
            set
            {
                if (SetProperty(ref _isRepacking, value))
                {
                    OnPropertyChanged(nameof(CanStartRepacking));
                    OnPropertyChanged(nameof(CanCancelOperation));
                }
            }
        }

        public bool CanStartExtraction => !IsExtracting && !IsRepacking;
        public bool CanStartRepacking => !IsExtracting && !IsRepacking;
        public bool CanCancelOperation => IsExtracting || IsRepacking;

        // ─── Commands ────────────────────────────────────────────────
        public ICommand ExtractIsoCommand { get; }
        public ICommand RepackIsoCommand { get; }
        public ICommand CancelCommand { get; }

        // ─── Constructor ─────────────────────────────────────────────
        public IsoViewModel(
            IIsoExtractor isoExtractor,
            IIsoRepacker isoRepacker,
            IFileSystemService fileSystem,
            ILogger logger)
        {
            _isoExtractor = isoExtractor ?? throw new ArgumentNullException(nameof(isoExtractor));
            _isoRepacker = isoRepacker ?? throw new ArgumentNullException(nameof(isoRepacker));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize commands
            ExtractIsoCommand = new RelayCommand(async _ => await ExtractIsoAsync(), _ => CanStartExtraction);
            RepackIsoCommand = new RelayCommand(async _ => await RepackIsoAsync(), _ => CanStartRepacking);
            CancelCommand = new RelayCommand(_ => CancelOperation(), _ => CanCancelOperation);

            _logger.Information("IsoViewModel initialized");
        }

        // ─── Methods ─────────────────────────────────────────────────

        public async Task ExtractIsoAsync()
        {
            if (!ValidationService.FileExists(IsoPath))
            {
                ErrorHandler.ShowWarning("Please select a valid ISO file");
                return;
            }

            if (!ValidationService.IsValidDirectoryPath(ExtractPath))
            {
                ErrorHandler.ShowWarning("Please select a valid extraction folder");
                return;
            }

            // Validate disk space
            var isoSize = _fileSystem.GetFileSize(IsoPath);
            if (!ValidationService.HasSufficientDiskSpace(ExtractPath, isoSize))
            {
                var available = ValidationService.FormatBytes(_fileSystem.GetAvailableDiskSpace(ExtractPath));
                var required = ValidationService.FormatBytes(isoSize);
                ErrorHandler.ShowWarning(
                    $"Insufficient disk space!\n\nRequired: ~{required}\nAvailable: {available}");
                return;
            }

            try
            {
                IsExtracting = true;
                _cts = new CancellationTokenSource();

                var progress = new Progress<IsoProgress>(p =>
                {
                    ProgressValue = (int)p.Percentage;
                    ProgressText = $"{p.Percentage:F1}% ({p.Current}/{p.Total})";
                });

                _logger.Information("Starting ISO extraction: {Path}", IsoPath);

                await _isoExtractor.ExtractAsync(
                    IsoPath,
                    ExtractPath,
                    TranslatedFolder,
                    progress,
                    _cts.Token);

                _logger.Information("ISO extraction completed successfully");
                ErrorHandler.ShowSuccess("ISO extracted successfully!");

                // Reset progress
                ProgressValue = 0;
                ProgressText = "0%";
            }
            catch (OperationCanceledException)
            {
                _logger.Warning("ISO extraction cancelled by user");
                ErrorHandler.ShowWarning("Extraction cancelled");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "ISO extraction failed");
                ErrorHandler.ShowError("Failed to extract ISO", ex);
            }
            finally
            {
                IsExtracting = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        public async Task RepackIsoAsync()
        {
            if (!ValidationService.DirectoryExists(SourceFolder))
            {
                ErrorHandler.ShowWarning("Please select a valid source folder");
                return;
            }

            if (!ValidationService.IsValidFilePath(OutputIsoPath))
            {
                ErrorHandler.ShowWarning("Please specify a valid output ISO path");
                return;
            }

            // Validate disk space
            var folderSize = _fileSystem.GetDirectorySize(SourceFolder);
            var outputDir = _fileSystem.GetDirectoryName(OutputIsoPath);
            if (!ValidationService.HasSufficientDiskSpace(outputDir, folderSize))
            {
                var available = ValidationService.FormatBytes(_fileSystem.GetAvailableDiskSpace(outputDir));
                var required = ValidationService.FormatBytes(folderSize);
                ErrorHandler.ShowWarning(
                    $"Insufficient disk space!\n\nRequired: ~{required}\nAvailable: {available}");
                return;
            }

            try
            {
                IsRepacking = true;
                _cts = new CancellationTokenSource();

                var progress = new Progress<IsoProgress>(p =>
                {
                    ProgressValue = (int)p.Percentage;
                    ProgressText = $"{p.Percentage:F1}% ({p.Current}/{p.Total})";
                });

                _logger.Information("Starting ISO repacking: {Folder} -> {Path}", SourceFolder, OutputIsoPath);

                await _isoRepacker.RepackAsync(
                    SourceFolder,
                    OutputIsoPath,
                    VolumeLabel,
                    progress,
                    _cts.Token);

                _logger.Information("ISO repacking completed successfully");
                ErrorHandler.ShowSuccess("ISO repacked successfully!");

                // Reset progress
                ProgressValue = 0;
                ProgressText = "0%";
            }
            catch (OperationCanceledException)
            {
                _logger.Warning("ISO repacking cancelled by user");
                ErrorHandler.ShowWarning("Repacking cancelled");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "ISO repacking failed");
                ErrorHandler.ShowError("Failed to repack ISO", ex);
            }
            finally
            {
                IsRepacking = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        public void CancelOperation()
        {
            _cts?.Cancel();
            _logger.Information("ISO operation cancellation requested");
        }

        public void Cleanup()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _logger.Information("IsoViewModel cleanup complete");
        }
    }
}
