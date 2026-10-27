using System;
using System.Collections.Generic;
using System.IO;
using QuackQuack.Systems.Save;
using NUnit.Framework;
using QuackQuack.Core.Run;
using QuackQuack.Core.Progression;
using QuackQuack.Systems.Run;

namespace QuackQuack.Tests.EditMode
{
    public class RelicLoadoutTests
    {
        private RelicLoadout loadout;
        private string[] ids;
        private float[] bonuses;

        [SetUp]
        public void CreateLoadout()
        {
            ids = new string[10];
            bonuses = new float[10];
            for (int i = 0; i < ids.Length; i++) { ids[i] = "relic-" + i; bonuses[i] = 0.1f; }
            loadout = new RelicLoadout(ids, bonuses);
        }

        [Test]
        public void NewLoadoutStartsLockedEmptyAndPreservesBaseEffect()
        {
            Assert.That(Ids("UnlockedIds"), Is.Empty);
            Assert.That(Ids("EquippedIds"), Is.Empty);
            Assert.That(Apply(), Is.EqualTo(100f));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("unknown")]
        public void UnknownOrInvalidIdsCannotUnlockOrEquip(string id)
        {
            Assert.That(Command("Unlock", id), Is.False);
            Assert.That(Command("Equip", id), Is.False);
            Assert.That(Ids("UnlockedIds"), Is.Empty);
            Assert.That(Ids("EquippedIds"), Is.Empty);
            Assert.That(Apply(), Is.EqualTo(100f));
        }

        [Test]
        public void KnownButLockedRelicCannotBeEquipped()
        {
            Assert.That(Command("Equip", ids[0]), Is.False);
            Assert.That(Ids("EquippedIds"), Is.Empty);
        }

        [Test]
        public void UnlockingTwiceDoesNotDuplicateOrApplyEffect()
        {
            Assert.That(Command("Unlock", ids[0]), Is.True);
            Assert.That(Command("Unlock", ids[0]), Is.False);
            CollectionAssert.AreEquivalent(new[] { ids[0] }, Ids("UnlockedIds"));
            Assert.That(Apply(), Is.EqualTo(100f));
        }

        [Test]
        public void ThreeSlotsRejectFourthWithoutChangingEffectsOrUnlocks()
        {
            for (int i = 0; i < 4; i++) Assert.That(Command("Unlock", ids[i]), Is.True);
            for (int i = 0; i < 3; i++)
            {
                Assert.That(Command("Equip", ids[i]), Is.True);
                Assert.That(Ids("EquippedIds").Count, Is.EqualTo(i + 1));
                Assert.That(Apply(), Is.EqualTo(100f * (1f + 0.1f * (i + 1))).Within(0.001f));
            }
            Assert.That(Command("Equip", ids[3]), Is.False);
            CollectionAssert.AreEquivalent(new[] { ids[0], ids[1], ids[2] }, Ids("EquippedIds"));
            Assert.That(Ids("UnlockedIds").Count, Is.EqualTo(4));
            Assert.That(Apply(), Is.EqualTo(130f).Within(0.001f));
        }

        [Test]
        public void ReapplyAndReequipNeverAccumulateAndRemovingRestoresBaseline()
        {
            Command("Unlock", ids[0]);
            Assert.That(Command("Equip", ids[0]), Is.True);
            for (int i = 0; i < 5; i++)
            {
                Assert.That(Command("Equip", ids[0]), Is.False);
                Assert.That(Apply(), Is.EqualTo(110f).Within(0.001f));
            }
            Assert.That(Command("Unequip", ids[0]), Is.True);
            Assert.That(Command("Unequip", ids[0]), Is.False);
            Assert.That(Ids("EquippedIds"), Is.Empty);
            Assert.That(Apply(), Is.EqualTo(100f));
            Assert.That(Command("Equip", ids[0]), Is.True);
            Assert.That(Apply(), Is.EqualTo(110f).Within(0.001f));
        }

        [Test]
        public void RemovingOneRelicKeepsOthersAndFreesSlot()
        {
            for (int i = 0; i < 4; i++) Command("Unlock", ids[i]);
            for (int i = 0; i < 3; i++) Command("Equip", ids[i]);
            Assert.That(Command("Unequip", ids[1]), Is.True);
            Assert.That(Apply(), Is.EqualTo(120f).Within(0.001f));
            Assert.That(Command("Equip", ids[3]), Is.True);
            CollectionAssert.AreEquivalent(new[] { ids[0], ids[2], ids[3] }, Ids("EquippedIds"));
        }

        [Test]
        public void DefinitionsAreCopiedAndLoadoutsAreIndependent()
        {
            Type type = loadout.GetType();
            var other = new RelicLoadout(ids, bonuses);
            Command("Unlock", "relic-0");
            Command("Equip", "relic-0");
            ids[0] = "mutated";
            bonuses[0] = 100f;
            Assert.That(Apply(), Is.EqualTo(110f).Within(0.001f));
            Assert.That(other.UnlockedIds, Is.Empty);
        }

        private bool Command(string name, string id) => name == "Unlock" ? loadout.Unlock(id) : name == "Equip" ? loadout.Equip(id) : loadout.Unequip(id);
        private float Apply() => loadout.ApplyEffects(100f);
        private IReadOnlyCollection<string> Ids(string name) => name == "UnlockedIds" ? loadout.UnlockedIds : loadout.EquippedIds;
    }

    public class SaveServiceTests
    {
        private JsonSaveService service;
        private string directory;
        private string path;
        private int version;
        private readonly string[] known = { "r0", "r1", "r2", "r3", "r4", "r5", "r6", "r7", "r8", "r9" };

        [SetUp]
        public void CreateService()
        {
            version = JsonSaveService.CurrentSchemaVersion;
            directory = Path.Combine(Path.GetTempPath(), "QuackQuack-T048-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            path = Path.Combine(directory, "save.json");
            service = new JsonSaveService(path, known);
        }

        [TearDown]
        public void RemoveOnlyOwnedFixtureDirectory()
        {
            if (directory == null) return;
            string resolved = Path.GetFullPath(directory);
            string tempRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            Assert.That(resolved.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase), Is.True);
            Assert.That(Path.GetFileName(resolved), Does.StartWith("QuackQuack-T048-"));
            if (Directory.Exists(resolved)) Directory.Delete(resolved, true);
        }

        [Test]
        public void MissingSaveReturnsUsableDefaults()
        {
            SaveOperationResult result = Load();
            AssertStatus(result, "Defaulted");
            AssertEmpty(Data(result));
        }

        [TestCase("")]
        [TestCase("{invalid")]
        [TestCase("null")]
        [TestCase("{}")]
        [TestCase("{\"schemaVersion\":-1}")]
        public void InvalidMainAndBackupReturnDefaultsWithoutThrowing(string invalid)
        {
            File.WriteAllText(path, invalid);
            File.WriteAllText(path + ".bak", "{broken");
            SaveOperationResult result = Load();
            AssertStatus(result, "Defaulted");
            AssertEmpty(Data(result));
            Assert.That(result.Diagnostic, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void InvalidMainRecoversValidBackupInsteadOfDiscardingProgress()
        {
            File.WriteAllText(path, "{broken");
            File.WriteAllText(path + ".bak", Json("[\"r0\",\"r1\"]", "[\"r1\"]"));
            SaveOperationResult result = Load();
            AssertStatus(result, "Recovered");
            CollectionAssert.AreEquivalent(new[] { "r0", "r1" }, Ids(Data(result), "UnlockedRelicIds"));
            CollectionAssert.AreEqual(new[] { "r1" }, Ids(Data(result), "EquippedRelicIds"));
        }

        [Test]
        public void ValidMainTakesPrecedenceOverBackup()
        {
            File.WriteAllText(path, Json("[\"r0\"]", "[]"));
            File.WriteAllText(path + ".bak", Json("[\"r1\"]", "[\"r1\"]"));
            SaveOperationResult result = Load();
            AssertStatus(result, "Success");
            CollectionAssert.AreEqual(new[] { "r0" }, Ids(Data(result), "UnlockedRelicIds"));
            Assert.That(Ids(Data(result), "EquippedRelicIds"), Is.Empty);
        }

        [Test]
        public void LoadedIdsAreKnownUniqueUnlockedAndLimitedToThreeEquipped()
        {
            File.WriteAllText(path, Json(
                "[\"r0\",\"r0\",\"unknown\",\"r1\",\"r2\",\"r3\"]",
                "[\"unknown\",\"r0\",\"r0\",\"r9\",\"r1\",\"r2\",\"r3\"]"));
            SaveOperationResult result = Load();
            AssertStatus(result, "Recovered");
            CollectionAssert.AreEquivalent(new[] { "r0", "r1", "r2", "r3" }, Ids(Data(result), "UnlockedRelicIds"));
            CollectionAssert.AreEqual(new[] { "r0", "r1", "r2" }, Ids(Data(result), "EquippedRelicIds"));
            Assert.That(result.Diagnostic, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void NullCollectionsRecoverToEmptyLoadout()
        {
            File.WriteAllText(path, Json("null", "null"));
            SaveOperationResult result = Load();
            Assert.That(result.Status.ToString(), Is.EqualTo("Recovered").Or.EqualTo("Defaulted"));
            AssertEmpty(Data(result));
        }

        [Test]
        public void RepeatedRecoveryDoesNotDuplicateUnlocksOrEquipment()
        {
            File.WriteAllText(path, "{broken");
            File.WriteAllText(path + ".bak", Json("[\"r0\"]", "[\"r0\"]"));
            for (int i = 0; i < 3; i++)
            {
                SaveData data = Data(Load());
                CollectionAssert.AreEqual(new[] { "r0" }, Ids(data, "UnlockedRelicIds"));
                CollectionAssert.AreEqual(new[] { "r0" }, Ids(data, "EquippedRelicIds"));
            }
        }

        private string Json(string unlocked, string equipped) =>
            "{\"schemaVersion\":" + version + ",\"unlockedRelicIds\":" + unlocked + ",\"equippedRelicIds\":" + equipped + "}";
        private SaveOperationResult Load() => service.Load();
        private static SaveData Data(SaveOperationResult result) => result.Data;
        private static IReadOnlyCollection<string> Ids(SaveData data, string name) => name == "UnlockedRelicIds" ? data.UnlockedRelicIds : data.EquippedRelicIds;
        private void AssertEmpty(SaveData data)
        {
            Assert.That(data.SchemaVersion, Is.EqualTo(version));
            Assert.That(Ids(data, "UnlockedRelicIds"), Is.Empty);
            Assert.That(Ids(data, "EquippedRelicIds"), Is.Empty);
        }
        private static void AssertStatus(SaveOperationResult result, string expected) =>
            Assert.That(result.Status.ToString(), Is.EqualTo(expected));
    }

}
