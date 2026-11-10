using System.Collections;
using System.Reflection;
using NUnit.Framework;
using QuackQuack.Content.Characters;
using QuackQuack.Core.Run;
using QuackQuack.Core.Shared;
using QuackQuack.Gameplay.Player;
using QuackQuack.Platform.Input;
using QuackQuack.Systems.Combat;
using UnityEngine;
using UnityEngine.TestTools;

namespace QuackQuack.Tests.PlayMode
{
    public class CartoonActorVisualTests
    {
        private GameObject root,child;
        private CharacterDefinition character;
        private QuackDefinition quack;
        private RunState run;
        private PlayerController player;
        private SpriteRenderer image;
        private Vector3 rest;
        private sealed class Input : IPlayerInput { public PlayerCommand Read()=>new PlayerCommand(Vector2.zero); }
        [SetUp] public void Setup()
        {
            character=ScriptableObject.CreateInstance<CharacterDefinition>();quack=ScriptableObject.CreateInstance<QuackDefinition>();
            Set(character,"id","pato");Set(quack,"id","quack");Set(character,"quack",quack);
            run=new RunState(43);run.TryTransition(RunPhase.Playing);
            root=new GameObject("Visual fixture");root.SetActive(false);player=root.AddComponent<PlayerController>();
            child=new GameObject("Cartoon actor",typeof(SpriteRenderer));child.transform.SetParent(root.transform,false);
            rest=child.transform.localScale=new Vector3(.3f,.4f,1);image=child.GetComponent<SpriteRenderer>();child.AddComponent<CartoonActorVisual>();
            player.Initialize(character,new Input(),run,new GameplayEventBus());root.SetActive(true);
        }
        [TearDown] public void Cleanup(){Object.DestroyImmediate(root);Object.DestroyImmediate(character);Object.DestroyImmediate(quack);}
        [Test] public void HudAnchorClearsVisibleCrestWithoutMovingThePhysicalShape()
        {
            var texture=new Texture2D(8,8);var sprite=Sprite.Create(texture,new Rect(0,0,8,8),Vector2.one*.5f,2);
            try
            {
                image.sprite=sprite;child.transform.localScale=new Vector3(.3f,3,1);
                var physical=new Vector3(2,.42f,3);var before=root.GetComponent<CircleCollider2D>().radius;
                var anchor=child.GetComponent<CartoonActorVisual>().RaiseHudAnchor(physical);
                Assert.That(anchor.y,Is.EqualTo(image.bounds.max.y));Assert.That(anchor.x,Is.EqualTo(physical.x));Assert.That(anchor.z,Is.EqualTo(physical.z));
                Assert.That(root.GetComponent<CircleCollider2D>().radius,Is.EqualTo(before));
                image.enabled=false;Assert.That(child.GetComponent<CartoonActorVisual>().RaiseHudAnchor(physical),Is.EqualTo(physical));
            }
            finally{Object.DestroyImmediate(sprite);Object.DestroyImmediate(texture);}
        }
        [UnityTest] public IEnumerator IdleAnimationNeverChangesPhysicalBodyAndFreezesInPause()
        {
            var position=root.transform.position;var scale=root.transform.localScale;float radius=root.GetComponent<CircleCollider2D>().radius;
            yield return null;yield return null;
            Assert.That(child.transform.localScale,Is.Not.EqualTo(rest));
            Assert.That(root.transform.position,Is.EqualTo(position));Assert.That(root.transform.localScale,Is.EqualTo(scale));
            Assert.That(root.GetComponent<CircleCollider2D>().radius,Is.EqualTo(radius));
            run.TryTransition(RunPhase.Paused);var paused=child.transform.localScale;
            yield return null;yield return null;Assert.That(child.transform.localScale,Is.EqualTo(paused));
            run.TryResume();yield return null;Assert.That(child.transform.localScale,Is.Not.EqualTo(paused));
        }
        [UnityTest] public IEnumerator DisablingAndReusingRestoresScaleTintAndFacing()
        {
            yield return null;image.color=Color.red;image.flipX=true;root.SetActive(false);
            Assert.That(child.transform.localScale,Is.EqualTo(rest));Assert.That(image.color,Is.EqualTo(Color.white));Assert.That(image.flipX,Is.False);
            root.SetActive(true);yield return null;Assert.That(image.color,Is.EqualTo(Color.white));
            Assert.That(child.GetComponent<Collider2D>(),Is.Null);
        }
        [UnityTest] public IEnumerator DamageFlashUsesHealthAndDoesNotExpireWhilePaused()
        {
            yield return null;player.State.TakeDamage(5,1,0);yield return null;
            Assert.That(image.color,Is.Not.EqualTo(Color.white));
            run.TryTransition(RunPhase.Paused);var tint=image.color;
            yield return new WaitForSeconds(.2f);Assert.That(image.color,Is.EqualTo(tint));
            run.TryResume();yield return new WaitForSeconds(.2f);Assert.That(image.color,Is.EqualTo(Color.white));
        }
        private static void Set(object target,string name,object value)
        {for(var type=target.GetType();type!=null;type=type.BaseType){var field=type.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic);if(field!=null){field.SetValue(target,value);return;}}Assert.Fail("Missing field "+name);}
    }
}
