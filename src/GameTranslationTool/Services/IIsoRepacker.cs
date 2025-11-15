using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameTranslationTool.Services
{
    /// <summary>
    /// Service for repacking folders into ISO disc images
    /// </summary>
    public interface IIsoRepacker
    {
        /// <summary>
        /// Repacks a folder into an ISO file asynchronously with progress reporting and cancellation support
        /// </summary>
        Task RepackAsync(
            string sourceFolder,
            string outputIsoPath,
            string volumeLabel,
            IProgress<IsoProgress>? progress = null,
            CancellationToken cancellationToken = default);
    }
}
