using System;
using System.Collections.Generic;
using QuackQuack.Core.Run;

namespace QuackQuack.Systems.Save
{
    public enum SaveStatus { Success, Recovered, Defaulted, Failed }

    // Detached values only: no asset references or mutable loadout state in persistence results.
    public sealed class SaveData
    {
        public int SchemaVersion { get; }
        public IReadOnlyList<string> UnlockedRelicIds { get; }
        public IReadOnlyList<string> EquippedRelicIds { get; }
        public RunProgressProfile Progress { get; }
        public SaveData(int schemaVersion, IEnumerable<string> unlockedRelicIds, IEnumerable<string> equippedRelicIds, RunProgressProfile progress = null)
        {
            SchemaVersion = schemaVersion;
            UnlockedRelicIds = new List<string>(unlockedRelicIds ?? Array.Empty<string>()).AsReadOnly();
            EquippedRelicIds = new List<string>(equippedRelicIds ?? Array.Empty<string>()).AsReadOnly();
            Progress = progress ?? new RunProgressProfile();
        }
    }

    public sealed class SaveOperationResult
    {
        public SaveData Data { get; }
        public SaveStatus Status { get; }
        public string Diagnostic { get; }
        internal SaveOperationResult(SaveData data, SaveStatus status, string diagnostic)
        { Data = data; Status = status; Diagnostic = diagnostic; }
    }
    public sealed class RunProgressCommitResult
    {
        public RunProgressReceipt Receipt { get; }
        public SaveOperationResult Save { get; }
        internal RunProgressCommitResult(RunProgressReceipt receipt, SaveOperationResult save) { Receipt = receipt; Save = save; }
    }
}
