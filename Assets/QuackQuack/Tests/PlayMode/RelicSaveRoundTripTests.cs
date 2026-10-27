#if UNITY_EDITOR
using System;
using System.IO;
using NUnit.Framework;
using QuackQuack.Content.Relics;
using QuackQuack.Core.Progression;
using QuackQuack.Core.Run;
using QuackQuack.Systems.Save;
using UnityEditor;

namespace QuackQuack.Tests.PlayMode
{
    public class RelicSaveRoundTripTests
    {
        [Test] public void ActualCatalogLoadoutSurvivesSaveReloadAndIdempotentReapplication()
        {
            string directory = Path.Combine(Path.GetTempPath(), "QuackQuack-T059Play-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var catalog = AssetDatabase.LoadAssetAtPath<PrototypeRelicCatalog>("Assets/QuackQuack/Content/Relics/PrototypeRelicCatalog.asset");
                var ids = new string[catalog.Relics.Count]; for (int i = 0; i < ids.Length; i++) ids[i] = catalog.Relics[i].Id;
                var original = catalog.CreateLoadout();
                for (int i = 0; i < 3; i++) { original.Unlock(ids[i]); original.Equip(ids[i]); }
                string path = Path.Combine(directory, "save.json");
                Assert.That(new JsonSaveService(path, ids).Save(original).Status, Is.EqualTo(SaveStatus.Success));
                var restored = new JsonSaveService(path, ids).Load(); Assert.That(restored.Status, Is.EqualTo(SaveStatus.Success));
                var other = catalog.CreateLoadout();
                for (int repeat = 0; repeat < 3; repeat++)
                {
                    foreach (string id in restored.Data.UnlockedRelicIds) other.Unlock(id);
                    foreach (string id in restored.Data.EquippedRelicIds) other.Equip(id);
                }
                CollectionAssert.AreEqual(original.EquippedIds, other.EquippedIds);
                foreach (RelicAttribute attribute in Enum.GetValues(typeof(RelicAttribute)))
                    foreach (TerrainType terrain in Enum.GetValues(typeof(TerrainType)))
                        Assert.That(other.ApplyEffects(100, attribute, terrain), Is.EqualTo(original.ApplyEffects(100, attribute, terrain)));
                foreach (string id in restored.Data.EquippedRelicIds) other.Unequip(id);
                foreach (RelicAttribute attribute in Enum.GetValues(typeof(RelicAttribute)))
                    Assert.That(other.ApplyEffects(100, attribute, TerrainType.Land), Is.EqualTo(100));
            }
            finally
            {
                string resolved = Path.GetFullPath(directory);
                string temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                Assert.That(resolved.StartsWith(temp, StringComparison.OrdinalIgnoreCase), Is.True);
                Assert.That(Path.GetFileName(resolved), Does.StartWith("QuackQuack-T059Play-"));
                if (Directory.Exists(resolved)) Directory.Delete(resolved, true);
            }
        }
    }
}
#endif
