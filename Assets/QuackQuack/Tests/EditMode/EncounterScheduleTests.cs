using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using QuackQuack.Core.Run;

namespace QuackQuack.Tests.EditMode
{
    public class EncounterScheduleTests
    {
        private static readonly string[] LakeIds = { "libelula-alfa", "sapo-cururu", "sapo-rei" };
        private RunState run;
        private object schedule;
        private Type scheduleType;

        [SetUp]
        public void CreateSchedule()
        {
            // Reflection is temporary: keep the test assembly runnable during the red phase.
            scheduleType = typeof(RunState).Assembly.GetType("QuackQuack.Core.Run.EncounterSchedule");
            Assert.That(scheduleType, Is.Not.Null,
                "T051/T060 pending: production EncounterSchedule is missing (T046 red phase).");
            run = new RunState(46);
            schedule = Create(run);
        }

        [Test]
        public void PreparingDoesNotRequestEncounters()
        {
            run.Advance(900);
            Assert.That(Collect(), Is.Empty);
            Assert.That(run.ElapsedSeconds, Is.Zero);
        }

        [Test]
        public void RequestsBothMinibossesAtFiveAndTenMinutesBeforeFinalBoss()
        {
            Start();
            run.Advance(299.5);
            Assert.That(Collect(), Is.Empty);
            run.Advance(0.5);
            CollectionAssert.AreEqual(new[] { LakeIds[0] }, Collect());
            Assert.That(Collect(), Is.Empty);
            run.Advance(299.5);
            Assert.That(Collect(), Is.Empty);
            run.Advance(0.5);
            CollectionAssert.AreEqual(new[] { LakeIds[1] }, Collect());
            Assert.That(run.ElapsedSeconds, Is.LessThan(900));
            run.Advance(299.5);
            Assert.That(Collect(), Is.Empty);
            run.Advance(0.5);
            CollectionAssert.AreEqual(new[] { LakeIds[2] }, Collect());
        }

        [Test]
        public void LargeAdvanceRequestsEveryCrossedEncounterInChronologicalOrder()
        {
            Start();
            run.Advance(1000);
            CollectionAssert.AreEqual(LakeIds, Collect());
            Assert.That(Collect(), Is.Empty);
        }

        [Test]
        public void FrogKingIsRequestedOnceAcrossRepeatedPollingAndBossPhase()
        {
            Start();
            run.Advance(900);
            CollectionAssert.AreEqual(LakeIds, Collect());
            Assert.That(run.TryTransition(RunPhase.Boss), Is.True);
            for (int i = 0; i < 10; i++)
            {
                run.Advance(60);
                Assert.That(Collect(), Is.Empty);
            }
        }

        [TestCase(RunPhase.Paused)]
        [TestCase(RunPhase.ChoosingUpgrade)]
        public void SuspendedTimeDoesNotAdvanceScheduleAndDueRequestWaitsForResume(RunPhase phase)
        {
            Start();
            run.Advance(300);
            Assert.That(run.TryTransition(phase), Is.True);
            run.Advance(1000);
            Assert.That(run.ElapsedSeconds, Is.EqualTo(300));
            Assert.That(Collect(), Is.Empty);
            Assert.That(run.TryTransition(RunPhase.Playing), Is.True);
            CollectionAssert.AreEqual(new[] { LakeIds[0] }, Collect());
            Assert.That(Collect(), Is.Empty);
        }

        [TestCase(RunPhase.Lost)]
        [TestCase(RunPhase.Abandoned)]
        [TestCase(RunPhase.Won)]
        [TestCase(RunPhase.Results)]
        public void EndedRunNeverRequestsPendingEncounters(RunPhase phase)
        {
            Start();
            run.Advance(900);
            if (phase == RunPhase.Won) run.TryTransition(RunPhase.Boss);
            Assert.That(run.TryTransition(phase == RunPhase.Results ? RunPhase.Lost : phase), Is.True);
            if (phase == RunPhase.Results) Assert.That(run.TryTransition(RunPhase.Results), Is.True);
            Assert.That(Collect(), Is.Empty);
            run.Advance(900);
            Assert.That(Collect(), Is.Empty);
        }

        [Test]
        public void NewRunHasIndependentRequests()
        {
            Start();
            run.Advance(900);
            CollectionAssert.AreEqual(LakeIds, Collect());
            var nextRun = new RunState(46);
            var nextSchedule = Create(nextRun);
            nextRun.TryTransition(RunPhase.Playing);
            nextRun.Advance(900);
            CollectionAssert.AreEqual(LakeIds, Collect(nextSchedule));
            Assert.That(Collect(), Is.Empty);
        }

        [Test]
        public void InjectedScheduleIsCopiedAndUsesAuthoredTimes()
        {
            var ids = new[] { "first", "second", "final" };
            var times = new[] { 2d, 4d, 6d };
            schedule = Create(run, ids, times);
            ids[0] = "mutated";
            times[0] = 999;
            Start();
            run.Advance(2);
            CollectionAssert.AreEqual(new[] { "first" }, Collect());
            run.Advance(4);
            CollectionAssert.AreEqual(new[] { "second", "final" }, Collect());
        }

        private void Start() => Assert.That(run.TryTransition(RunPhase.Playing), Is.True);

        private object Create(RunState state, string[] ids = null, double[] times = null)
        {
            var constructor = scheduleType.GetConstructor(new[] { typeof(RunState), typeof(string[]), typeof(double[]) });
            Assert.That(constructor, Is.Not.Null, "Expected EncounterSchedule(RunState, string[], double[]).");
            return constructor.Invoke(new object[] { state, ids ?? (string[])LakeIds.Clone(), times ?? new[] { 300d, 600d, 900d } });
        }

        private IReadOnlyList<string> Collect(object source = null)
        {
            MethodInfo method = scheduleType.GetMethod("CollectDue", Type.EmptyTypes);
            Assert.That(method, Is.Not.Null, "Expected CollectDue() returning IReadOnlyList<string>.");
            object result = method.Invoke(source ?? schedule, null);
            Assert.That(result, Is.InstanceOf<IReadOnlyList<string>>());
            return (IReadOnlyList<string>)result;
        }
    }
}
