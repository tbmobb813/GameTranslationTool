using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Input;
using GameTranslationTool.Constants;
using GameTranslationTool.Models;
using GameTranslationTool.MVVM;
using GameTranslationTool.Services;
using GameTranslationTool.Translation;
using Polly;
using Polly.Retry;
using Serilog;

namespace GameTranslationTool.ViewModels
{
    /// <summary>
    /// Main ViewModel for the application
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        // ─── Services (Injected via DI) ──────────────────────────────
        private readonly ISettingsService _settingsService;
        private readonly ICacheService _cacheService;
        private readonly RateLimiter _rateLimiter;
        private readonly ILogger _logger;

        // ─── Fields ──────────────────────────────────────────────────
        private TranslationSettings _settings;
        private ITranslator _translationEngine;
        private readonly AsyncRetryPolicy<string> _retryPolicy;
        private CancellationTokenSource? _cts;

        // ─── Bound Properties ────────────────────────────────────────
        private string _selectedProvider = TranslationProvider.GoogleTranslate.ToDisplayName();
        public string SelectedProvider
        {
            get => _selectedProvider;
            set
            {
                if (SetProperty(ref _selectedProvider, value))
                {
                    SetupTranslator();
                    OnPropertyChanged(nameof(IsMicrosoftTranslator));
                }
            }
        }

        private string _googleApiKey = string.Empty;
        public string GoogleApiKey
        {
            get => _googleApiKey;
            set => SetProperty(ref _googleApiKey, value);
        }

        private string _microsoftApiKey = string.Empty;
        public string MicrosoftApiKey
        {
            get => _microsoftApiKey;
            set => SetProperty(ref _microsoftApiKey, value);
        }

        private string _microsoftRegion = string.Empty;
        public string MicrosoftRegion
        {
            get => _microsoftRegion;
            set => SetProperty(ref _microsoftRegion, value);
        }

        private string _selectedSourceLanguage = Languages.Supported[0];
        public string SelectedSourceLanguage
        {
            get => _selectedSourceLanguage;
            set => SetProperty(ref _selectedSourceLanguage, value);
        }

        private string _selectedTargetLanguage = Languages.Supported[1];
        public string SelectedTargetLanguage
        {
            get => _selectedTargetLanguage;
            set => SetProperty(ref _selectedTargetLanguage, value);
        }

        public bool IsMicrosoftTranslator =>
            SelectedProvider == TranslationProvider.MicrosoftTranslator.ToDisplayName();

        // ─── Collections ─────────────────────────────────────────────
        public ObservableCollection<string> AvailableProviders { get; }
        public ObservableCollection<string> SupportedLanguages { get; }
        public ObservableCollection<DialogEntry> DialogEntries { get; }
        public ObservableCollection<TranslationEntry> StringEntries { get; }

        // ─── Commands ────────────────────────────────────────────────
        public ICommand SaveSettingsCommand { get; }
        public ICommand ClearCacheCommand { get; }

        // ─── Constructor ─────────────────────────────────────────────
        public MainViewModel(
            ISettingsService settingsService,
            ICacheService cacheService,
            RateLimiter rateLimiter,
            ILogger logger)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize collections
            AvailableProviders = new ObservableCollection<string>
            {
                TranslationProvider.MicrosoftTranslator.ToDisplayName(),
                TranslationProvider.GoogleTranslate.ToDisplayName()
            };
            SupportedLanguages = new ObservableCollection<string>(Languages.Supported);
            DialogEntries = new ObservableCollection<DialogEntry>();
            StringEntries = new ObservableCollection<TranslationEntry>();

            // Initialize retry policy
            _retryPolicy = Policy<string>
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(200 * attempt),
                    onRetry: (outcome, delay, retryCount, context) =>
                    {
                        var msg = outcome.Exception?.Message ?? "(none)";
                        _logger.Warning("Retry #{RetryCount} failed: {Message}", retryCount, msg);
                    }
                );

            // Initialize commands
            SaveSettingsCommand = new RelayCommand(_ => SaveSettings(), _ => true);
            ClearCacheCommand = new RelayCommand(_ => ClearCache(), _ => true);

            // Load settings and cache
            LoadSettings();
            _cacheService.LoadCache();

            // Setup initial translator
            SetupTranslator();

            _logger.Information("MainViewModel initialized");
        }

        // ─── Methods ─────────────────────────────────────────────────

        private void LoadSettings()
        {
            _settings = _settingsService.LoadSettings();

            GoogleApiKey = _settings.GoogleApiKey ?? string.Empty;
            MicrosoftApiKey = _settings.MicrosoftApiKey ?? string.Empty;
            MicrosoftRegion = _settings.MicrosoftRegion ?? string.Empty;

            _logger.Information("Settings loaded into ViewModel");
        }

        private void SaveSettings()
        {
            try
            {
                _settings.GoogleApiKey = GoogleApiKey;
                _settings.MicrosoftApiKey = MicrosoftApiKey;
                _settings.MicrosoftRegion = MicrosoftRegion;

                _settingsService.SaveSettings(_settings);
                _logger.Information("Settings saved successfully");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to save settings");
                ErrorHandler.ShowError("Failed to save settings", ex);
            }
        }

        private void ClearCache()
        {
            try
            {
                _cacheService.ClearCache();
                _logger.Information("Translation cache cleared");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to clear cache");
                ErrorHandler.ShowError("Failed to clear cache", ex);
            }
        }

        private void SetupTranslator()
        {
            if (SelectedProvider == TranslationProvider.GoogleTranslate.ToDisplayName())
            {
                _translationEngine = new GoogleTranslatorService(GoogleApiKey);
                _logger.Information("Switched to Google Translate");
            }
            else if (SelectedProvider == TranslationProvider.MicrosoftTranslator.ToDisplayName())
            {
                _translationEngine = new MicrosoftTranslatorService(MicrosoftApiKey, MicrosoftRegion);
                _logger.Information("Switched to Microsoft Translator");
            }
        }

        public void Cleanup()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cacheService.SaveCache();
            _logger.Information("MainViewModel cleanup complete");
        }
    }
}
