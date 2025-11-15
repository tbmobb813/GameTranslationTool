using System;
using System.IO;
using System.Windows;
using GameTranslationTool.Services;
using GameTranslationTool.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using WpfMessageBox = System.Windows.MessageBox;

namespace GameTranslationTool
{
    public partial class App : System.Windows.Application
    {
        private IServiceProvider? _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            // First-chance handler for UI thread exceptions
            this.DispatcherUnhandledException += (s, ev) =>
            {
                Log.Fatal(ev.Exception, "Unhandled dispatcher exception");
                WpfMessageBox.Show($"Fatal error: {ev.Exception.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ev.Handled = true;
                Shutdown();
            };

            // Catch any domain‐level exceptions on background threads
            AppDomain.CurrentDomain.UnhandledException += (s, ev) =>
            {
                var ex = ev.ExceptionObject as Exception;
                Log.Fatal(ex, "Unhandled domain exception");
                WpfMessageBox.Show($"Fatal error: {ex?.Message ?? "Unknown error"}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            };

            base.OnStartup(e);

            // Ensure Logs folder exists
            var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(
                    path: Path.Combine(logDir, "log-.txt"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
                )
                .CreateLogger();

            Log.Information("Application started.");

            // Configure Dependency Injection
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // Create and show MainWindow via DI
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Register Serilog logger
            services.AddSingleton<ILogger>(Log.Logger);

            // Register core services
            services.AddSingleton<IFileSystemService, FileSystemService>();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<ICacheService, CacheService>();

            // Register ISO services
            services.AddSingleton<IIsoExtractor, IsoExtractorService>();
            services.AddSingleton<IIsoRepacker, IsoRepackerService>();

            // Register utility services
            services.AddSingleton<RateLimiter>(sp => new RateLimiter(maxRequestsPerMinute: 60));

            // Register ViewModels
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<IsoViewModel>();

            // Register MainWindow for DI
            services.AddTransient<MainWindow>();

            Log.Information("Dependency injection configured successfully");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("Application exited.");

            // Dispose service provider
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }

            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}
