using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using QuackQuack.Core.Progression;
using UnityEngine;

namespace QuackQuack.Systems.Save
{
    public sealed class JsonSaveService
    {
        public static int CurrentSchemaVersion => 1;
        private const long MaximumBytes = 1024 * 1024;
        private readonly string path;
        private readonly HashSet<string> known;
        private readonly object gate = new object();
        private enum FileState { Invalid, Valid, Future, Unavailable }

        [Serializable] private sealed class Document
        {
            public int schemaVersion;
            public string[] unlockedRelicIds;
            public string[] equippedRelicIds;
        }

        public JsonSaveService(string path, string[] knownRelicIds)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A save file path is required.", nameof(path));
            this.path = Path.GetFullPath(path);
            if (knownRelicIds == null) throw new ArgumentNullException(nameof(knownRelicIds));
            known = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in knownRelicIds)
                if (string.IsNullOrWhiteSpace(id) || !known.Add(id)) throw new ArgumentException("Known relic IDs must be nonempty and unique.");
            if (known.Count != 10) throw new ArgumentException("The prototype requires ten known relics.");
        }

        public SaveData CreateDefault() => new SaveData(CurrentSchemaVersion, Array.Empty<string>(), Array.Empty<string>());

        public SaveOperationResult Load()
        {
            lock (gate)
            {
                if (Read(path, out var main) == FileState.Valid) return main;
                if (Read(path + ".bak", out var backup) == FileState.Valid)
                    return Result(backup.Data, SaveStatus.Recovered, "Recovered from backup; the main file was not modified.");
                return Result(CreateDefault(), SaveStatus.Defaulted, "No supported valid save was readable; defaults are available.");
            }
        }

        // Version 0 used the unlock list and optionally equipment. Missing equipment migrates to empty.
        // Future versions must never be silently downgraded or replaced.
        public SaveOperationResult Migrate(SaveData data)
        {
            if (data == null || data.SchemaVersion < 0 || data.SchemaVersion > CurrentSchemaVersion)
                return Result(CreateDefault(), SaveStatus.Failed, "Unsupported save schema.");
            bool changed = data.SchemaVersion != CurrentSchemaVersion;
            var unlocked = Filter(data.UnlockedRelicIds, known, int.MaxValue, ref changed);
            var equipped = Filter(data.EquippedRelicIds, new HashSet<string>(unlocked, StringComparer.Ordinal), RelicLoadout.SlotLimit, ref changed);
            return Result(new SaveData(CurrentSchemaVersion, unlocked, equipped), changed ? SaveStatus.Recovered : SaveStatus.Success,
                changed ? "Save migrated or relic IDs normalized." : "Save data validated.");
        }

        public SaveOperationResult Save(SaveData data)
        {
            lock (gate)
            {
                var migrated = Migrate(data);
                return migrated.Status == SaveStatus.Failed ? migrated : Write(migrated, false);
            }
        }

        public SaveOperationResult Save(RelicLoadout loadout) => loadout == null
            ? Result(CreateDefault(), SaveStatus.Failed, "A loadout is required.")
            : Save(new SaveData(CurrentSchemaVersion, loadout.UnlockedIds, loadout.EquippedIds));

        public SaveOperationResult RestoreBackup()
        {
            lock (gate)
            {
                if (Read(path + ".bak", out var backup) != FileState.Valid)
                    return Result(CreateDefault(), SaveStatus.Failed, "No supported valid backup was readable.");
                var written = Write(backup, true);
                return written.Status == SaveStatus.Failed ? written : Result(written.Data, SaveStatus.Recovered, "Backup restored atomically.");
            }
        }

        private SaveOperationResult Write(SaveOperationResult data, bool preserveBackup)
        {
            string temporary = null;
            try
            {
                FileState previous = Read(path, out _);
                if (previous == FileState.Future || previous == FileState.Unavailable)
                    return Result(data.Data, SaveStatus.Failed, "Newer or unreadable save preserved; write refused.");
                FileState backupState = Read(path + ".bak", out _);
                bool preserveExistingBackup = backupState == FileState.Future || backupState == FileState.Unavailable;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                var document = new Document { schemaVersion = CurrentSchemaVersion,
                    unlockedRelicIds = new List<string>(data.Data.UnlockedRelicIds).ToArray(),
                    equippedRelicIds = new List<string>(data.Data.EquippedRelicIds).ToArray() };
                byte[] bytes = new UTF8Encoding(false, true).GetBytes(JsonUtility.ToJson(document, true));
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                // Both paths are on the same volume. Never delete the main file before replacing it.
                if (File.Exists(path))
                    File.Replace(temporary, path, previous == FileState.Valid && !preserveBackup && !preserveExistingBackup ? path + ".bak" : null);
                else File.Move(temporary, path);
                temporary = null;
                return Result(data.Data, data.Status, data.Status == SaveStatus.Recovered ? "Save normalized and committed atomically." : "Save committed atomically.");
            }
            catch (Exception error) when (IsStorageError(error))
            { return Result(data.Data, SaveStatus.Failed, "Save could not be committed; previous files were preserved where supported by the filesystem."); }
            finally
            {
                // Only this operation's unique temporary is eligible for cleanup.
                if (temporary != null)
                    try { File.Delete(temporary); } catch (Exception error) when (IsStorageError(error)) { }
            }
        }

        private FileState Read(string file, out SaveOperationResult result)
        {
            result = null;
            try
            {
                using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (stream.Length == 0) return FileState.Invalid;
                if (stream.Length > MaximumBytes) return FileState.Unavailable;
                using (var textReader = new StreamReader(stream, new UTF8Encoding(false, true), true, 4096, true))
                    if (!CompleteObject(textReader.ReadToEnd())) return FileState.Invalid;
                stream.Position = 0;
                var quotas = new XmlDictionaryReaderQuotas { MaxDepth = 32, MaxArrayLength = 1024, MaxStringContentLength = 65536 };
                using var reader = JsonReaderWriterFactory.CreateJsonReader(stream, quotas);
                var root = XDocument.Load(reader).Root;
                if (root == null || (string)root.Attribute("type") != "object") return FileState.Invalid;
                var versions = new List<XElement>(root.Elements("schemaVersion"));
                if (versions.Count != 1 || (string)versions[0].Attribute("type") != "number"
                    || !int.TryParse(versions[0].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int version)
                    || versions[0].Value != version.ToString(CultureInfo.InvariantCulture)) return FileState.Invalid;
                if (version > CurrentSchemaVersion) return FileState.Future;
                if (!ReadIds(root, "unlockedRelicIds", out var unlocked, out bool repairedUnlocks)
                    || !ReadIds(root, "equippedRelicIds", out var equipped, out bool repairedEquipment)) return FileState.Invalid;
                var migrated = Migrate(new SaveData(version, unlocked, equipped));
                result = repairedUnlocks || repairedEquipment ? Result(migrated.Data, SaveStatus.Recovered, "Missing or null relic lists recovered.") : migrated;
                return FileState.Valid;
            }
            catch (Exception error) when (IsStorageError(error) || error is XmlException || error is SerializationException || error is FormatException)
            {
                return error is FileNotFoundException || error is DirectoryNotFoundException ? FileState.Invalid
                    : error is IOException || error is UnauthorizedAccessException || error is System.Security.SecurityException
                        ? FileState.Unavailable : FileState.Invalid;
            }
        }

        // The framework JSON reader accepts EOF with an unclosed object. Check framing first;
        // token parsing/decoding remains the framework reader's responsibility.
        private static bool CompleteObject(string text)
        {
            text = text.Trim();
            if (text.Length < 2 || text[0] != '{' || text[text.Length - 1] != '}') return false;
            var stack = new char[32]; int depth = 0; bool quoted = false, escaped = false; char previous = '\0';
            for (int i = 0; i < text.Length; i++)
            {
                char value = text[i];
                if (quoted)
                {
                    if (value < 0x20) return false;
                    if (escaped) { escaped = false; continue; }
                    if (value == '\\') { escaped = true; continue; }
                    if (value == '"') quoted = false;
                    continue;
                }
                if (char.IsWhiteSpace(value)) continue;
                if (value == '"') quoted = true;
                else if (value == '{' || value == '[')
                {
                    if (depth == stack.Length) return false;
                    stack[depth++] = value;
                }
                else if (value == '}' || value == ']')
                {
                    if (depth == 0 || previous == ',' || stack[--depth] != (value == '}' ? '{' : '[')) return false;
                    if (depth == 0 && i != text.Length - 1) return false;
                }
                else if (value == '/') return false; // JSON does not contain comments.
                previous = value;
            }
            return depth == 0 && !quoted && !escaped;
        }

        private static bool ReadIds(XElement root, string name, out List<string> ids, out bool repaired)
        {
            ids = new List<string>(); repaired = false;
            var fields = new List<XElement>(root.Elements(name));
            if (fields.Count == 0 || (fields.Count == 1 && (string)fields[0].Attribute("type") == "null"))
            { repaired = true; return true; }
            if (fields.Count != 1 || (string)fields[0].Attribute("type") != "array") return false;
            foreach (var item in fields[0].Elements())
            {
                string type = (string)item.Attribute("type");
                if (type == "null") { ids.Add(null); continue; }
                if (type != "string") return false;
                ids.Add(item.Value);
            }
            return true;
        }

        private static List<string> Filter(IEnumerable<string> ids, HashSet<string> allowed, int maximum, ref bool changed)
        {
            var result = new List<string>(); var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in ids)
            {
                if (id == null || !allowed.Contains(id) || !seen.Add(id) || result.Count >= maximum) { changed = true; continue; }
                result.Add(id);
            }
            return result;
        }
        private static SaveOperationResult Result(SaveData data, SaveStatus status, string diagnostic) => new SaveOperationResult(data, status, diagnostic);
        private static bool IsStorageError(Exception error) => error is IOException || error is UnauthorizedAccessException
            || error is System.Security.SecurityException || error is NotSupportedException || error is ArgumentException;
    }
}
