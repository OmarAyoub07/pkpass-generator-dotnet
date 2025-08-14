using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Passkit
{
    /// <summary>
    /// Generates manifest.json for an Apple Wallet pass (.pkpass).
    /// Uses SHA-1 hashes as expected by Wallet for pass manifests.
    /// Excludes "manifest.json" and "signature" (and common OS junk files).
    /// </summary>
    public static class PassManifest
    {
        /// <summary>
        /// Build the manifest for the pass at <paramref name="passFolderPath"/>.
        /// Returns the full path to the written manifest.json.
        /// </summary>
        /// <param name="passFolderPath">Folder containing pass.json, images, etc.</param>
        /// <param name="outputPath">Optional explicit path for manifest.json; default writes inside the pass folder.</param>
        /// <param name="additionalIgnores">Optional extra file names to exclude (case-insensitive, file-name only).</param>
        public static string Build(string passFolderPath, string outputPath)
        {
            if (string.IsNullOrWhiteSpace(passFolderPath))
                throw new ArgumentException("Pass folder path is required.", nameof(passFolderPath));

            if (!Directory.Exists(passFolderPath))
                throw new DirectoryNotFoundException($"Pass folder not found: {passFolderPath}");

            // Files to ignore by file name (top-level or nested); case-insensitive
            var ignore = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "manifest.json",
                "signature",
                ".ds_store",
                "thumbs.db",
            };

            // Build a deterministic (sorted) map: relativePath -> sha1Hex
            var manifest = new SortedDictionary<string, string>(StringComparer.Ordinal);

            foreach (var file in Directory.EnumerateFiles(passFolderPath, "*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(passFolderPath, file);
                var fileName = Path.GetFileName(rel);
                if (ignore.Contains(fileName))
                    continue;

                // Compute SHA-1 over raw file bytes
                using var stream = File.OpenRead(file);
                using var sha1 = SHA1.Create();
                var hash = sha1.ComputeHash(stream);
                var hex = ToHex(hash);

                manifest[rel] = hex;
            }

            var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            // UTF-8 without BOM
            File.WriteAllText(outputPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            return outputPath;
        }

        private static string ToHex(ReadOnlySpan<byte> bytes)
        {
            var c = new char[bytes.Length * 2];
            int i = 0;
            foreach (var b in bytes)
            {
                c[i++] = GetHexNibble(b >> 4);
                c[i++] = GetHexNibble(b & 0xF);
            }
            return new string(c);

            static char GetHexNibble(int val) => (char)(val < 10 ? ('0' + val) : ('a' + (val - 10)));
        }
    }
}
