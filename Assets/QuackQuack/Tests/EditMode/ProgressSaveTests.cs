using System;
using System.IO;
using NUnit.Framework;
using QuackQuack.Core.Progression;
using QuackQuack.Core.Run;
using QuackQuack.Systems.Save;
namespace QuackQuack.Tests.EditMode
{
    public class ProgressSaveTests
    {
        private string directory, path;
        private string[] ids;
        private RelicLoadout loadout;
        private JsonSaveService save;
        private ResultChallengeRule[] rules;
        [SetUp] public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "QuackQuack-T061-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory); path = Path.Combine(directory, "save.json");
            ids = new string[10]; var bonuses = new float[10]; for (int i = 0; i < 10; i++) { ids[i] = "r" + i; bonuses[i] = 0.05f; }
            loadout = new RelicLoadout(ids, bonuses); loadout.Unlock("r0"); loadout.Equip("r0"); save = new JsonSaveService(path, ids);
            rules = new[] { new ResultChallengeRule("win", ResultMetric.Victories, 1, true) };
        }
        [TearDown] public void Cleanup()
        {
            string resolved = Path.GetFullPath(directory), temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            Assert.That(resolved.StartsWith(temp, StringComparison.OrdinalIgnoreCase), Is.True); Assert.That(Path.GetFileName(resolved), Does.StartWith("QuackQuack-T061-"));
            if (Directory.Exists(resolved)) Directory.Delete(resolved, true);
        }
        [Test] public void CommitReopenedInAnotherServiceIsIdempotentAndRelicSavePreservesProgress()
        {
            var result = RunProgressionTests.Make(RunPhase.Won); Assert.That(save.CommitResult("a", result, rules, loadout).Save.Status, Is.EqualTo(SaveStatus.Success));
            var reopened = new JsonSaveService(path, ids); string before = File.ReadAllText(path);
            Assert.That(reopened.CommitResult("a", result, rules, loadout).Receipt.AlreadyApplied, Is.True); Assert.That(File.ReadAllText(path), Is.EqualTo(before));
            loadout.Unlock("r1"); loadout.Equip("r1"); reopened.Save(loadout); var loaded = reopened.Load().Data;
            Assert.That(loaded.Progress.Coins, Is.EqualTo(151)); Assert.That(loaded.Progress.Victories, Is.EqualTo(1));
            Assert.That(loaded.Progress.Challenges["win"], Is.EqualTo(1)); Assert.That(loaded.EquippedRelicIds.Count, Is.EqualTo(2));
        }
        [Test] public void FailedCommitThenRetryCreditsExactlyOnce()
        {
            save.Save(loadout); var result = RunProgressionTests.Make(RunPhase.Abandoned);
            using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            { Assert.That(save.CommitResult("a", result, rules, loadout).Save.Status, Is.EqualTo(SaveStatus.Failed)); Assert.That(save.Load().Data.Progress.Coins, Is.Zero); }
            Assert.That(save.CommitResult("a", result, rules, loadout).Save.Status, Is.EqualTo(SaveStatus.Success));
            var again = new JsonSaveService(path, ids).CommitResult("a", result, rules, loadout);
            Assert.That(again.Receipt.AlreadyApplied, Is.True); Assert.That(again.Receipt.Profile.Coins, Is.EqualTo(50)); Assert.That(again.Receipt.Profile.Victories, Is.Zero);
        }
        [Test] public void SchemaOneMigratesRelicsWithoutInventingCurrencyOrReceipts()
        {
            File.WriteAllText(path, "{\"schemaVersion\":1,\"unlockedRelicIds\":[\"r0\"],\"equippedRelicIds\":[\"r0\"]}");
            var loaded = save.Load(); Assert.That(loaded.Status, Is.EqualTo(SaveStatus.Recovered)); Assert.That(loaded.Data.SchemaVersion, Is.EqualTo(2));
            Assert.That(loaded.Data.Progress.Coins, Is.Zero); Assert.That(loaded.Data.EquippedRelicIds[0], Is.EqualTo("r0"));
        }
        [Test] public void InvalidProgressRecoversBackupInsteadOfResettingOnlyWallet()
        {
            save.CommitResult("a", RunProgressionTests.Make(RunPhase.Won), rules, loadout); File.Copy(path, path + ".bak");
            File.WriteAllText(path, "{\"schemaVersion\":2,\"progress\":{\"coins\":-1}}");
            var loaded = save.Load(); Assert.That(loaded.Status, Is.EqualTo(SaveStatus.Recovered)); Assert.That(loaded.Data.Progress.Coins, Is.EqualTo(151));
            Assert.That(loaded.Data.Progress.AppliedRunIds[0], Is.EqualTo("a"));
        }
    }
}
