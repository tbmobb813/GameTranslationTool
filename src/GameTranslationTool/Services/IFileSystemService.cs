using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GameTranslationTool.Services
{
    /// <summary>
    /// Abstraction over file system operations for testability and security
    /// </summary>
    public interface IFileSystemService
    {
        // File operations
        bool FileExists(string path);
        Task<string> ReadAllTextAsync(string path, CancellationToken ct = default);
        Task<string[]> ReadAllLinesAsync(string path, CancellationToken ct = default);
        Task WriteAllTextAsync(string path, string content, CancellationToken ct = default);
        Task WriteAllLinesAsync(string path, IEnumerable<string> lines, CancellationToken ct = default);
        void DeleteFile(string path);
        long GetFileSize(string path);

        // Directory operations
        bool DirectoryExists(string path);
        void CreateDirectory(string path);
        void DeleteDirectory(string path, bool recursive = false);
        string[] GetFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);
        long GetDirectorySize(string path);

        // Disk operations
        long GetAvailableDiskSpace(string path);

        // Path operations
        string CombinePath(params string[] paths);
        string GetFileName(string path);
        string GetDirectoryName(string path);
        string GetFullPath(string path);

        /// <summary>
        /// Safely combines paths and validates against path traversal attacks
        /// </summary>
        string SafeCombinePath(string basePath, string relativePath);
    }
}
