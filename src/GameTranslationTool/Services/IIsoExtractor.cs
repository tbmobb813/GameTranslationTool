using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameTranslationTool.Services
{
    /// <summary>
    /// Progress information for ISO operations
    /// </summary>
    public class IsoProgress
    {
        public int Current { get; set; }
        public int Total { get; set; }
        public string CurrentFile { get; set; } = string.Empty;
        public double Percentage => Total > 0 ? (double)Current / Total * 100 : 0;
    }

    /// <summary>
    /// Service for extracting ISO disc images
    /// </summary>
    public interface IIsoExtractor
    {
        /// <summary>
        /// Extracts an ISO to disk asynchronously with progress reporting and cancellation support
        /// </summary>
        Task ExtractAsync(
            string isoPath,
            string outputFolder,
            string translatedFolder,
            IProgress<IsoProgress>? progress = null,
            CancellationToken cancellationToken = default);
    }
}
