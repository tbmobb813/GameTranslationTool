using System;
using Serilog;
using System.IO;
using System.Linq;
using DiscUtils.Iso9660;
using System.Linq.Expressions;

namespace GameTranslationTool.ISO
{
    public static class IsoRepacker
    {
        public static void RepackIso(string sourceFolder, string outputIsoPath, string volumeLabel)
        {
            if (!Directory.Exists(sourceFolder))
                throw new DirectoryNotFoundException($"Source folder does not exist: {sourceFolder}");

            try
            {
                using FileStream isoStream = File.Create(outputIsoPath);
                var builder = new CDBuilder
                {
                    UseJoliet = true,
                    VolumeIdentifier = volumeLabel
                };

                foreach (string filePath in Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories))
                {
                    string relativePath = Path.GetRelativePath(sourceFolder, filePath).Replace('\\', '/');
                    builder.AddFile(relativePath, filePath);
                }

                builder.Build(isoStream);

                Log.Information("ISO repacked to: {Path}", outputIsoPath);
            }
            catch (Exception ex)
            {
                Log.Error("Failed to repack ISO: {Message}", ex.Message);
            }
        }
    }
        
}
