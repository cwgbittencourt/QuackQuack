using System;
using NUnit.Framework;
using QuackQuack.Content.Encounters;
using QuackQuack.Core.Progression;
using QuackQuack.Editor;
using UnityEditor;
using UnityEngine;

namespace QuackQuack.Tests.EditMode
{
    public class RewardCapacityTests
    {
        [Test]
        public void EntireAuthoredRunFitsWithEveryCommonHealingRollSuccessfulAndNoViews()
        {
            var settings = AssetDatabase.LoadAssetAtPath<LakeRunSettings>(LakeRunSetup.SettingsPath);
            long waveDefeats = 0;
            foreach (var wave in settings.Catalog.Waves)
            {
                int quantity = 0; foreach (int count in wave.Quantities) quantity += count;
                waveDefeats += ((long)Math.Ceiling((wave.EndSeconds - wave.StartSeconds) / wave.SpawnInterval) + 1) * quantity;
            }
            int capacity = settings.CalculateRewardDefeatCapacity();
            Assert.That(capacity, Is.EqualTo(waveDefeats + 2L * settings.Catalog.QueueCapacity));
            int rolls = 0;
            var ledger = settings.PickupPolicy.CreateLedger(1, () => { rolls++; return 0; }, capacity);
            ledger.ProcessDefeat(7, 1, RewardOrigin.Common, 1, -3, 2, true); // warm generic paths before allocation measurement
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 2; i <= capacity; i++)
                AssertAccepted(ledger.ProcessDefeat(7, i, RewardOrigin.Common, 1, -3, 2, true));
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(ledger.DefeatCount, Is.EqualTo(capacity));
            Assert.That(ledger.Entries.Count, Is.EqualTo(capacity * 2));
            Assert.That(ledger.PendingCount, Is.EqualTo(ledger.Entries.Count));
            Assert.That(rolls, Is.EqualTo(capacity)); Assert.That(allocated, Is.Zero);
            Debug.Log($"T027_REWARD_CAPACITY_OK waves={waveDefeats} reserve={2 * settings.Catalog.QueueCapacity} defeats={capacity} obligations={ledger.Entries.Count} emissionBytes={allocated}");
        }
        private static void AssertAccepted(bool result)
        { if (!result) throw new InvalidOperationException("Unexpected suppressed authored reward."); }
    }
}
