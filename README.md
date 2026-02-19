# Game Translation Tool

A Windows desktop application for translating video game content. Extracts text from ISO disc images, translates using multiple translation services, and repacks the translated content.

## Features

### 🎮 ISO Management
- **Extract ISO Files** - Extract disc images with progress tracking
- **Repack ISO Files** - Rebuild translated ISOs for distribution
- **Cancellable Operations** - Cancel long-running operations at any time
- **Progress Indicators** - Visual progress bars with percentage and file counts
- **Disk Space Validation** - Checks available space before operations

### 🌐 Translation Support
- **Multiple Providers** - Google Translate and Microsoft Translator support
- **Batch Translation** - Translate to multiple languages simultaneously
- **Translation Caching** - Avoids redundant API calls
- **Rate Limiting** - Prevents API quota exhaustion
- **Smart Line Breaking** - Automatic line breaks for dialog text

### 🎨 User Interface
- **Modern WPF Interface** - Clean, intuitive design
- **Dark/Light Themes** - Toggle between themes
- **Real-time Progress** - Live updates during operations
- **Comprehensive Logging** - Detailed operation logs
- **Dialog Management** - Manage game dialog/string databases

## Technology Stack

- **.NET 8.0** (Windows-specific)
- **WPF** - Windows Presentation Foundation for UI
- **C# 10+** - Modern language features
- **DiscUtils.Iso9660** - ISO file manipulation
- **Serilog** - Structured logging
- **Polly** - Resilience and retry policies
- **xUnit** - Unit testing framework

## Getting Started

### Prerequisites

- Windows 10/11
- .NET 8.0 SDK or Runtime
- Visual Studio 2022 or later (for development)

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/tbmobb813/GameTranslationTool.git
   cd GameTranslationTool
   ```

2. Build the solution:
   ```bash
   dotnet build
   ```

3. Run the application:
   ```bash
   dotnet run --project src/GameTranslationTool/GameTranslationTool.csproj
   ```

### Configuration

1. **Create `appsettings.json`** (first time only):
   ```bash
   cp src/GameTranslationTool/appsettings.example.json src/GameTranslationTool/appsettings.json
   ```

2. **Add your API keys**:
   Edit `appsettings.json` and add your translation API keys:
   ```json
   {
       "GoogleTranslate": {
           "ApiKey": "YOUR_GOOGLE_API_KEY_HERE"
       }
   }
   ```

3. **Microsoft Translator Setup**:
   - Open the application
   - Go to **API Settings** tab
   - Enter your Microsoft API key and region (e.g., "eastus")
   - Click **Save Settings**

## Usage

### Extracting an ISO

1. Go to the **Project** tab
2. Click **Browse** next to "ISO File" and select your game ISO
3. Click **Browse** next to "Extract To Folder" and choose a destination
4. Click **Extract ISO**
5. Watch the progress bar - you can cancel at any time
6. A TranslatableFiles.txt will open showing detected translatable files

### Translating Strings

1. Go to the **Strings** tab
2. Select source and target languages
3. Choose your translation provider (Google or Microsoft)
4. Click **Load Strings** and select a text file
5. Click **Auto Translate** to translate all strings
6. Review and edit translations in the grid
7. Click **Save Strings** to save translated file

### Batch Translation

1. Load your strings file
2. Select multiple target languages (Ctrl+Click)
3. Click **Batch Translate**
4. Choose output folder
5. All translations will be saved as separate files and zipped

### Repacking an ISO

1. After translating, go back to **Project** tab
2. Your extract folder should be selected
3. Click **Repack ISO**
4. Find the repacked ISO in the extract folder as "Repacked.iso"

## Architecture

### Project Structure

```
GameTranslationTool/
├── src/
│   ├── GameTranslationTool/          # Main WPF application
│   │   ├── Constants/                # Enums and constants
│   │   ├── ISO/                      # ISO extraction/repacking
│   │   ├── Models/                   # Data models
│   │   ├── MVVM/                     # MVVM infrastructure
│   │   ├── Services/                 # Business logic services
│   │   ├── Translation/              # Translation providers
│   │   ├── Utils/                    # Utility classes
│   │   └── Themes/                   # UI themes
│   ├── GameTranslationTool.Tests/    # Unit tests
│   └── GameTranslationTool.ConsoleTest/ # Console test harness
└── README.md
```

### Key Services

- **SettingsService** - Manages application settings persistence
- **CacheService** - Translation caching to reduce API calls
- **ValidationService** - Input validation and disk space checks
- **RateLimiter** - API rate limiting to prevent quota exhaustion
- **ErrorHandler** - Centralized error handling with user-friendly messages

### Design Patterns

- **MVVM** - Model-View-ViewModel architecture
- **Service Layer** - Separation of business logic from UI
- **Repository Pattern** - Data access abstraction
- **Retry Pattern** - Polly for resilient API calls
- **Observer Pattern** - INotifyPropertyChanged for data binding

## Testing

Run all unit tests:

```bash
dotnet test
```

Run with code coverage:

```bash
dotnet test /p:CollectCoverage=true
```

### Test Coverage

- **ValidationService** - 30+ test cases
- **CacheService** - 15+ test cases
- **SettingsService** - 10+ test cases
- **RateLimiter** - 12+ test cases

## Performance

### Optimizations

- **Async Operations** - All long-running operations are async
- **Progress Callbacks** - Efficient progress reporting
- **Rate Limiting** - Prevents API throttling
- **Translation Caching** - Reduces redundant API calls
- **Singleton HttpClient** - Prevents socket exhaustion

### Benchmarks

- **ISO Extraction** - ~500-1000 files/second (depends on disk speed)
- **Translation** - Limited by API rate (60 requests/minute default)
- **Caching** - 10,000+ translations loaded in <1 second

## Security

### Best Practices

- ✅ API keys stored in Windows Credential Manager
- ✅ `appsettings.json` excluded from version control
- ✅ Input validation on all user inputs
- ✅ Disk space validation before operations
- ✅ No hardcoded credentials in source code

### Configuration Files

- `appsettings.json` - **DO NOT COMMIT** (contains secrets)
- `appsettings.example.json` - Template for configuration

## Troubleshooting

### "API key is invalid"

- Verify your API key is correct in settings
- Check that you have credits/quota remaining
- Ensure your API key has the correct permissions

### "Insufficient disk space"

- Free up disk space on the target drive
- Choose a different extraction location
- The tool requires 110% of the ISO size for safety

### "Translation failed"

- Check your internet connection
- Verify API key and quotas
- Check the logs in `/Logs` folder
- Try a different translation provider

### UI Freezing

- If the UI freezes, it's a bug - please report it!
- ISO operations should be async and not block the UI
- Use the Cancel button to stop long operations

## Contributing

Contributions are welcome! Please follow these guidelines:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### Code Style

- Follow C# coding conventions
- Use meaningful variable names
- Add XML documentation to public APIs
- Write unit tests for new features
- Keep methods focused and small

## Roadmap

### Planned Features

- [ ] Support for additional translation providers (DeepL, Azure OpenAI)
- [ ] Custom dictionary/glossary support
- [ ] Translation memory (TMX format)
- [ ] Batch processing multiple ISOs
- [ ] Plugin system for custom file formats
- [ ] Cross-platform support (Avalonia UI)
- [ ] Web API for headless translation

### Known Issues

- Opening Notepad with TranslatableFiles.txt doesn't work on non-Windows
- Large ISOs (>4GB) may cause memory pressure
- Some special characters may not translate correctly

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- **DiscUtils** - For excellent ISO manipulation library
- **Serilog** - For structured logging
- **Polly** - For resilience patterns
- **xUnit** - For testing framework

## Support

- 🐛 **Bug Reports** - [GitHub Issues](https://github.com/tbmobb813/GameTranslationTool/issues)
- 💬 **Discussions** - [GitHub Discussions](https://github.com/tbmobb813/GameTranslationTool/discussions)
- 📧 **Email** - For security issues only

## Changelog

### [Unreleased]

#### Added
- Comprehensive unit test suite
- Disk space validation before ISO operations
- Visual progress bars for ISO operations
- Cancellation support for long-running operations
- Rate limiting for all translation API calls
- MVVM infrastructure and service layer
- Error handling service with user-friendly messages
- Translation caching service
- Settings persistence service
- Input validation service

#### Fixed
- Security vulnerability (removed hardcoded API key)
- HttpClient resource leaks (singleton pattern)
- Async/await anti-patterns (removed .Result calls)
- UI thread blocking during ISO operations
- Missing validation on user inputs

#### Changed
- ISO operations now fully async
- Improved error messages throughout
- Better progress reporting
- Organized code into proper layers (MVVM, Services, Models)

---

Made with ❤️ for game translation enthusiasts

---

## Python Translation Pipeline (v1)

A modular, cross-platform command-line pipeline for extracting, translating, and patching game assets.

### Pipeline Architecture

```
[Game Binary / Asset Bundle]
           ↓
 [Format Adapter Layer]
   adapters/base.py        ← BaseAdapter + TextEntry
   adapters/unity.py       ← Unity asset bundle handler
           ↓
 [Translation Engine]
   core/translator.py      ← DeepL / OpenAI / Ollama providers
   core/text_graph.py      ← Text object DAG representation
           ↓
 [Patch Generator]
   core/patcher.py         ← xdelta3 / IPS / BPS wrappers
```

### Python Project Structure

```
adapters/          # Plugin-based format handlers
  __init__.py
  base.py          # BaseAdapter interface + TextEntry dataclass
  unity.py         # Unity asset bundle (.assets, .unity3d) handler

core/              # Core pipeline logic
  __init__.py
  translator.py    # Multi-provider translation engine with glossary support
  patcher.py       # Patch generation (xdelta3, IPS, BPS)
  text_graph.py    # Directed-acyclic-graph text representation

utils/             # Shared utilities
  __init__.py
  encoding.py      # Shift-JIS ↔ UTF-8 encoding helpers
  validation.py    # Text overflow and control-code detection

cli/               # Command-line interface
  __init__.py
  main.py          # gtool CLI entry point (Click-based)

tests/             # Pytest test suite
  test_adapters.py
  test_translator.py
  fixtures/        # Sample JSON text dumps for testing

requirements.txt   # Python dependencies
```

### Quick Start (Python Pipeline)

#### Prerequisites

```bash
pip install -r requirements.txt
```

#### Install as a command-line tool

```bash
pip install -e .
```

#### Example Workflow – Unity Game Translation

```bash
# 1. Extract text from a Unity asset bundle
gtool extract resources.assets --adapter unity --output text_dump.json

# 2. Translate extracted text with DeepL
gtool translate text_dump.json \
    --provider deepl \
    --api-key $DEEPL_API_KEY \
    --glossary terms.json

# 3. Check for overflow / control-code issues
gtool validate text_dump.json --report warnings.txt

# 4. Rebuild the asset bundle with translations
gtool rebuild resources.assets text_dump.json --output translated/

# 5. Generate a binary patch
gtool patch resources.assets translated/resources.assets \
    --output game_english.xdelta
```

### Configuration

Create a `.env` file (or set environment variables) for API keys:

```
GTOOL_API_KEY=your_deepl_or_openai_key_here
```

Create a `glossary.json` for game-specific terminology:

```json
{
    "terms": {
        "ポーション": "Potion",
        "勇者": "Hero"
    },
    "preserve": [
        "\\{[A-Z_]+\\}",
        "\\\\n",
        "\\x1B\\[[0-9;]+m"
    ]
}
```

### Translation Providers

| Provider | Flag | Notes |
|----------|------|-------|
| DeepL    | `--provider deepl`  | Requires `--api-key` or `GTOOL_API_KEY` |
| OpenAI   | `--provider openai` | Requires `--api-key` or `GTOOL_API_KEY` |
| Ollama   | `--provider ollama` | Local model, no key required |

### Adapter Development Guide

Implement `BaseAdapter` from `adapters/base.py`:

```python
from adapters.base import BaseAdapter, TextEntry
from typing import List

class MyAdapter(BaseAdapter):
    def detect(self, file_path: str) -> bool:
        return file_path.endswith(".myformat")

    def extract(self, file_path: str) -> List[TextEntry]:
        ...

    def rebuild(self, file_path: str, entries: List[TextEntry], output_path: str) -> bool:
        ...
```

### Running Python Tests

```bash
pytest tests/ -v
```

