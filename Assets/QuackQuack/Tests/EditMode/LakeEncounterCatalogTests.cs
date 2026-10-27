using System;
using System.Collections.Generic;
using NUnit.Framework;
using QuackQuack.Content.Catalogs;
using QuackQuack.Content.Enemies;
using QuackQuack.Content.Encounters;
using QuackQuack.Core.Run;
using QuackQuack.Gameplay.Enemies;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace QuackQuack.Tests.EditMode
{
    public class LakeEncounterCatalogTests
    {
        private LakeEncounterCatalog catalog;
        private readonly List<Object> copies=new List<Object>();
        [SetUp]public void SetUp(){catalog=AssetDatabase.LoadAssetAtPath<LakeEncounterCatalog>("Assets/QuackQuack/Content/Catalogs/LakeEncounterCatalog.asset");Assert.That(catalog,Is.Not.Null);}
        [TearDown]public void Cleanup(){foreach(var value in copies)Object.DestroyImmediate(value);copies.Clear();}
        [Test]
        public void AuthoredRosterContainsExactlyFiveCommonAndTwoStrongerMinibosses()
        {
            Assert.That(catalog.IsValid(out string reason),Is.True,reason);
            Assert.That(catalog.CommonEnemies.Count,Is.EqualTo(5));Assert.That(catalog.MiniBosses.Count,Is.EqualTo(2));
            CollectionAssert.AreEqual(new[]{"formiga","aranha","vespa","besouro","caramujo-aquatico"},Ids(catalog.CommonEnemies));
            CollectionAssert.AreEqual(new[]{"libelula-alfa","sapo-cururu"},Ids(catalog.MiniBosses));
            foreach(var d in catalog.GetAllEnemies())
            {
                Assert.That(d.Prefab.GetComponent<PooledEnemy>(),Is.Not.Null);Assert.That(d.Prefab.layer,Is.EqualTo(9));
                Assert.That(d.Prefab.GetComponentsInChildren<SpriteRenderer>().Length,Is.GreaterThan(0));
                if(d.HasTelegraphedAttack){Assert.That(d.TelegraphVisual.GetComponent<EnemyAreaEffect>(),Is.Not.Null);Assert.That(d.TelegraphAudio.samples,Is.GreaterThan(0));
                    Assert.That(d.TelegraphVisual.GetComponent<LineRenderer>().sharedMaterial.shader.name,Is.EqualTo("QuackQuack/FeedbackRing"));}
            }
            foreach(var mini in catalog.MiniBosses)foreach(var common in catalog.CommonEnemies)Assert.That(mini.Health,Is.GreaterThan(common.Health));
        }
        [Test]
        public void ClosedTimelineRequestsBothMinibossesBeforeExternalFinalBossExactlyOnce()
        {
            var run=new RunState(56);var schedule=catalog.CreateSchedule(run);run.TryTransition(RunPhase.Playing);run.Advance(900);
            var due=schedule.CollectDue();Assert.That(due.Count,Is.EqualTo(9));
            Assert.That(due[3],Is.EqualTo("encounter-libelula-alfa"));Assert.That(due[6],Is.EqualTo("encounter-sapo-cururu"));
            Assert.That(due[8],Is.EqualTo("encounter-sapo-rei"));Assert.That(schedule.CollectDue(),Is.Empty);
            Assert.That(catalog.FinalBossEncounter.EnemyIds[0],Is.EqualTo("sapo-rei"));
        }
        [Test]
        public void CollectionsAndReturnedArraysCannotMutateCatalog()
        {
            string before=EditorJsonUtility.ToJson(catalog);
            Assert.Throws<NotSupportedException>(()=>((IList<EnemyDefinition>)catalog.CommonEnemies)[0]=null);
            var all=catalog.GetAllEnemies();all[0]=null;var encounters=catalog.GetAllEncounters();encounters[0]=null;
            Assert.That(EditorJsonUtility.ToJson(catalog),Is.EqualTo(before));Assert.That(catalog.IsValid(out _),Is.True);
        }
        [TestCase("commonEnemies",4)] [TestCase("miniBosses",1)] [TestCase("miniBossEncounters",1)] [TestCase("waves",0)]
        public void MissingRequiredRosterOrScheduleEntriesAreRejected(string field,int size)
        {var copy=Copy(catalog);Set(copy,field,p=>p.arraySize=size);Assert.That(copy.IsValid(out _),Is.False);}
        [Test]
        public void DuplicateOrWrongCommonAndMiniRolesAreRejected()
        {
            var copy=Copy(catalog);Set(copy,"commonEnemies",p=>p.GetArrayElementAtIndex(1).objectReferenceValue=catalog.CommonEnemies[0]);Assert.That(copy.IsValid(out _),Is.False);
            copy=Copy(catalog);Set(copy,"miniBosses",p=>p.GetArrayElementAtIndex(0).objectReferenceValue=catalog.MiniBosses[1]);Assert.That(copy.IsValid(out _),Is.False);
        }
        [TestCase("startSeconds")] [TestCase("spawnInterval")]
        public void GapsAndRegressingPressureAreRejected(string field)
        {
            var copy=Copy(catalog);var wave=Copy(catalog.Waves[1]);
            Set(wave,field,p=>{if(field=="startSeconds")p.doubleValue+=1;else p.floatValue=10;});
            Set(copy,"waves",p=>p.GetArrayElementAtIndex(1).objectReferenceValue=wave);Assert.That(copy.IsValid(out _),Is.False);
        }
        [TestCase("unknown")] [TestCase("libelula-alfa")] [TestCase("sapo-rei")]
        public void CommonWavesCannotContainUnknownMinibossOrFinalBoss(string id)
        {
            var copy=Copy(catalog);var wave=Copy(catalog.Waves[0]);Set(wave,"enemyIds",p=>p.GetArrayElementAtIndex(0).stringValue=id);
            Set(copy,"waves",p=>p.GetArrayElementAtIndex(0).objectReferenceValue=wave);Assert.That(copy.IsValid(out _),Is.False);
        }
        [TestCase(0)] [TestCase(401)]
        public void InvalidGlobalLimitIsRejected(int limit)
        {var copy=Copy(catalog);Set(copy,"activeLimit",p=>p.intValue=limit);Assert.That(copy.IsValid(out _),Is.False);}
        [Test]
        public void MilestoneCannotBeMovedOrRemovedAndInvalidCatalogCannotCreateSession()
        {
            var copy=Copy(catalog);Set(copy,"finalBossEncounter",p=>p.objectReferenceValue=null);Assert.That(copy.IsValid(out _),Is.False);
            Assert.Throws<InvalidOperationException>(()=>copy.CreateSchedule(new RunState(56)));
            copy=Copy(catalog);var milestone=Copy(catalog.MiniBossEncounters[0]);Set(milestone,"startSeconds",p=>p.doubleValue=301);
            Set(copy,"miniBossEncounters",p=>p.GetArrayElementAtIndex(0).objectReferenceValue=milestone);Assert.That(copy.IsValid(out _),Is.False);
        }
        private static string[] Ids(IReadOnlyList<EnemyDefinition> source){var ids=new string[source.Count];for(int i=0;i<ids.Length;i++)ids[i]=source[i].Id;return ids;}
        private T Copy<T>(T source)where T:Object{var copy=Object.Instantiate(source);copies.Add(copy);return copy;}
        private static void Set(Object obj,string name,Action<SerializedProperty> action){var s=new SerializedObject(obj);action(s.FindProperty(name));s.ApplyModifiedPropertiesWithoutUndo();}
    }
}
