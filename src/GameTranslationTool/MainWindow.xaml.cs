using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using GameTranslationTool.ISO;
using GameTranslationTool.Translation;
using GameTranslationTool.Utils;
using Polly;
using Polly.Retry;
using Serilog;
using WinForms = System.Windows.Forms;
using WpfMessageBox = System.Windows.MessageBox;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;
using WpfSaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace GameTranslationTool
{
    public partial class MainWindow : Window
    {
        // ─── Fields ──────────────────────────────────────────────────

        // For the Dialogs tab
        private readonly ObservableCollection<DialogEntry> _dialogEntries = [];
        private string _lastLoadedFile = string.Empty;
        private CancellationTokenSource _cts;
        private readonly AsyncRetryPolicy<string> _retryPolicy;
        private ITranslator _translationEngine;
        private readonly string _settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "translator_settings.json");
        private TranslationSettings _settings;
        private readonly Dictionary<string, string> _translationCache = [];
        private readonly ObservableCollection<TranslationEntry> _stringEntries = [];
        private readonly string _cacheFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "translation_cache.json");
        private static readonly string[] SupportedLanguages = ["en", "es", "fr", "de", "ja"];

        // ─── Constructor ────────────────────────────────────────────

        public MainWindow()
        {
            InitializeComponent();

            // 1️⃣ Load settings & cache
            LoadSettings();
            LoadTranslationCache();

            // 2️⃣ Configure retry policy
            _retryPolicy = Policy<string>
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(200 * attempt),
                    onRetry: (outcome, delay, retryCount, context) =>
                    {
                        var msg = outcome.Exception?.Message ?? "(none)";
                        Log.Warning("Retry #{RetryCount} failed: {Message}", retryCount, msg);
                    }
                );

            // 3️⃣ Populate & wire provider ComboBox
            ComboApiProvider.Items.Clear();
            ComboApiProvider.ItemsSource = new[] { "Microsoft Translator", "Google Translate" };
            ComboApiProvider.SelectedIndex = 1;
            ComboApiProvider.SelectionChanged += ComboApiProvider_SelectionChanged;

            // 4️⃣ Populate language ComboBoxes
            ComboSourceLang.ItemsSource = SupportedLanguages;
            ListTargetLangs.ItemsSource = SupportedLanguages;
            ComboSourceLang.SelectedIndex = 0;
            ListTargetLangs.SelectedIndex = 1;

            // 5️⃣ Bind DataGrid
            DataGridStrings.ItemsSource = _stringEntries;

            // 6️⃣ Initial translator & UI setup
            SetupTranslator();
            UpdateRegionPanelVisibility();

            // 7️⃣ Set up DataGrid for Dialogs
            DataGridDialogs.ItemsSource = _dialogEntries;
            DataGridDialogs.SelectionChanged += (s, e) =>
            {
                BtnRemovePhrase.IsEnabled = DataGridDialogs.SelectedItem != null;
            };

        }

        //--- API Settings Tab –-------------------------------------------

        private void BtnResetMicrosoftKey_Click(object sender, RoutedEventArgs e)
        {
            SecureVault.DeleteMicrosoftKey();
            TextApiKey.Text = string.Empty;
            SetupTranslator();
        }

        private void BtnResetGoogleKey_Click(object sender, RoutedEventArgs e)
        {
            SecureVault.DeleteGoogleKey();
            TextGoogleApiKey.Text = string.Empty;
            SetupTranslator();
        }

        private void BtnSaveApiSettings_Click(object sender, RoutedEventArgs e)
        {
            _settings.Region = TextRegion.Text.Trim();
            _settings.GoogleApiKey = TextGoogleApiKey.Text.Trim();
            File.WriteAllText(_settingsPath,
                JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true }));
            SecureVault.SaveMicrosoftKey(TextApiKey.Text.Trim());
            SecureVault.SaveGoogleKey(TextGoogleApiKey.Text.Trim());
            SetupTranslator();
            TextLog.AppendText("API settings saved.\n");
        }

        private async void BtnTestApiKey_Click(object sender, RoutedEventArgs e)
        {
            var provider = ComboApiProvider.SelectedItem as string;
            if (provider == "Google Translate")
            {
                if (string.IsNullOrWhiteSpace(TextGoogleApiKey.Text))
                {
                    WpfMessageBox.Show("Google API key is empty.", "Warning",
                                       MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                try
                {
                    var res = await _translationEngine.TranslateAsync("Hello", "en", "es");
                    WpfMessageBox.Show($"Google Translate OK: {res}", "API Test",
                                       MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    WpfMessageBox.Show(ex.Message, "Error",
                                       MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(TextApiKey.Text) ||
                    string.IsNullOrWhiteSpace(TextRegion.Text))
                {
                    WpfMessageBox.Show("Microsoft key or region is empty.", "Warning",
                                       MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                try
                {
                    var res = await _translationEngine.TranslateAsync("Hello", "en", "fr");
                    WpfMessageBox.Show($"Microsoft Translate OK: {res}", "API Test",
                                       MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    WpfMessageBox.Show(ex.Message, "Error",
                                       MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnClearCache_Click(object sender, RoutedEventArgs e)
        {
            _translationCache.Clear();
            if (File.Exists(_cacheFilePath)) File.Delete(_cacheFilePath);
            Log.Information("Translation cache cleared.");

            StatusBarText.Text = "Cache cleared";

        }

        private async void BtnTestTranslate_Click(object sender, RoutedEventArgs e)
        {
            // Simply call your existing TranslateWithRetryAsync (or TranslateAsync) to test:
            try
            {
                var result = await _translationEngine.TranslateAsync("Hello, world!", "en", "es");
                WpfMessageBox.Show($"Test translate result: {result}",
                                   "Test Translate",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show($"Test translate failed: {ex.Message}",
                                   "Error",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Error);
            }
        }

        // ─── Theme Toggle ────────────────────────────────────────────
        private void ThemeToggle_Checked(object sender, RoutedEventArgs e)
        {
            // load dark, remove light
            var dicts = System.Windows.Application.Current.Resources.MergedDictionaries;
            dicts.Clear();
            dicts.Add(new ResourceDictionary { Source = new Uri("Themes/DarkTheme.xaml", UriKind.Relative) });
            ThemeToggle.Content = "☀️";
        }

        private void ThemeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            // load light
            var dicts = System.Windows.Application.Current.Resources.MergedDictionaries;
            dicts.Clear();
            dicts.Add(new ResourceDictionary { Source = new Uri("Themes/LightTheme.xaml", UriKind.Relative) });
            ThemeToggle.Content = "🌙";
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // on tab switch, restart fade-in
            var sb = (Storyboard)TabContent.Resources["FadeIn"];
            TabContent.Opacity = 0;
            sb.Begin();
        }

        // ─── Settings & Cache ────────────────────────────────────────

        private void LoadSettings()
        {
            if (File.Exists(_settingsPath))
            {
                try
                {
                    var json = File.ReadAllText(_settingsPath);
                    _settings = JsonSerializer.Deserialize<TranslationSettings>(json)
                                ?? new TranslationSettings();
                }
                catch
                {
                    _settings = new TranslationSettings();
                }
            }
            else
            {
                _settings = new TranslationSettings();
            }

            TextApiKey.Text = SecureVault.LoadMicrosoftKey() ?? string.Empty;
            TextRegion.Text = _settings.Region;
            TextGoogleApiKey.Text = SecureVault.LoadGoogleKey() ?? _settings.GoogleApiKey;
        }

        private void LoadTranslationCache()
        {
            if (!File.Exists(_cacheFilePath)) return;
            try
            {
                var json = File.ReadAllText(_cacheFilePath);
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (dict != null)
                    foreach (var kv in dict)
                        _translationCache[kv.Key] = kv.Value;
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to load translation cache: {Message}", ex.Message);
            }
        }

        private void SaveTranslationCache()
        {
            try
            {
                var json = JsonSerializer.Serialize(_translationCache,
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_cacheFilePath, json);
            }
            catch (Exception ex)
            {
                Log.Error("Failed to save translation cache: {Message}", ex.Message);
            }
        }

        // ─── Translator Setup ────────────────────────────────────────

        /// <summary>
        /// Wraps your _translationEngine.TranslateAsync call in the retry policy.
        /// </summary>
        private Task<string> TranslateWithRetryAsync(string text, string fromLang, string toLang)
        {
            // capture the text in the Polly Context (for logging)
            var ctx = new Context
            {
                ["text"] = text
            };

            // fire off your retry‐wrapped translate call
            return _retryPolicy.ExecuteAsync(
                // note: this delegate gets (Context, CancellationToken)
                (pollyCtx, ct) => _translationEngine.TranslateAsync(text, fromLang, toLang),
                ctx,
                CancellationToken.None
            );
        }


        private void SetupTranslator()
        {
            var provider = ComboApiProvider.SelectedItem as string;
            if (provider == "Google Translate" &&
                !string.IsNullOrWhiteSpace(TextGoogleApiKey.Text))
            {
                _translationEngine = new GoogleTranslatorService(TextGoogleApiKey.Text.Trim());
            }
            else
            {
                _translationEngine = new MicrosoftTranslatorService(
                    TextApiKey.Text.Trim(),
                    TextRegion.Text.Trim());
            }
        }

        private void UpdateRegionPanelVisibility()
        {
            var provider = ComboApiProvider.SelectedItem as string;
            MsRegionPanel.Visibility =
                provider == "Microsoft Translator"
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        // ─── String File Handlers ────────────────────────────────────

        private void ComboApiProvider_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateRegionPanelVisibility();
            SetupTranslator();
        }

        private void BtnLoadStrings_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new WpfOpenFileDialog
            {
                Filter = "Text files|*.txt;*.json;*.xml|All files|*.*"
            };
            if (dlg.ShowDialog() != true) return;
            _lastLoadedFile = dlg.FileName;

            _stringEntries.Clear();
            foreach (var line in File.ReadAllLines(_lastLoadedFile))
            {
                var entry = new TranslationEntry { Original = line };
                if (_translationCache.TryGetValue(line, out var cached))
                    entry.Translated = cached;
                _stringEntries.Add(entry);
            }

            BtnAutoTranslate.IsEnabled = BtnSaveStrings.IsEnabled =
                _stringEntries.Any();
            BtnExportCsv.IsEnabled = _stringEntries.Any();

            StatusBarText.Text = $"Loaded {_stringEntries.Count} strings";

        }

        private async void BtnAutoTranslate_Click(object sender, RoutedEventArgs e)
        {
            // before kicking off the translation, check if we have a valid API key
            CompletionGauge.Value = 0;
            CompletionLabel.Text = "0%";

            // --- disable controls while translating ---
            ComboSourceLang.IsEnabled = false;
            ListTargetLangs.IsEnabled = false;
            ComboApiProvider.IsEnabled = false;
            BtnLoadStrings.IsEnabled = false;
            BtnSaveStrings.IsEnabled = false;
            BtnExportCsv.IsEnabled = false;
            BtnAutoTranslate.IsEnabled = false;
            BtnCancelTranslate.IsEnabled = true;
            TranslationProgressBar.Visibility = Visibility.Visible;

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            var total = _stringEntries.Count;
            for (int i = 0; i < total; i++)

                try
                {
                    var entry = _stringEntries[i];
                    {
                        token.ThrowIfCancellationRequested();

                        var ctx = new Context
                        {
                            ["text"] = entry.Original
                        };

                        var result = await _retryPolicy
                            .ExecuteAndCaptureAsync(
                              (c, ct) => _translationEngine.TranslateAsync(
                                   entry.Original,
                                   ComboSourceLang.SelectedItem as string ?? "en",
                                   ListTargetLangs.SelectedItem as string ?? "es"),
                              ctx, token);

                        if (result.Outcome == OutcomeType.Successful)
                        {
                            // if the API responded but gave you back the same text,
                            // mark it as an error
                            if (result.Result == entry.Original)
                            {
                                entry.Translated = string.Empty;
                                entry.Error = "No translation found.";
                            }
                            else
                            {
                                entry.Translated = result.Result;
                                entry.Error = string.Empty;

                                // --- New: smart line-break insertion ---
                                if (int.TryParse(TextMaxLineLength.Text, out var maxLen) && maxLen > 0)
                                    entry.Translated = SmartLineBreaker.InsertLineBreaks(entry.Translated, maxLen);

                                _translationCache[entry.Original] = entry.Translated;
                            }
                        }
                        else
                        {
                            // a real exception occurred
                            entry.Translated = string.Empty;
                            entry.Error = result.FinalException?.Message ?? "Error";
                        }

                        TranslationProgressBar.Value = i + 1;
                        int percent = (int)((i + 1) * 100.0 / _stringEntries.Count);
                        CompletionGauge.Value = percent;
                        CompletionLabel.Text = $"{percent}%";

                        TranslationStatusLabel.Text =
                            $"Translated {i + 1}/{total}: \"{entry.Original}\" -> \"{entry.Translated}\"";
                        Log.Information("Translated {Original}", entry.Original);

                        StatusBarText.Text = $"Translated {_stringEntries.Count} strings at {DateTime.Now:T}";

                    }

                    TranslationStatusLabel.Text = "✅ All strings processed.";
                    SaveTranslationCache();
                }
                catch (OperationCanceledException)
                {
                    StatusBarText.Text = "Translation cancelled by user";

                    Log.Information("Translation cancelled by user.");
                    TranslationStatusLabel.Text = "⚠️ Translation cancelled.";
                }
                catch (Exception ex)
                {
                    WpfMessageBox.Show($"Unexpected error: {ex.Message}",
                                       "Translation Error",
                                       MessageBoxButton.OK,
                                       MessageBoxImage.Error);
                    Log.Error(ex, "Error during translation");
                    TranslationStatusLabel.Text = "❌ Error during translation.";
                }
                finally
                {
                    // --- re-enable controls now that we’re done ---
                    ComboSourceLang.IsEnabled = true;
                    ListTargetLangs.IsEnabled = true;
                    ComboApiProvider.IsEnabled = true;
                    BtnLoadStrings.IsEnabled = _stringEntries.Any();
                    BtnSaveStrings.IsEnabled = _stringEntries.Any();
                    BtnExportCsv.IsEnabled = _stringEntries.Any();
                    BtnAutoTranslate.IsEnabled = true;
                    BtnCancelTranslate.IsEnabled = false;

                    CompletionGauge.Value = 100;
                    CompletionLabel.Text = "100%";

                }
        }

        private async void BtnBatchTranslate_Click(object sender, RoutedEventArgs e)
        {
            var fromLang = ComboSourceLang.SelectedItem as string ?? "en";
            var targets = ListTargetLangs.SelectedItems.Cast<string>().ToList();
            if (!targets.Any())
            {
                WpfMessageBox.Show("Pick at least one target language.",
                                   "Batch Translate",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Warning);
                return;
            }

            // pick a folder for the .txt files
            using var fd = new WinForms.FolderBrowserDialog { Description = "Select output folder" };
            if (fd.ShowDialog() != WinForms.DialogResult.OK) return;
            var outDir = fd.SelectedPath;

            // init batch UI
            int totalLangs = targets.Count;
            BatchProgressBar.Maximum = totalLangs;
            BatchProgressBar.Value = 0;
            BatchProgressBar.Visibility = Visibility.Visible;
            BatchStatusLabel.Text = "";
            BatchStatusLabel.Visibility = Visibility.Visible;
            StatusBarText.Text = "Batch translating…";
            BtnBatchTranslate.IsEnabled = false;
            BtnLoadStrings.IsEnabled = false;
            BtnAutoTranslate.IsEnabled = false;
            BtnSaveStrings.IsEnabled = false;
            BtnExportCsv.IsEnabled = false;

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            try
            {
                // 1) per-language .txt files
                foreach (var toLang in targets)
                {
                    BatchProgressBar.Value++;
                    BatchStatusLabel.Text =
                        $"Translating {toLang} ({BatchProgressBar.Value}/{totalLangs})";

                    var fileOut = Path.Combine(outDir, $"Translated_{toLang}_{timestamp}.txt");
                    var errors = new List<string>();
                    var lines = new List<string>();

                    foreach (var entry in _stringEntries)
                    {
                        var translation = await TranslateWithRetryAsync(entry.Original, fromLang, toLang);

                        if (translation == entry.Original)
                            errors.Add(entry.Original);

                        if (int.TryParse(TextMaxLineLength.Text, out var maxLen) && maxLen > 0)
                            translation = SmartLineBreaker.InsertLineBreaks(translation, maxLen);

                        lines.Add(translation);
                    }

                    File.WriteAllLines(fileOut, lines);

                    if (errors.Any())
                    {
                        var errFile = Path.Combine(outDir, $"Errors_{toLang}_{timestamp}.txt");
                        File.WriteAllLines(errFile, errors);
                    }
                }

                // 2) ask where to save the ZIP
                var sfdZip = new WpfSaveFileDialog
                {
                    Filter = "ZIP Archive|*.zip",
                    FileName = $"Translations_{timestamp}.zip"
                };
                if (sfdZip.ShowDialog() != true)
                {
                    StatusBarText.Text = "Batch cancelled (no ZIP path).";
                    return;
                }

                // 3) create it
                ZipFile.CreateFromDirectory(outDir, sfdZip.FileName);

                WpfMessageBox.Show($"Batch complete!\nArchive saved to:\n{sfdZip.FileName}",
                                   "Done",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Information);
                StatusBarText.Text = "Batch finished.";
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show($"Batch translate failed: {ex.Message}",
                                   "Error",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Error);
                StatusBarText.Text = "Batch failed.";
            }
            finally
            {
                // tear-down UI
                BatchProgressBar.Visibility = Visibility.Collapsed;
                BatchStatusLabel.Visibility = Visibility.Collapsed;
                BtnBatchTranslate.IsEnabled = true;
                BtnLoadStrings.IsEnabled = _stringEntries.Any();
                BtnAutoTranslate.IsEnabled = _stringEntries.Any();
                BtnSaveStrings.IsEnabled = _stringEntries.Any();
                BtnExportCsv.IsEnabled = _stringEntries.Any();
            }
        }  

        private void BtnCancelTranslate_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            BtnCancelTranslate.IsEnabled = false;
        }

        private void BtnSaveStrings_Click(object sender, RoutedEventArgs e)
        {
            if (!_stringEntries.Any()) return;
            var dlg = new WpfSaveFileDialog
            {
                Filter = "Text files|*.txt|All files|*.*",
                FileName = "TranslatedStrings.txt"
            };
            if (dlg.ShowDialog() != true) return;

            File.WriteAllLines(dlg.FileName, _stringEntries.Select(x => x.Translated));
            WpfMessageBox.Show("Saved translations.", "Done",
                               MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnExportCsv_Click(object sender, RoutedEventArgs e)
        {
            if (!_stringEntries.Any()) return;
            var dlg = new WpfSaveFileDialog
            {
                Filter = "CSV files|*.csv|All files|*.*",
                FileName = "Translations.csv"
            };
            if (dlg.ShowDialog() != true) return;

            var sb = new StringBuilder();
            sb.AppendLine("Original,Translated,Error");
            foreach (var entry in _stringEntries)
            {
                var o = entry.Original.Replace("\"", "\"\"");
                var t = entry.Translated.Replace("\"", "\"\"");
                var r = entry.Error.Replace("\"", "\"\"");
                sb.AppendLine($"\"{o}\",\"{t}\",\"{r}\"");
            }

            File.WriteAllText(dlg.FileName, sb.ToString());
            WpfMessageBox.Show("Exported to CSV.", "Done",
                               MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ─── Project Tab Handlers ──────────────────────────────────

        private void BtnBrowseIso_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new WpfOpenFileDialog { Filter = "ISO files|*.iso|All files|*.*" };
            if (dlg.ShowDialog() == true)
                TextIsoPath.Text = dlg.FileName;
        }

        private void BtnBrowseExtract_Click(object sender, RoutedEventArgs e)
        {
            using var fd = new WinForms.FolderBrowserDialog();
            if (fd.ShowDialog() == WinForms.DialogResult.OK)
                TextExtractPath.Text = fd.SelectedPath;
        }

        private void BtnExtractIso_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IsoExtractor.ExtractIso(TextIsoPath.Text, TextExtractPath.Text, string.Empty);
                TextProjectLog.AppendText("Extraction complete.\n");
            }
            catch (Exception ex)
            {
                TextProjectLog.AppendText($"Error: {ex.Message}\n");
            }
        }

        private void BtnRepackIso_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var repacked = Path.Combine(TextExtractPath.Text, "Repacked.iso");
                IsoRepacker.RepackIso(TextExtractPath.Text, repacked, "MYGAME");
                TextProjectLog.AppendText($"Repacked: {repacked}\n");
            }
            catch (Exception ex)
            {
                TextProjectLog.AppendText($"Error: {ex.Message}\n");
            }
        }

        // ─── Translation Tab Handlers ─────────────────────────────

        private void BtnBrowseExtracted_Click(object sender, RoutedEventArgs e)
        {
            using var dlg = new WinForms.FolderBrowserDialog();
            if (dlg.ShowDialog() == WinForms.DialogResult.OK)
                TextExtractedPath.Text = dlg.SelectedPath;
        }

        private void BtnBrowseTranslated_Click(object sender, RoutedEventArgs e)
        {
            using var dlg = new WinForms.FolderBrowserDialog();
            if (dlg.ShowDialog() == WinForms.DialogResult.OK)
                TextTranslatedPath.Text = dlg.SelectedPath;
        }

        private void BtnTranslate_Click(object sender, RoutedEventArgs e)
        {
            var root = TextExtractedPath.Text;
            var dest = TextTranslatedPath.Text;
            if (!Directory.Exists(root) || string.IsNullOrWhiteSpace(dest))
            {
                WpfMessageBox.Show("Please select valid folders.",
                                   "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                IsoExtractor.ExtractIso(root, dest, string.Empty);
                TextLog.AppendText("Translation complete.\n");
            }
            catch (Exception ex)
            {
                TextLog.AppendText($"Error: {ex.Message}\n");
            }
        }

        //-- Dialogs Tab Handlers ----------------------------------------
        private void BtnLoadDialogs_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new WpfOpenFileDialog { Filter = "Text files|*.txt|All files|*.*" };
            if (dlg.ShowDialog() != true) return;

            _dialogEntries.Clear();
            foreach (var line in File.ReadAllLines(dlg.FileName))
            {
                var parts = line.Split(['|'], 2);
                var id = parts.Length > 0 ? parts[0] : "";
                var text = parts.Length > 1 ? parts[1] : "";
                _dialogEntries.Add(new DialogEntry { Id = id, Text = text });
            }
            BtnSaveDialogs.IsEnabled = _dialogEntries.Count > 0;
        }

        private void BtnAddPhrase_Click(object sender, RoutedEventArgs e)
        {
            // Add a blank entry for inline editing
            var entry = new DialogEntry { Id = "", Text = "" };
            _dialogEntries.Add(entry);
            DataGridDialogs.SelectedItem = entry;
            BtnSaveDialogs.IsEnabled = true;
            BtnRemovePhrase.IsEnabled = true;
        }

        private void BtnRemovePhrase_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridDialogs.SelectedItem is DialogEntry entry)
            {
                _dialogEntries.Remove(entry);
                BtnSaveDialogs.IsEnabled = _dialogEntries.Count > 0;
            }
        }

        private void BtnSaveDialogs_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new WpfSaveFileDialog { Filter = "Text files|*.txt|All files|*.*", FileName = "Dialogs.txt" };
            if (dlg.ShowDialog() != true) return;

            var lines = _dialogEntries
                .Select(en => $"{en.Id}|{en.Text.Replace("|", @"\|")}");

            File.WriteAllLines(dlg.FileName, lines);
            WpfMessageBox.Show("Dialogs saved.", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        // 1) When the user checks “Enable Debug Mode”
        private void ChkEnableDebug_Checked(object sender, RoutedEventArgs e)
        {
            // Enable the level picker and teleport button
            ComboLevels.IsEnabled = true;
            BtnTeleport.IsEnabled = true;

            // (Optional) populate the levels if you haven't already
            if (ComboLevels.Items.Count == 0)
            {
                // example static list—swap for your real level list
                ComboLevels.ItemsSource = new[] { "Level1", "Level2", "Level3" };
                ComboLevels.SelectedIndex = 0;
            }

            StatusBarText.Text = "Debug mode: ON";
        }

        // 2) And when they uncheck it
        private void ChkEnableDebug_Unchecked(object sender, RoutedEventArgs e)
        {
            ComboLevels.IsEnabled = false;
            BtnTeleport.IsEnabled = false;
            StatusBarText.Text = "Debug mode: OFF";
        }

        // 3) Teleport button handler
        private void BtnTeleport_Click(object sender, RoutedEventArgs e)
        {
            var selected = ComboLevels.SelectedItem as string;
            if (string.IsNullOrEmpty(selected))
            {
                WpfMessageBox.Show("Please pick a level first.", "Teleport", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // TODO: call into your game debug API here.
            // For now, we just show a confirmation:
            WpfMessageBox.Show($"Teleporting to {selected}", "Teleport", MessageBoxButton.OK, MessageBoxImage.Information);
            StatusBarText.Text = $"Last teleport: {selected}";
        }

    }
}


public class TranslationEntry : INotifyPropertyChanged
{
    private string _original = string.Empty;
    private string _translated = string.Empty;
    private string _error = string.Empty;

    public string Original
    {
        get => _original;
        set { _original = value; OnPropertyChanged(); }
    }

    public string Translated
    {
        get => _translated;
        set { _translated = value; OnPropertyChanged(); }
    }

    public string Error
    {
        get => _error;
        set { _error = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string propName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
}