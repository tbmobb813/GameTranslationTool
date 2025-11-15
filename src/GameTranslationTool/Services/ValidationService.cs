using System;
using System.IO;

namespace GameTranslationTool.Services
{
    /// <summary>
    /// Provides input validation for the application
    /// </summary>
    public static class ValidationService
    {
        public static bool IsValidFilePath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                _ = Path.GetFullPath(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsValidDirectoryPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                _ = Path.GetFullPath(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool FileExists(string? path)
        {
            return IsValidFilePath(path) && File.Exists(path);
        }

        public static bool DirectoryExists(string? path)
        {
            return IsValidDirectoryPath(path) && Directory.Exists(path);
        }

        public static bool IsValidApiKey(string? apiKey)
        {
            return !string.IsNullOrWhiteSpace(apiKey) && apiKey.Length >= 10;
        }

        public static bool IsValidLanguageCode(string? languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
                return false;

            return languageCode.Length == 2 && languageCode == languageCode.ToLower();
        }

        public static bool IsValidRegion(string? region)
        {
            return !string.IsNullOrWhiteSpace(region) && region.Length >= 2;
        }

        public static bool IsPositiveInteger(string? text, out int value)
        {
            value = 0;
            return int.TryParse(text, out value) && value > 0;
        }

        /// <summary>
        /// Gets available disk space in bytes for the drive containing the specified path
        /// </summary>
        public static long GetAvailableDiskSpace(string path)
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

        /// <summary>
        /// Checks if there's enough disk space available (with 10% safety margin)
        /// </summary>
        public static bool HasSufficientDiskSpace(string path, long requiredBytes)
        {
            var available = GetAvailableDiskSpace(path);
            var requiredWithMargin = (long)(requiredBytes * 1.1); // 10% safety margin
            return available >= requiredWithMargin;
        }

        /// <summary>
        /// Gets the size of a file in bytes
        /// </summary>
        public static long GetFileSize(string filePath)
        {
            try
            {
                return new FileInfo(filePath).Length;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets the total size of all files in a directory (recursive)
        /// </summary>
        public static long GetDirectorySize(string directoryPath)
        {
            try
            {
                var directory = new DirectoryInfo(directoryPath);
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

        /// <summary>
        /// Formats bytes into human-readable string (e.g., "1.5 GB")
        /// </summary>
        public static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}
