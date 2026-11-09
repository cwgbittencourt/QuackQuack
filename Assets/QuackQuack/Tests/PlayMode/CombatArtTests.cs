using System.Linq;
using System.Reflection;
using NUnit.Framework;
using QuackQuack.Content.Weapons;
using QuackQuack.Gameplay.Weapons;
using QuackQuack.Presentation.Weapons;
using UnityEngine;

namespace QuackQuack.Tests.PlayMode
{
    public class CombatArtTests
    {
        private GameObject root;
        private Texture2D texture;
        private Sprite sprite;
        private Material material;
        [SetUp] public void Setup()
        {
            root=new GameObject("Combat art tests");texture=new Texture2D(64,64);
            texture.SetPixels(Enumerable.Repeat(Color.white,64*64).ToArray());texture.Apply();
            sprite=Sprite.Create(texture,new Rect(0,0,64,64),new Vector2(.5f,.5f),16);
            material=new Material(Shader.Find("Sprites/Default"));
        }
        [TearDown] public void Cleanup(){Object.DestroyImmediate(root);Object.DestroyImmediate(sprite);Object.DestroyImmediate(texture);Object.DestroyImmediate(material);}

        [Test] public void FrontalDecorationStaysInsideNominalSectorAndUsesSimulationTime()
        {
            var view=root.AddComponent<WeaponVisuals>();view.Initialize(1);
            view.PulseFrontal(0,Vector2.zero,Vector2.right,4,60,10,sprite,material);
            var image=root.GetComponentsInChildren<SpriteRenderer>().Single(s=>s.name=="Frontal art 0");
            Assert.That(image.sharedMaterial,Is.SameAs(material));
            var bounds=image.bounds;
            foreach(var point in new[]{new Vector2(bounds.min.x,bounds.min.y),new Vector2(bounds.min.x,bounds.max.y),new Vector2(bounds.max.x,bounds.min.y),new Vector2(bounds.max.x,bounds.max.y)})
            {Assert.That(point.magnitude,Is.LessThanOrEqualTo(4));Assert.That(Vector2.Angle(Vector2.right,point),Is.LessThanOrEqualTo(30));}
            var tint=image.color;view.Advance(10);Assert.That(image.color,Is.EqualTo(tint));
            view.Advance(10.3);Assert.That(image.enabled,Is.False);
        }
        [Test] public void AuraMaterialAndDiameterFollowTheEffectiveRadiusOnNonUnitSprite()
        {
            root.transform.localScale=new Vector3(2,3,1);
            var view=root.AddComponent<WeaponVisuals>();view.Initialize(1);
            view.ShowAura(0,Vector2.zero,2.5f,sprite,1,material);
            var image=root.GetComponentsInChildren<SpriteRenderer>().Single(s=>s.name=="Aura 0");
            Assert.That(image.bounds.size.x,Is.EqualTo(5).Within(.001));Assert.That(image.bounds.size.y,Is.EqualTo(5).Within(.001));
            Assert.That(image.sharedMaterial,Is.SameAs(material));view.HideAll();Assert.That(image.enabled,Is.False);
        }
        [Test] public void RocketBurstIsDistinctAndNeverExceedsRealDamageRadius()
        {
            var view=root.AddComponent<WeaponVisuals>();view.Initialize(1,1);view.PulseEggArea(Vector2.zero,3,0,true);
            var line=root.GetComponentsInChildren<LineRenderer>().Single(l=>l.name=="Egg burst 0");
            Assert.That(line.GetPosition(0).magnitude,Is.EqualTo(3).Within(.001));Assert.That(line.GetPosition(1).magnitude,Is.LessThan(3));
            for(int i=0;i<line.positionCount;i++)Assert.That(line.GetPosition(i).magnitude,Is.LessThanOrEqualTo(3.001));
            view.PulseEggArea(Vector2.zero,2,1);for(int i=0;i<line.positionCount;i++)Assert.That(line.GetPosition(i).magnitude,Is.EqualTo(2).Within(.001));
            view.HideAll();Assert.That(line.enabled,Is.False);
        }
        [Test] public void ReusedBubbleRestoresMaterialRotationTintAndExactHitDiameter()
        {
            var definition=ScriptableObject.CreateInstance<WeaponDefinition>();
            try
            {
                Set(definition,"behavior",WeaponBehavior.Bubbles);Set(definition,"projectileSprite",sprite);Set(definition,"projectileMaterial",material);
                var projectile=root.AddComponent<WeaponProjectile>();
                typeof(WeaponProjectile).GetMethod("Begin",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(projectile,
                    new object[]{definition,new WeaponLevel(2,1,4,1,0),ProjectileMotion.Straight,Vector2.zero,Vector2.right,null,(float?).36f});
                var image=root.GetComponent<SpriteRenderer>();Assert.That(image.sharedMaterial,Is.SameAs(material));
                Assert.That(image.bounds.size.x,Is.EqualTo(.72f).Within(.001));Assert.That(image.bounds.size.y,Is.EqualTo(.72f).Within(.001));
                image.color=Color.red;root.transform.rotation=Quaternion.Euler(0,0,80);projectile.ResetState();
                Assert.That(image.sprite,Is.Null);Assert.That(image.color,Is.EqualTo(Color.white));Assert.That(image.sharedMaterial,Is.Not.SameAs(material));
                Assert.That(root.transform.localScale,Is.EqualTo(Vector3.one));Assert.That(root.transform.rotation,Is.EqualTo(Quaternion.identity));
            }
            finally{Object.DestroyImmediate(definition);}
        }
        private static void Set(object obj,string field,object value)=>obj.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(obj,value);
    }
}
