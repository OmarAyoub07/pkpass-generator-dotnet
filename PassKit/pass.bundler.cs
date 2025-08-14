using System;
using System.IO;
using System.IO.Compression;

namespace Passkit
{
    public static class PassBundler
    {
        /// <summary>
        /// Zips the pass folder into a .pkpass bundle (ZIP) with files at the root.
        /// Verifies required files exist: pass.json, manifest.json, signature.
        /// </summary>
        /// <param name="passFolderPath">Path to the pass directory containing pass.json, manifest.json, signature, images…</param>
        /// <param name="outputPkpassPath">Optional output path. If null, writes <parent>/<foldername>.pkpass</param>
        /// <param name="overwrite">Overwrite existing .pkpass if present</param>
        /// <returns>The full path to the written .pkpass file.</returns>
        public static string CreatePkPass(string passFolderPath, string outputPkpassPath, bool overwrite = true)
        {
            if (string.IsNullOrWhiteSpace(passFolderPath) || !Directory.Exists(passFolderPath))
                throw new DirectoryNotFoundException($"Pass folder not found: {passFolderPath}");

            // Minimal required files per spec
            var required = new[] { "pass.json", "manifest.json", "signature", "icon.png", "icon@2x.png"};
            foreach (var name in required)
            {
                var p = Path.Combine(passFolderPath, name);
                if (!File.Exists(p))
                    throw new FileNotFoundException($"Required file missing: {name}", p);
            }

            if (File.Exists(outputPkpassPath))
            {
                if (!overwrite) throw new IOException($"File exists: {outputPkpassPath}");
                File.Delete(outputPkpassPath);
            }

            // Build ZIP at the root (no enclosing directory)
            using var fs = new FileStream(outputPkpassPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            using var zip = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: false);

            foreach (var file in Directory.EnumerateFiles(passFolderPath, "*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(passFolderPath, file)
                              .Replace(Path.DirectorySeparatorChar, '/');

                // Skip junk
                var fname = Path.GetFileName(rel);
                if (string.Equals(fname, ".DS_Store", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(fname, "Thumbs.db", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var entry = zip.CreateEntry(rel, CompressionLevel.Optimal);
                entry.LastWriteTime = File.GetLastWriteTimeUtc(file);

                using var src = File.OpenRead(file);
                using var dst = entry.Open();
                src.CopyTo(dst);
            }

            return outputPkpassPath;
        }
    }
}
