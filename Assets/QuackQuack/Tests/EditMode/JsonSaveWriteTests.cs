using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using QuackQuack.Systems.Save;

namespace QuackQuack.Tests.EditMode
{
    public class JsonSaveWriteTests
    {
        private string directory, path;
        private JsonSaveService service;
        private string[] known;
        private static int Version => JsonSaveService.CurrentSchemaVersion;
        [SetUp] public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "QuackQuack-T059-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory); path = Path.Combine(directory, "save.json");
            known = new[] { "r0", "r1", "r2", "r3", "r4", "r5", "r6", "r7", "r8", "r9" };
            service = new JsonSaveService(path, known);
        }
        [TearDown] public void Cleanup()
        {
            string resolved = Path.GetFullPath(directory);
            string temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            Assert.That(resolved.StartsWith(temp, StringComparison.OrdinalIgnoreCase), Is.True);
            Assert.That(Path.GetFileName(resolved), Does.StartWith("QuackQuack-T059-"));
            if (Directory.Exists(resolved)) Directory.Delete(resolved, true);
        }
        private SaveData Data(string id) => new SaveData(Version, new[] { id }, new[] { id });
        private void AssertMain(string id)
        {
            var loaded = new JsonSaveService(path, known).Load(); Assert.That(loaded.Status, Is.EqualTo(SaveStatus.Success));
            CollectionAssert.AreEqual(new[] { id }, loaded.Data.UnlockedRelicIds);
            CollectionAssert.AreEqual(new[] { id }, loaded.Data.EquippedRelicIds);
        }

        [Test] public void FirstSaveCreatesDirectoriesAndRoundTripsInNewService()
        {
            path = Path.Combine(directory, "nested", "save.json"); service = new JsonSaveService(path, known);
            Assert.That(service.Save(Data("r0")).Status, Is.EqualTo(SaveStatus.Success)); AssertMain("r0");
            Assert.That(Directory.GetFiles(directory, "*.tmp", SearchOption.AllDirectories), Is.Empty);
        }
        [Test] public void ReplacementKeepsPreviousValidGenerationAsBackup()
        {
            service.Save(Data("r0")); string first = File.ReadAllText(path);
            Assert.That(service.Save(Data("r1")).Status, Is.EqualTo(SaveStatus.Success));
            Assert.That(File.ReadAllText(path + ".bak"), Is.EqualTo(first)); AssertMain("r1");
            service.Save(Data("r2"));
            CollectionAssert.AreEqual(new[] { "r1" }, new JsonSaveService(path + ".bak", known).Load().Data.EquippedRelicIds);
        }
        [Test] public void SavingOverCorruptMainPreservesLastGoodBackup()
        {
            service.Save(Data("r0")); service.Save(Data("r1")); string backup = File.ReadAllText(path + ".bak");
            File.WriteAllText(path, "{broken"); service.Save(Data("r2"));
            AssertMain("r2"); Assert.That(File.ReadAllText(path + ".bak"), Is.EqualTo(backup));
        }
        [Test] public void RestoreBackupRepairsMainWithoutReplacingBackupWithCorruption()
        {
            service.Save(Data("r0")); service.Save(Data("r1")); string backup = File.ReadAllText(path + ".bak");
            File.WriteAllText(path, "{broken"); var restored = service.RestoreBackup();
            Assert.That(restored.Status, Is.EqualTo(SaveStatus.Recovered)); AssertMain("r0");
            Assert.That(File.ReadAllText(path + ".bak"), Is.EqualTo(backup));
        }
        [Test] public void LoadRecoveryDoesNotWriteOrRotateFiles()
        {
            service.Save(Data("r0")); service.Save(Data("r1")); string backup = File.ReadAllText(path + ".bak");
            File.WriteAllText(path, "{broken"); Assert.That(service.Load().Status, Is.EqualTo(SaveStatus.Recovered));
            Assert.That(File.ReadAllText(path), Is.EqualTo("{broken")); Assert.That(File.ReadAllText(path + ".bak"), Is.EqualTo(backup));
        }
        [Test] public void FailedReplacementPreservesMainAndBackupAndCleansOwnedTemporary()
        {
            service.Save(Data("r0")); service.Save(Data("r1")); string before = File.ReadAllText(path), backup = File.ReadAllText(path + ".bak");
            using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var result = service.Save(Data("r2")); Assert.That(result.Status, Is.EqualTo(SaveStatus.Failed));
                Assert.That(result.Diagnostic, Does.Not.Contain(directory));
                Assert.That(File.ReadAllText(path), Is.EqualTo(before)); Assert.That(File.ReadAllText(path + ".bak"), Is.EqualTo(backup));
            }
            Assert.That(Directory.GetFiles(directory, "*.tmp"), Is.Empty); AssertMain("r1");
        }
        [Test] public void InvalidDestinationReturnsFailureWithoutThrowing()
        {
            File.WriteAllText(path, "parent is a file"); var blocked = new JsonSaveService(Path.Combine(path, "save.json"), known);
            Assert.That(blocked.Save(Data("r0")).Status, Is.EqualTo(SaveStatus.Failed));
            Assert.That(File.ReadAllText(path), Is.EqualTo("parent is a file"));
        }
        [Test] public void InterruptedTemporaryIsIgnoredAndNeverPromotedOrDeletedByLoad()
        {
            service.Save(Data("r0")); File.Move(path, path + ".interrupted.tmp");
            Assert.That(service.Load().Status, Is.EqualTo(SaveStatus.Defaulted));
            Assert.That(File.Exists(path), Is.False); Assert.That(File.Exists(path + ".interrupted.tmp"), Is.True);
            service.Save(Data("r1")); AssertMain("r1"); Assert.That(File.Exists(path + ".interrupted.tmp"), Is.True);
        }
        [Test] public void VersionZeroMigratesWithoutChangingEquipmentOrder()
        {
            File.WriteAllText(path, "{\"schemaVersion\":0,\"unlockedRelicIds\":[\"r1\",\"r0\"],\"equippedRelicIds\":[\"r1\",\"r0\"]}");
            var loaded = service.Load(); Assert.That(loaded.Status, Is.EqualTo(SaveStatus.Recovered));
            Assert.That(loaded.Data.SchemaVersion, Is.EqualTo(Version));
            CollectionAssert.AreEqual(new[] { "r1", "r0" }, loaded.Data.EquippedRelicIds);
            service.Save(loaded.Data); Assert.That(service.Load().Status, Is.EqualTo(SaveStatus.Success));
        }
        [Test] public void VersionZeroWithoutEquipmentMigratesToEmptyEquipment()
        {
            File.WriteAllText(path, "{\"schemaVersion\":0,\"unlockedRelicIds\":[\"r0\"]}");
            var loaded = service.Load(); Assert.That(loaded.Status, Is.EqualTo(SaveStatus.Recovered));
            CollectionAssert.AreEqual(new[] { "r0" }, loaded.Data.UnlockedRelicIds); Assert.That(loaded.Data.EquippedRelicIds, Is.Empty);
        }
        [Test] public void FutureMainCanLoadBackupButCannotBeOverwritten()
        {
            service.Save(Data("r0")); service.Save(Data("r1"));
            string future = "{\"schemaVersion\":" + (Version + 1) + ",\"newProgress\":123}";
            File.WriteAllText(path, future); string backup = File.ReadAllText(path + ".bak");
            Assert.That(service.Load().Status, Is.EqualTo(SaveStatus.Recovered));
            Assert.That(service.Save(Data("r2")).Status, Is.EqualTo(SaveStatus.Failed));
            Assert.That(service.RestoreBackup().Status, Is.EqualTo(SaveStatus.Failed));
            Assert.That(File.ReadAllText(path), Is.EqualTo(future)); Assert.That(File.ReadAllText(path + ".bak"), Is.EqualTo(backup));
        }
        [Test] public void FutureBackupIsPreservedDuringNormalSave()
        {
            service.Save(Data("r0")); string future = "{\"schemaVersion\":" + (Version + 1) + "}";
            File.WriteAllText(path + ".bak", future); Assert.That(service.Save(Data("r1")).Status, Is.EqualTo(SaveStatus.Success));
            AssertMain("r1"); Assert.That(File.ReadAllText(path + ".bak"), Is.EqualTo(future));
        }
        [Test] public void MigrationAndSaveNormalizeIdsAndRespectThreeSlots()
        {
            var raw = new SaveData(0, new[] { "r0", "r0", "unknown", "r1", "r2", "r3" }, new[] { "r9", "r3", "r3", "r2", "r1", "r0" });
            var migrated = service.Migrate(raw); Assert.That(migrated.Status, Is.EqualTo(SaveStatus.Recovered));
            CollectionAssert.AreEqual(new[] { "r3", "r2", "r1" }, migrated.Data.EquippedRelicIds);
            Assert.That(service.Save(raw).Status, Is.EqualTo(SaveStatus.Recovered));
            CollectionAssert.AreEqual(migrated.Data.EquippedRelicIds, service.Load().Data.EquippedRelicIds);
            Assert.That(raw.SchemaVersion, Is.Zero); Assert.That(raw.UnlockedRelicIds.Count, Is.EqualTo(6));
        }
        [Test] public void SnapshotAndKnownIdsAreCopiedAndReadOnly()
        {
            var unlocked = new[] { "r0" }; var data = new SaveData(Version, unlocked, unlocked);
            unlocked[0] = "r1"; known[0] = "mutated";
            Assert.That(service.Save(data).Status, Is.EqualTo(SaveStatus.Success));
            CollectionAssert.AreEqual(new[] { "r0" }, service.Load().Data.UnlockedRelicIds);
            Assert.Throws<NotSupportedException>(() => ((IList<string>)data.EquippedRelicIds).Clear());
        }
        [Test] public void EscapedUnicodeIdsRoundTripThroughNativeJsonWriterAndStrictReader()
        {
            string id = "r\"\\\nç"; known[0] = id; service = new JsonSaveService(path, known);
            Assert.That(service.Save(Data(id)).Status, Is.EqualTo(SaveStatus.Success)); AssertMain(id);
        }
        [TestCase("[]")] [TestCase("{\"schemaVersion\":\"1\"}")]
        [TestCase("{\"schemaVersion\":1.5}")] [TestCase("{\"schemaVersion\":1,\"schemaVersion\":0}")]
        [TestCase("{\"schemaVersion\":1,\"unlockedRelicIds\":{}}")]
        [TestCase("{\"schemaVersion\":1,\"equippedRelicIds\":[7]}")]
        [TestCase("{\"schemaVersion\":1} trailing")]
        [TestCase("{\"schemaVersion\":1,\"unlockedRelicIds\":[\"r0\"]")]
        [TestCase("{\"schemaVersion\":1,}")]
        [TestCase("{\"schemaVersion\":01}")]
        public void InvalidStructuralJsonNeverBecomesUsableProgress(string json)
        { File.WriteAllText(path, json); Assert.That(service.Load().Status, Is.EqualTo(SaveStatus.Defaulted)); }
        [Test] public void OversizedInputReturnsDefaultsWithoutParsing()
        {
            using (var file = File.Create(path)) file.SetLength(1024 * 1024 + 1);
            Assert.That(service.Load().Status, Is.EqualTo(SaveStatus.Defaulted));
            Assert.That(service.Save(Data("r0")).Status, Is.EqualTo(SaveStatus.Failed));
            Assert.That(new FileInfo(path).Length, Is.EqualTo(1024 * 1024 + 1));
        }
        [Test] public void InvalidDataAndUnavailableBackupDoNotWrite()
        {
            Assert.That(service.Save((SaveData)null).Status, Is.EqualTo(SaveStatus.Failed));
            Assert.That(service.Save(new SaveData(-1, null, null)).Status, Is.EqualTo(SaveStatus.Failed));
            Assert.That(service.Save(new SaveData(Version + 1, null, null)).Status, Is.EqualTo(SaveStatus.Failed));
            Assert.That(service.RestoreBackup().Status, Is.EqualTo(SaveStatus.Failed)); Assert.That(File.Exists(path), Is.False);
        }
        [Test] public void InvalidConfigurationIsRejected()
        {
            Assert.Throws<ArgumentException>(() => new JsonSaveService(" ", known));
            Assert.Throws<ArgumentNullException>(() => new JsonSaveService(path, null));
            Assert.Throws<ArgumentException>(() => new JsonSaveService(path, new[] { "r0" }));
            known[1] = known[0]; Assert.Throws<ArgumentException>(() => new JsonSaveService(path, known));
        }
    }
}
