using System.IO;
using GameTranslationTool.Services;
using Xunit;

namespace GameTranslationTool.Tests.Services
{
    public class ValidationServiceTests
    {
        [Fact]
        public void IsValidFilePath_WithValidPath_ReturnsTrue()
        {
            // Arrange
            var validPath = @"C:\test\file.txt";

            // Act
            var result = ValidationService.IsValidFilePath(validPath);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void IsValidFilePath_WithInvalidPath_ReturnsFalse(string? invalidPath)
        {
            // Act
            var result = ValidationService.IsValidFilePath(invalidPath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidDirectoryPath_WithValidPath_ReturnsTrue()
        {
            // Arrange
            var validPath = @"C:\test\folder";

            // Act
            var result = ValidationService.IsValidDirectoryPath(validPath);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void IsValidDirectoryPath_WithInvalidPath_ReturnsFalse(string? invalidPath)
        {
            // Act
            var result = ValidationService.IsValidDirectoryPath(invalidPath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void FileExists_WithExistingFile_ReturnsTrue()
        {
            // Arrange - Create a temp file
            var tempFile = Path.GetTempFileName();

            try
            {
                // Act
                var result = ValidationService.FileExists(tempFile);

                // Assert
                Assert.True(result);
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public void FileExists_WithNonExistingFile_ReturnsFalse()
        {
            // Arrange
            var nonExistentFile = Path.Combine(Path.GetTempPath(), "nonexistent_file_12345.txt");

            // Act
            var result = ValidationService.FileExists(nonExistentFile);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void DirectoryExists_WithExistingDirectory_ReturnsTrue()
        {
            // Arrange
            var tempDir = Directory.GetCurrentDirectory();

            // Act
            var result = ValidationService.DirectoryExists(tempDir);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void DirectoryExists_WithNonExistingDirectory_ReturnsFalse()
        {
            // Arrange
            var nonExistentDir = Path.Combine(Path.GetTempPath(), "nonexistent_dir_12345");

            // Act
            var result = ValidationService.DirectoryExists(nonExistentDir);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("1234567890", true)]  // 10 chars - minimum
        [InlineData("12345678901234567890", true)]  // 20 chars
        [InlineData("AlzaSyAdVmpdO8yhplpdh8vlgTjldvjV7AKYCGM", true)]  // Real API key format
        public void IsValidApiKey_WithValidKeys_ReturnsTrue(string apiKey, bool expected)
        {
            // Act
            var result = ValidationService.IsValidApiKey(apiKey);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData("123456789", false)]  // Only 9 chars - too short
        public void IsValidApiKey_WithInvalidKeys_ReturnsFalse(string? apiKey, bool expected)
        {
            // Act
            var result = ValidationService.IsValidApiKey(apiKey);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("en", true)]
        [InlineData("es", true)]
        [InlineData("fr", true)]
        [InlineData("de", true)]
        [InlineData("ja", true)]
        public void IsValidLanguageCode_WithValidCodes_ReturnsTrue(string langCode, bool expected)
        {
            // Act
            var result = ValidationService.IsValidLanguageCode(langCode);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData("e", false)]  // Too short
        [InlineData("eng", false)]  // Too long
        [InlineData("EN", false)]  // Uppercase
        [InlineData("En", false)]  // Mixed case
        public void IsValidLanguageCode_WithInvalidCodes_ReturnsFalse(string? langCode, bool expected)
        {
            // Act
            var result = ValidationService.IsValidLanguageCode(langCode);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("eastus", true)]
        [InlineData("westus", true)]
        [InlineData("us", true)]  // Minimum 2 chars
        public void IsValidRegion_WithValidRegions_ReturnsTrue(string region, bool expected)
        {
            // Act
            var result = ValidationService.IsValidRegion(region);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData("a", false)]  // Too short
        public void IsValidRegion_WithInvalidRegions_ReturnsFalse(string? region, bool expected)
        {
            // Act
            var result = ValidationService.IsValidRegion(region);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("1", 1, true)]
        [InlineData("100", 100, true)]
        [InlineData("999999", 999999, true)]
        public void IsPositiveInteger_WithValidIntegers_ReturnsTrueAndValue(string text, int expectedValue, bool expectedResult)
        {
            // Act
            var result = ValidationService.IsPositiveInteger(text, out int value);

            // Assert
            Assert.Equal(expectedResult, result);
            Assert.Equal(expectedValue, value);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("abc")]
        [InlineData("0")]  // Zero is not positive
        [InlineData("-1")]  // Negative
        [InlineData("1.5")]  // Decimal
        public void IsPositiveInteger_WithInvalidInputs_ReturnsFalse(string? text)
        {
            // Act
            var result = ValidationService.IsPositiveInteger(text, out int value);

            // Assert
            Assert.False(result);
            Assert.Equal(0, value);
        }

        [Fact]
        public void GetFileSize_WithExistingFile_ReturnsCorrectSize()
        {
            // Arrange - Create a temp file with known content
            var tempFile = Path.GetTempFileName();
            var testContent = "Hello, World!";
            File.WriteAllText(tempFile, testContent);
            var expectedSize = new FileInfo(tempFile).Length;

            try
            {
                // Act
                var result = ValidationService.GetFileSize(tempFile);

                // Assert
                Assert.Equal(expectedSize, result);
                Assert.True(result > 0);
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public void GetFileSize_WithNonExistingFile_ReturnsZero()
        {
            // Arrange
            var nonExistentFile = Path.Combine(Path.GetTempPath(), "nonexistent_12345.txt");

            // Act
            var result = ValidationService.GetFileSize(nonExistentFile);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void GetDirectorySize_WithDirectory_ReturnsCorrectSize()
        {
            // Arrange - Create a temp directory with files
            var tempDir = Path.Combine(Path.GetTempPath(), $"test_dir_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            var file1 = Path.Combine(tempDir, "file1.txt");
            var file2 = Path.Combine(tempDir, "file2.txt");
            File.WriteAllText(file1, "Hello");
            File.WriteAllText(file2, "World");

            var expectedSize = new FileInfo(file1).Length + new FileInfo(file2).Length;

            try
            {
                // Act
                var result = ValidationService.GetDirectorySize(tempDir);

                // Assert
                Assert.Equal(expectedSize, result);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void FormatBytes_FormatsCorrectly()
        {
            // Arrange & Act & Assert
            Assert.Equal("0 B", ValidationService.FormatBytes(0));
            Assert.Equal("1 B", ValidationService.FormatBytes(1));
            Assert.Equal("1023 B", ValidationService.FormatBytes(1023));
            Assert.Equal("1 KB", ValidationService.FormatBytes(1024));
            Assert.Equal("1.5 KB", ValidationService.FormatBytes(1536));
            Assert.Equal("1 MB", ValidationService.FormatBytes(1024 * 1024));
            Assert.Equal("1.5 GB", ValidationService.FormatBytes((long)(1.5 * 1024 * 1024 * 1024)));
            Assert.Equal("2 TB", ValidationService.FormatBytes(2L * 1024 * 1024 * 1024 * 1024));
        }

        [Fact]
        public void GetAvailableDiskSpace_ReturnsPositiveValue()
        {
            // Arrange
            var currentDir = Directory.GetCurrentDirectory();

            // Act
            var result = ValidationService.GetAvailableDiskSpace(currentDir);

            // Assert
            Assert.True(result > 0, "Available disk space should be positive");
        }

        [Fact]
        public void HasSufficientDiskSpace_WithEnoughSpace_ReturnsTrue()
        {
            // Arrange
            var currentDir = Directory.GetCurrentDirectory();
            var requiredBytes = 1024L; // 1 KB - very small, should always have this

            // Act
            var result = ValidationService.HasSufficientDiskSpace(currentDir, requiredBytes);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasSufficientDiskSpace_WithInsufficientSpace_ReturnsFalse()
        {
            // Arrange
            var currentDir = Directory.GetCurrentDirectory();
            var requiredBytes = long.MaxValue; // Ridiculously large amount

            // Act
            var result = ValidationService.HasSufficientDiskSpace(currentDir, requiredBytes);

            // Assert
            Assert.False(result);
        }
    }
}
