using System;
using System.IO;
using NUnit.Framework;
using QuackQuack.Platform.Input;
using UnityEngine;

namespace QuackQuack.Tests.EditMode
{
    public sealed class ControlProfileTests
    {
        private string directory;
        [SetUp] public void SetUp() { directory = Path.Combine(Path.GetTempPath(), "QuackControlProfileTests", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory); }
        [TearDown] public void TearDown() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        private ControlProfile Assign(ProfileAction action, string path, bool alternative = false)
        { Assert.That(new ControlProfile().TryAssign(action, path, alternative, out var result), Is.True); return result; }
        [Test] public void DefaultsPreserveCommonActions()
        {
            var p = new ControlProfile(); Assert.That(p.IsValid(), Is.True);
            Assert.That(p.Version, Is.EqualTo(1)); Assert.That(p.Family, Is.EqualTo(ControllerFamily.Automatic));
            Assert.That(p.Binding(ProfileAction.Quack), Is.EqualTo("<Gamepad>/buttonSouth"));
            Assert.That(p.Binding(ProfileAction.Pause), Is.EqualTo("<Gamepad>/start"));
            Assert.That(p.Binding(ProfileAction.Relics), Is.EqualTo("<Gamepad>/select"));
        }
        [TestCase(ControllerFamily.Automatic)] [TestCase(ControllerFamily.Xbox)]
        [TestCase(ControllerFamily.PlayStation)] [TestCase(ControllerFamily.Generic)]
        public void RoundTripKeepsFamilyAndBindingWithoutSelect(ControllerFamily family)
        {
            File.WriteAllText(Path.Combine(directory, "progress.json"), "untouched progress");
            var expected = Assign(ProfileAction.Relics, "<Gamepad>/rightShoulder").WithFamily(family);
            var store = new ControlProfileStore(directory);
            Assert.That(store.TrySave(expected, false, false, out _), Is.True);
            var loaded = new ControlProfileStore(directory).Current;
            Assert.That(loaded.Family, Is.EqualTo(family)); Assert.That(loaded.Binding(ProfileAction.Relics), Is.EqualTo("<Gamepad>/rightShoulder"));
            Assert.That(File.ReadAllText(Path.Combine(directory, "progress.json")), Is.EqualTo("untouched progress"));
            Assert.That(Directory.GetFiles(directory, "*.tmp"), Is.Empty);
        }
        [TestCase(ProfileAction.Pause)] [TestCase(ProfileAction.Relics)]
        public void CannotRemoveLastAccessWithoutFocusableAlternative(ProfileAction action)
        {
            Assert.That(new ControlProfile().TryAssign(action, "", false, out _), Is.False);
            var p = Assign(action, "", true); var store = new ControlProfileStore(directory);
            Assert.That(store.TrySave(p, false, false, out _), Is.False);
            Assert.That(File.Exists(Path.Combine(directory, ControlProfileStore.FileName)), Is.False);
            Assert.That(store.TrySave(p, action == ProfileAction.Pause, action == ProfileAction.Relics, out _), Is.True);
            Assert.That(store.ForConsumer().Binding(action), Is.Not.Empty, "consumer without menu must retain access");
            Assert.That(store.ForConsumer(action == ProfileAction.Pause, action == ProfileAction.Relics).Binding(action), Is.Empty);
        }
        [TestCase("<Gamepad>/buttonEast")] [TestCase("<Gamepad>/start")]
        [TestCase("<Gamepad>/buttonSouth")] [TestCase("<Keyboard>/r")]
        [TestCase("/Gamepad123/select")] [TestCase(null)]
        public void RejectsConflictsCancelAndDeviceSpecificPaths(string path)
        { Assert.That(new ControlProfile().TryAssign(ProfileAction.Relics, path, false, out _), Is.False); }
        [TestCase("not json")] [TestCase("{}")] [TestCase("null")]
        [TestCase("{\"version\":99,\"family\":0,\"quack\":\"<Gamepad>/buttonSouth\",\"pause\":\"<Gamepad>/start\",\"relics\":\"<Gamepad>/select\"}")]
        [TestCase("{\"version\":1,\"family\":99,\"quack\":\"<Gamepad>/buttonSouth\",\"pause\":\"<Gamepad>/start\",\"relics\":\"<Gamepad>/select\"}")]
        public void CorruptionOrUnsupportedSchemaFallsBackWithoutOverwriting(string json)
        {
            string path = Path.Combine(directory, ControlProfileStore.FileName); File.WriteAllText(path, json);
            var store = new ControlProfileStore(directory);
            Assert.That(store.Recovered, Is.True); Assert.That(store.Current.IsValid(), Is.True);
            Assert.That(store.Current.Binding(ProfileAction.Relics), Is.EqualTo("<Gamepad>/select"));
            Assert.That(File.ReadAllText(path), Is.EqualTo(json));
        }
        [Test] public void ValidBackupRecoversCorruptMain()
        {
            var store = new ControlProfileStore(directory);
            Assert.That(store.TrySave(Assign(ProfileAction.Relics, "<Gamepad>/rightShoulder"), false, false, out _), Is.True);
            Assert.That(store.TrySave(new ControlProfile(), false, false, out _), Is.True);
            File.WriteAllText(Path.Combine(directory, ControlProfileStore.FileName), "broken");
            var recovered = new ControlProfileStore(directory);
            Assert.That(recovered.Recovered, Is.True); Assert.That(recovered.Current.Binding(ProfileAction.Relics), Is.EqualTo("<Gamepad>/rightShoulder"));
        }
        [Test] public void FailedWriteDoesNotPublishOrChangeCurrent()
        {
            string file = Path.Combine(directory, "not-a-directory"); File.WriteAllText(file, "file");
            var store = new ControlProfileStore(file); int events = 0; store.Changed += () => events++;
            Assert.That(store.TrySave(Assign(ProfileAction.Relics, "<Gamepad>/rightShoulder"), false, false, out var error), Is.False);
            Assert.That(error, Is.Not.Empty); Assert.That(events, Is.Zero);
            Assert.That(store.Current.Binding(ProfileAction.Relics), Is.EqualTo("<Gamepad>/select"));
        }
        [Test] public void SnapshotAndEditingDoNotMutateCurrentProfile()
        {
            var store = new ControlProfileStore(directory); var original = store.Current;
            Assert.That(original.TryAssign(ProfileAction.Relics, "<Gamepad>/rightShoulder", false, out var next), Is.True);
            Assert.That(original.Binding(ProfileAction.Relics), Is.EqualTo("<Gamepad>/select"));
            Assert.That(store.Current.Binding(ProfileAction.Relics), Is.EqualTo("<Gamepad>/select"));
            Assert.That(next.Binding(ProfileAction.Relics), Is.EqualTo("<Gamepad>/rightShoulder"));
        }
        [Test] public void OversizedFileFallsBack() { File.WriteAllText(Path.Combine(directory, ControlProfileStore.FileName), new string('x', 9000)); Assert.That(new ControlProfileStore(directory).Recovered, Is.True); }
        [Test] public void MovingQuackCannotFreeUiSubmitForPauseOrRelics()
        {
            var profile = Assign(ProfileAction.Quack, "<Gamepad>/buttonNorth");
            Assert.That(profile.TryAssign(ProfileAction.Pause, "<Gamepad>/buttonSouth", false, out _), Is.False);
            Assert.That(profile.TryAssign(ProfileAction.Relics, "<Gamepad>/buttonSouth", false, out _), Is.False);
        }
        [Test] public void InvalidEnumsAreRejected()
        {
            Assert.That(new ControlProfile().TryAssign((ProfileAction)99, "<Gamepad>/rightShoulder", false, out _), Is.False);
            Assert.Throws<ArgumentException>(() => new ControlProfile().WithFamily((ControllerFamily)99));
        }
    }
}
