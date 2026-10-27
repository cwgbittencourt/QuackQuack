#if UNITY_EDITOR
using NUnit.Framework;
using QuackQuack.Content.Enemies;
using UnityEditor;
using UnityEngine;
namespace QuackQuack.Tests.EditMode
{
    public class FrogKingSettingsTests
    {
        private FrogKingSettings settings;
        [SetUp] public void SetUp() => settings = Object.Instantiate(AssetDatabase.LoadAssetAtPath<FrogKingSettings>("Assets/QuackQuack/Content/Enemies/Prototype/FrogKingSettings.asset"));
        [TearDown] public void Cleanup() => Object.DestroyImmediate(settings);
        [Test] public void AuthoredSettingsAreValid() => Assert.That(settings.IsValid(out _), Is.True);
        [TestCase(0)] [TestCase(-1)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)]
        public void InvalidHealthIsRejected(float value) { settings.Health = value; Assert.That(settings.IsValid(out _), Is.False); }
        [Test] public void PhaseThresholdsMustBeOrdered() { settings.DestructionPhaseHealth = settings.SummonPhaseHealth; Assert.That(settings.IsValid(out _), Is.False); }
        [Test] public void SummonBatchCannotExceedPool() { settings.TadpoleCount = settings.HazardCapacity + 1; Assert.That(settings.IsValid(out _), Is.False); }
        [Test] public void WarningRequiresAudio() { settings.WarningAudio = null; Assert.That(settings.IsValid(out _), Is.False); }
        [Test] public void WarningRequiresMaterial() { settings.MarkerMaterial = null; Assert.That(settings.IsValid(out _), Is.False); }
        [Test] public void HazardsRequirePrefab() { settings.HazardPrefab = null; Assert.That(settings.IsValid(out _), Is.False); }
    }
}
#endif
