using System;
using System.Collections.Generic;

namespace QuackQuack.Core.Run
{
    public sealed class EncounterSchedule
    {
        private readonly RunState run;
        private readonly string[] ids;
        private readonly double[] times;
        private int cursor;

        public EncounterSchedule(RunState run, string[] encounterIds, double[] scheduledSeconds)
        {
            this.run = run ?? throw new ArgumentNullException(nameof(run));
            if (encounterIds == null || scheduledSeconds == null || encounterIds.Length == 0 || encounterIds.Length != scheduledSeconds.Length)
                throw new ArgumentException("Matching nonempty encounter IDs and times are required.");
            ids = (string[])encounterIds.Clone(); times = (double[])scheduledSeconds.Clone();
            var unique = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < ids.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(ids[i]) || !unique.Add(ids[i])) throw new ArgumentException("Encounter IDs must be nonempty and unique.");
                if (double.IsNaN(times[i]) || double.IsInfinity(times[i]) || times[i] < 0 || (i > 0 && times[i] < times[i - 1]))
                    throw new ArgumentException("Encounter times must be finite, nonnegative and ordered.");
            }
        }

        public IReadOnlyList<string> CollectDue()
        {
            if (run.Phase != RunPhase.Playing || cursor == ids.Length || times[cursor] > run.ElapsedSeconds)
                return Array.Empty<string>();
            int start = cursor;
            while (cursor < ids.Length && times[cursor] <= run.ElapsedSeconds) cursor++;
            var due = new string[cursor - start];
            Array.Copy(ids, start, due, 0, due.Length);
            return Array.AsReadOnly(due);
        }
    }
}
