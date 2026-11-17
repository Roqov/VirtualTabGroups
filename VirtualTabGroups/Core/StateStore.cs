using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VirtualTabGroups.Core
{
    public sealed class StateStore : IDisposable
    {
        private readonly string _stateFilePath;
        private readonly IStateStoreObserver _observer;
        private readonly System.Threading.Timer _debounceTimer;
        private readonly TimeSpan _debounceInterval;
        private volatile bool _disposed;

        public StateStore(string stateFilePath, IStateStoreObserver observer = null)
            : this(stateFilePath, observer, TimeSpan.FromMilliseconds(500)) { }

        internal StateStore(string stateFilePath, IStateStoreObserver observer, TimeSpan debounce)
        {
            _stateFilePath = stateFilePath ?? throw new ArgumentNullException(nameof(stateFilePath));
            _observer = observer;
            _debounceInterval = debounce;
            _debounceTimer = new System.Threading.Timer(_ => OnDebounceFired(), null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
        }

        public Guid? LastSelectedId { get; private set; }
        public bool IsReadOnly { get; private set; }
        private const int CurrentSchemaVersion = 1;

        public FolderNode Load()
        {
            if (!File.Exists(_stateFilePath) || new FileInfo(_stateFilePath).Length == 0)
            {
                return new FolderNode("");
            }

            var settings = new JsonSerializerSettings { Converters = { new NodeJsonConverter() } };

            JObject envelope;
            try
            {
                var json = File.ReadAllText(_stateFilePath);
                envelope = JsonConvert.DeserializeObject<JObject>(json);
                if (envelope == null) throw new JsonSerializationException("empty envelope");
            }
            catch (Exception)
            {
                var backupPath = BackupCorruptFile();
                if (backupPath != null)
                {
                    try { _observer?.OnRecoveredFromCorruptFile(backupPath); }
                    catch { /* swallow observer exceptions — never let host code propagate through Load(). */ }
                }
                return new FolderNode("");
            }

            var schemaVersion = (int?)envelope["schemaVersion"] ?? CurrentSchemaVersion;
            if (schemaVersion > CurrentSchemaVersion)
            {
                IsReadOnly = true;
                try { _observer?.OnFutureSchemaVersion(schemaVersion, CurrentSchemaVersion); }
                catch { /* swallow observer exceptions — never let host code propagate through Load(). */ }
                return new FolderNode("");
            }

            LastSelectedId = null;
            var lastSelectedToken = (string)envelope["lastSelectedId"];
            if (lastSelectedToken != null && Guid.TryParse(lastSelectedToken, out var parsedId))
            {
                LastSelectedId = parsedId;
            }
            try
            {
                var rootToken = envelope["root"]
                    ?? throw new JsonException("state.json envelope is missing required 'root' property.");
                var rootNode = rootToken.ToObject<TreeNodeModel>(JsonSerializer.Create(settings));
                if (!(rootNode is FolderNode folder))
                {
                    throw new JsonException("state.json root must be a folder node, not a file.");
                }

                // The file parsed, so a copy is worth keeping before this session can
                // overwrite it; a tree damaged later in the session is otherwise gone.
                _knownStamp = StampOf(_stateFilePath);
                StateSnapshot.Take(_stateFilePath);
                return folder;
            }
            catch (Exception)
            {
                var backupPath = BackupCorruptFile();
                if (backupPath != null)
                {
                    try { _observer?.OnRecoveredFromCorruptFile(backupPath); }
                    catch { /* swallow observer exceptions — never let host code propagate through Load(). */ }
                }
                LastSelectedId = null;
                return new FolderNode("");
            }
        }

        private string _pendingJson;
        private readonly object _writeLock = new object();

        // Signature (length + last write time) of the file as this session last saw it.
        // A different signature before a write means somebody else — normally a second
        // Notepad++ instance sharing the config folder — wrote the file in between.
        private string _knownStamp;

        public void MarkDirty(FolderNode root, Guid? selectedId = null)
        {
            if (_disposed) return;
            if (IsReadOnly) return;
            if (root == null) throw new ArgumentNullException(nameof(root));

            var settings = new JsonSerializerSettings
            {
                Converters = { new NodeJsonConverter() },
            };

            var envelope = new JObject
            {
                ["schemaVersion"] = CurrentSchemaVersion,
                ["lastSelectedId"] = selectedId.HasValue ? selectedId.Value.ToString() : null,
                ["root"] = JToken.FromObject(root, JsonSerializer.Create(settings)),
            };

            var json = envelope.ToString(Formatting.Indented);
            lock (_writeLock)
            {
                _pendingJson = json;
            }

            _debounceTimer.Change(_debounceInterval, System.Threading.Timeout.InfiniteTimeSpan);
        }

        private void WritePendingNow()
        {
            string json;
            lock (_writeLock)
            {
                json = _pendingJson;
                if (json == null) return;
                // Do NOT clear _pendingJson yet — only clear on successful write.
            }

            // The temp file name must be unique per attempt: a second instance writing
            // "state.json.tmp" at the same moment would make File.Replace fail or
            // replace our own data with the other window's.
            var tmpPath = _stateFilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                // A changed signature means another process owns the current file. Keep
                // its bytes next to ours instead of overwriting them silently; the user
                // is told where they went.
                var diskStamp = StampOf(_stateFilePath);
                if (_knownStamp != null && diskStamp != null && diskStamp != _knownStamp)
                {
                    var foreignCopy = CopyForeignStateFile();
                    if (foreignCopy != null)
                    {
                        try { _observer?.OnForeignStateDetected(foreignCopy); }
                        catch { /* swallow observer exceptions — timer thread */ }
                    }
                }

                File.WriteAllText(tmpPath, json, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

                if (File.Exists(_stateFilePath))
                {
                    File.Replace(tmpPath, _stateFilePath, destinationBackupFileName: null);
                }
                else
                {
                    File.Move(tmpPath, _stateFilePath);
                }

                _knownStamp = StampOf(_stateFilePath);

                lock (_writeLock)
                {
                    // Only clear if no new MarkDirty raced in with a newer string.
                    if (_pendingJson == json) _pendingJson = null;
                }
            }
            catch (Exception ex)
            {
                try { _observer?.OnSaveFailed(ex); }
                catch { /* swallow observer exceptions — never let host code crash the timer thread. */ }
                // Leave _pendingJson set so the next MarkDirty or Flush retries.
            }
            finally
            {
                try
                {
                    if (File.Exists(tmpPath)) File.Delete(tmpPath);
                }
                catch { /* a stale temp file is harmless; the next write uses a new name */ }
            }
        }

        private void OnDebounceFired() => WritePendingNow();

        /// <summary>
        /// Length plus last write time is enough to notice a foreign write without
        /// rehashing the whole file on every save.
        /// </summary>
        private static string StampOf(string path)
        {
            try
            {
                var info = new FileInfo(path);
                return info.Exists
                    ? info.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)
                        + ":" + info.LastWriteTimeUtc.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string CopyForeignStateFile()
        {
            var directory = Path.GetDirectoryName(_stateFilePath);
            if (string.IsNullOrEmpty(directory)) return null;

            var name = Path.GetFileName(_stateFilePath) + ".foreign-"
                + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + "-"
                + Path.GetRandomFileName().Replace(".", string.Empty) + ".json";
            var target = Path.Combine(directory, name);

            try
            {
                File.Copy(_stateFilePath, target, overwrite: false);
                return target;
            }
            catch (Exception)
            {
                // The copy is a courtesy; never let it stop this session from saving.
                return null;
            }
        }

        public void Flush()
        {
            if (_disposed) return;
            if (IsReadOnly) return;
            _debounceTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            WritePendingNow();
        }

        private string BackupCorruptFile()
        {
            // Random suffix guarantees uniqueness; no File.Exists race.
            var randomToken = Path.GetRandomFileName().Replace(".", "");
            var suffix = ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmssZ") + "-" + randomToken;
            var backupPath = _stateFilePath + suffix;

            try
            {
                File.Move(_stateFilePath, backupPath);
                return backupPath;
            }
            catch (Exception)
            {
                // Locked file, denied permissions, etc. — recovery still proceeds without a backup.
                return null;
            }
        }

        /// <summary>
        /// Flushes any pending write synchronously and releases the debounce timer.
        /// Safe to call multiple times from the same thread (idempotent), but NOT thread-safe:
        /// concurrent calls from multiple threads are not supported. Notepad++ plugins run on
        /// the main UI thread by construction, so this is not a practical concern in production.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            try { Flush(); }
            finally
            {
                _disposed = true;
                _debounceTimer.Dispose();
            }
        }
    }
}
