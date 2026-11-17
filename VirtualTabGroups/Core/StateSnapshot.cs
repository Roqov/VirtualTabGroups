using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace VirtualTabGroups.Core
{
    /// <summary>
    /// Timestamped copies of state.json taken at startup. A corrupt file is already
    /// preserved by <see cref="StateStore"/>, but a file that parses yet holds a bad
    /// tree (a duplicate, an entry relinked to the wrong path, a lost write from a
    /// second instance) cannot be recovered from otherwise.
    /// </summary>
    public static class StateSnapshot
    {
        public const int DefaultKeep = 5;

        /// <summary>
        /// Copies <paramref name="stateFilePath"/> next to itself as
        /// "state.json.bak-yyyyMMdd-HHmmss-fff.json" unless the newest snapshot already
        /// holds the same bytes, then prunes old snapshots. Returns the snapshot path,
        /// or null when nothing was written. Never throws.
        /// </summary>
        public static string Take(string stateFilePath, int keep = DefaultKeep)
        {
            if (string.IsNullOrEmpty(stateFilePath) || !File.Exists(stateFilePath)) return null;

            var directory = Path.GetDirectoryName(stateFilePath);
            if (string.IsNullOrEmpty(directory)) return null;

            var prefix = Path.GetFileName(stateFilePath) + ".bak-";
            try
            {
                byte[] current;
                try { current = File.ReadAllBytes(stateFilePath); }
                catch (Exception) { return null; }
                if (current.Length == 0) return null;

                var existing = Directory.GetFiles(directory, prefix + "*.json")
                    .OrderByDescending(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                string created = null;
                if (existing.Count == 0 || !SameBytes(existing[0], current))
                {
                    // The random suffix keeps two starts in the same millisecond from
                    // overwriting each other; the timestamp still decides the order.
                    created = Path.Combine(directory,
                        prefix + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff")
                        + "-" + Path.GetRandomFileName().Replace(".", string.Empty).Substring(0, 4)
                        + ".json");
                    File.WriteAllBytes(created, current);
                    existing.Insert(0, created);
                }

                Prune(existing, keep);
                return created;
            }
            catch (Exception)
            {
                // Snapshots are best-effort: a full disk or a locked folder must not
                // stop the plugin from loading.
                return null;
            }
        }

        private static void Prune(List<string> snapshotsNewestFirst, int keep)
        {
            int limit = Math.Max(1, keep);
            for (int i = limit; i < snapshotsNewestFirst.Count; i++)
            {
                try { File.Delete(snapshotsNewestFirst[i]); }
                catch (Exception) { /* keep going: one locked snapshot must not stop the rest */ }
            }
        }

        private static bool SameBytes(string path, byte[] candidate)
        {
            try
            {
                var existing = File.ReadAllBytes(path);
                if (existing.Length != candidate.Length) return false;
                using (var sha = SHA256.Create())
                {
                    return sha.ComputeHash(existing).SequenceEqual(sha.ComputeHash(candidate));
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
