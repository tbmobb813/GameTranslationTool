using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace GameTranslationTool.Services
{
    /// <summary>
    /// Secure implementation of file system operations with path traversal protection
    /// </summary>
    public class FileSystemService : IFileSystemService
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<FileSystemService>();

        // File operations
        public bool FileExists(string path) => File.Exists(path);

        public Task<string> ReadAllTextAsync(string path, CancellationToken ct = default)
            => File.ReadAllTextAsync(path, ct);

        public Task<string[]> ReadAllLinesAsync(string path, CancellationToken ct = default)
            => File.ReadAllLinesAsync(path, ct);

        public Task WriteAllTextAsync(string path, string content, CancellationToken ct = default)
            => File.WriteAllTextAsync(path, content, ct);

        public Task WriteAllLinesAsync(string path, IEnumerable<string> lines, CancellationToken ct = default)
            => File.WriteAllLinesAsync(path, lines, ct);

        public void DeleteFile(string path) => File.Delete(path);

        public long GetFileSize(string path)
        {
            try
            {
                return new FileInfo(path).Length;
            }
            catch
            {
                return 0;
            }
        }

        // Directory operations
        public bool DirectoryExists(string path) => Directory.Exists(path);

        public void CreateDirectory(string path) => Directory.CreateDirectory(path);

        public void DeleteDirectory(string path, bool recursive = false)
            => Directory.Delete(path, recursive);

        public string[] GetFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
            => Directory.GetFiles(path, searchPattern, searchOption);

        public long GetDirectorySize(string path)
        {
            try
            {
                var directory = new DirectoryInfo(path);
                long size = 0;

                foreach (var file in directory.GetFiles("*", SearchOption.AllDirectories))
                {
                    size += file.Length;
                }

                return size;
            }
            catch
            {
                return 0;
            }
        }

        // Disk operations
        public long GetAvailableDiskSpace(string path)
        {
            try
            {
                var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(path)) ?? "C:\\");
                return drive.AvailableFreeSpace;
            }
            catch
            {
                return 0;
            }
        }

        // Path operations
        public string CombinePath(params string[] paths) => Path.Combine(paths);

        public string GetFileName(string path) => Path.GetFileName(path);

        public string GetDirectoryName(string path) => Path.GetDirectoryName(path) ?? string.Empty;

        public string GetFullPath(string path) => Path.GetFullPath(path);

        /// <summary>
        /// Safely combines paths and validates against path traversal attacks
        /// </summary>
        public string SafeCombinePath(string basePath, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(basePath))
                throw new ArgumentException("Base path cannot be null or empty", nameof(basePath));

            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("Relative path cannot be null or empty", nameof(relativePath));

            // Normalize paths
            var normalizedBase = Path.GetFullPath(basePath);
            var combinedPath = Path.GetFullPath(Path.Combine(basePath, relativePath));

            // Check if the combined path is within the base path
            if (!combinedPath.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase))
            {
                Log.Error("Path traversal detected: Base={Base}, Relative={Relative}, Combined={Combined}",
                    basePath, relativePath, combinedPath);
                throw new SecurityException($"Path traversal detected: '{relativePath}' attempts to escape base directory");
            }

            return combinedPath;
        }
    }
}
