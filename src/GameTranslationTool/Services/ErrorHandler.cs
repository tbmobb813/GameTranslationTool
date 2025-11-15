using System;
using System.Windows;
using Serilog;

namespace GameTranslationTool.Services
{
    /// <summary>
    /// Centralized error handling with user-friendly messages
    /// </summary>
    public static class ErrorHandler
    {
        /// <summary>
        /// Shows a user-friendly error message and logs the exception
        /// </summary>
        public static void ShowError(string userMessage, Exception ex, string title = "Error")
        {
            Log.Error(ex, userMessage);
            MessageBox.Show(
                $"{userMessage}\n\nDetails: {ex.Message}",
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        /// <summary>
        /// Shows a simple error message
        /// </summary>
        public static void ShowError(string message, string title = "Error")
        {
            Log.Error(message);
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// Shows a warning message
        /// </summary>
        public static void ShowWarning(string message, string title = "Warning")
        {
            Log.Warning(message);
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>
        /// Shows an info message
        /// </summary>
        public static void ShowInfo(string message, string title = "Information")
        {
            Log.Information(message);
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Shows a confirmation dialog
        /// </summary>
        public static bool Confirm(string message, string title = "Confirm")
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }

        /// <summary>
        /// Handles file operation errors with context-specific messages
        /// </summary>
        public static void HandleFileError(string operation, string filePath, Exception ex)
        {
            var message = operation switch
            {
                "read" => $"Failed to read file: {filePath}",
                "write" => $"Failed to write file: {filePath}",
                "delete" => $"Failed to delete file: {filePath}",
                "create" => $"Failed to create file: {filePath}",
                _ => $"File operation '{operation}' failed: {filePath}"
            };

            ShowError(message, ex, "File Error");
        }

        /// <summary>
        /// Handles API errors with context-specific messages
        /// </summary>
        public static void HandleApiError(string apiName, Exception ex)
        {
            var message = $"API request to {apiName} failed. Please check:\n" +
                         "• Your API key is valid\n" +
                         "• You have internet connection\n" +
                         "• The API service is available";

            ShowError(message, ex, $"{apiName} Error");
        }

        /// <summary>
        /// Handles validation errors
        /// </summary>
        public static void HandleValidationError(string fieldName, string requirement)
        {
            ShowWarning(
                $"Invalid {fieldName}.\n\nRequirement: {requirement}",
                "Validation Error");
        }
    }
}
