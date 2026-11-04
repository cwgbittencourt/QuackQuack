using System;
using System.IO;
using UnityEngine;

namespace QuackQuack.Platform.Input
{
    // Separate from progression. A missing/corrupt/future file falls back without touching progress.
    public sealed class ControlProfileStore
    {
        public const string FileName = "control-profile-v1.json";
        private static ControlProfileStore shared;
        public static ControlProfileStore Shared => shared ?? (shared = new ControlProfileStore(Application.persistentDataPath));
        [Serializable] private sealed class Document { public int version = -1; public int family = -1; public string quack, pause, relics; }
        private readonly string path;
        private ControlProfile current;
        public event Action Changed;
        public ControlProfile Current => current.Copy();
        public bool Recovered { get; private set; }
        public ControlProfileStore(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("Profile directory required.");
            path = Path.Combine(directory, FileName);
            current = Read(path);
            if (current == null) { current = Read(path + ".bak") ?? new ControlProfile(); Recovered = File.Exists(path) || File.Exists(path + ".bak"); }
        }
        private static ControlProfile Read(string file)
        {
            try {
                if (!File.Exists(file) || new FileInfo(file).Length > 8192) return null;
                var json = File.ReadAllText(file);
                // JsonUtility supplies field initializers for omitted fields: reject partial documents.
                foreach (var key in new[] { "version", "family", "quack", "pause", "relics" })
                    if (json.IndexOf("\"" + key + "\"", StringComparison.Ordinal) < 0) return null;
                var document = JsonUtility.FromJson<Document>(json);
                if (document == null) return null;
                var profile = ControlProfile.FromStored(document.version, (ControllerFamily)document.family, document.quack, document.pause, document.relics);
                return profile.IsValid() ? profile : null;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException) { return null; }
        }
        public bool TrySave(ControlProfile profile, bool pauseAlternative, bool relicsAlternative, out string error)
        {
            error = null;
            if (profile == null || !profile.CanActivate(pauseAlternative, relicsAlternative)) { error = "Mantenha um atalho ou acesso pelo menu para Pausa e Relíquias."; return false; }
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (var stream = new FileStream(path + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var bytes = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(profile, true));
                    stream.Write(bytes, 0, bytes.Length); stream.Flush(true);
                }
                if (File.Exists(path)) File.Replace(path + ".tmp", path, Read(path) != null ? path + ".bak" : null);
                else File.Move(path + ".tmp", path);
                current = profile.Copy(); Recovered = false;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { error = "Não foi possível salvar o perfil de controle."; return false; }
            Changed?.Invoke(); return true;
        }
        // An input consumer without the generated menu must retain default shortcuts.
        public ControlProfile ForConsumer(bool pauseAlternative = false, bool relicsAlternative = false)
        { return current.CanActivate(pauseAlternative, relicsAlternative) ? current.Copy() : new ControlProfile().WithFamily(current.Family); }
    }
}
