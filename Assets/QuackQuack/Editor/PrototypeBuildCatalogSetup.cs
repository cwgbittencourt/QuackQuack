using System;
using System.IO;
using QuackQuack.Content.Catalogs;
using QuackQuack.Content.Passives;
using QuackQuack.Content.Shared;
using QuackQuack.Content.Weapons;
using UnityEditor;
using UnityEngine;

namespace QuackQuack.Editor
{
    public static class PrototypeBuildCatalogSetup
    {
        private const string Root = "Assets/QuackQuack/Content";
        public const string CatalogPath = Root + "/Catalogs/PrototypeBuildCatalog.asset";

        [MenuItem("QuackQuack/Build T043 prototype build catalog")]
        public static void Build()
        {
            Folder(Root + "/Catalogs"); Folder(Root + "/Weapons/Prototype"); Folder(Root + "/Passives/Prototype");
            var circle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/QuackQuack/Art/Prototype/Circle.png");
            if (circle == null) throw new InvalidOperationException("The prototype Circle sprite must exist before creating weapon content.");
            var weapons = new[]
            {
                Weapon("bumerangue", "Bumerangue", WeaponBehavior.Boomerang, WeaponTargetMode.NearestEnemy, 3, 1.8f, 6, 1, 0, 7, 1.8f, circle),
                Weapon("ovos", "Ovos", WeaponBehavior.Eggs, WeaponTargetMode.Self, 5, 3, 4, 1, 1.3f, 1, 6, circle),
                Weapon("quack-frontal", "Quack frontal", WeaponBehavior.FrontalQuack, WeaponTargetMode.Forward, 3, 1.6f, 4, 0, 1, 1, 1, circle),
                Weapon("aura-energia", "Aura de energia", WeaponBehavior.EnergyAura, WeaponTargetMode.Self, 2, 1.4f, 3, 0, 2, 1, 1, circle),
                Weapon("foguetes", "Foguetes", WeaponBehavior.Rockets, WeaponTargetMode.NearestEnemy, 7, 2.8f, 8, 1, 1.5f, 5, 3, circle),
                Weapon("bolhas", "Bolhas", WeaponBehavior.Bubbles, WeaponTargetMode.NearestEnemy, 2, 1.2f, 6, 1, 0, 3, 3, circle)
            };
            var passiveIds = new[] { "pena-aerodinamica", "ninho-reforcado", "pulmao-poderoso", "amuleto-lago", "oculos-aviador", "sabao-magico" };
            var passiveNames = new[] { "Pena aerodinâmica", "Ninho reforçado", "Pulmão poderoso", "Amuleto do lago", "Óculos de aviador", "Sabão mágico" };
            var attributes = new[] { PassiveAttribute.AttackSpeed, PassiveAttribute.Damage, PassiveAttribute.BreathCapacity,
                PassiveAttribute.BreathRecovery, PassiveAttribute.Damage, PassiveAttribute.AreaRadius };
            var passives = new PassiveDefinition[6];
            for (int i = 0; i < passives.Length; i++)
            {
                var passive = Load<PassiveDefinition>(Root + "/Passives/Prototype/" + passiveIds[i] + ".asset");
                Identity(passive, passiveIds[i], passiveNames[i]);
                Set(passive, "compatibleWeaponId", p => p.stringValue = weapons[i].Id);
                Set(passive, "attribute", p => p.enumValueIndex = (int)attributes[i]);
                Set(passive, "modifierPerLevel", p => p.floatValue = passiveIds[i] == "sabao-magico" ? 0.16f : 0.08f);
                Set(passive, "modifierLimit", p => p.floatValue = passiveIds[i] == "sabao-magico" ? 0.8f : 0.4f);
                Set(passive, "maxLevel", p => p.intValue = 5);
                passives[i] = passive;
            }
            var results = new[]
            {
                Weapon("tornado-penas", "Tornado de penas", WeaponBehavior.Boomerang, WeaponTargetMode.NearestEnemy, 8, 1.2f, 8, 4, 0, 8, 2.4f, circle),
                Weapon("incubadora-combate", "Incubadora de combate", WeaponBehavior.Eggs, WeaponTargetMode.Self, 12, 2, 5, 3, 2.2f, 1, 8, circle),
                Weapon("quack-supersonico", "Quack supersônico", WeaponBehavior.FrontalQuack, WeaponTargetMode.Forward, 10, 0.9f, 7, 0, 2, 1, 1, circle)
            };
            var evolutions = new EvolutionDefinition[3];
            for (int i = 0; i < evolutions.Length; i++)
            {
                Set(weapons[i], "compatiblePassiveId", p => p.stringValue = passives[i].Id);
                Set(weapons[i], "evolutionWeaponId", p => p.stringValue = results[i].Id);
                var evolution = Load<EvolutionDefinition>(Root + "/Weapons/Prototype/evolucao-" + weapons[i].Id + ".asset");
                Identity(evolution, "evolucao-" + weapons[i].Id, results[i].DisplayName);
                Set(evolution, "baseWeapon", p => p.objectReferenceValue = weapons[i]);
                Set(evolution, "requiredPassive", p => p.objectReferenceValue = passives[i]);
                Set(evolution, "resultWeapon", p => p.objectReferenceValue = results[i]);
                evolutions[i] = evolution;
            }
            var catalog = Load<PrototypeBuildCatalog>(CatalogPath);
            Identity(catalog, "prototype-build", "Build inicial do Lago");
            References(catalog, "weapons", weapons); References(catalog, "passives", passives); References(catalog, "evolutions", evolutions);
            if (!catalog.IsValid(out var reason)) throw new InvalidOperationException(reason);
            AssetDatabase.SaveAssets();
            Debug.Log("T043 catalog validated: 6 basic weapons, 6 passives, 3 evolutions and 3 result weapons.");
        }

        [MenuItem("QuackQuack/Update Soap balance (002 T023)")]
        public static void UpdateSoapBalance()
        {
            var soap = AssetDatabase.LoadAssetAtPath<PassiveDefinition>(Root + "/Passives/Prototype/sabao-magico.asset");
            if (soap == null) throw new InvalidOperationException("Missing Soap definition.");
            Set(soap, "modifierPerLevel", p => p.floatValue = 0.16f);
            Set(soap, "modifierLimit", p => p.floatValue = 0.8f);
            if (!soap.IsValid(out var reason)) throw new InvalidOperationException(reason);
            AssetDatabase.SaveAssets();
            Debug.Log("T023_SOAP_BALANCE_OK perLevel=0.16 limit=0.8");
        }

        private static WeaponDefinition Weapon(string id, string name, WeaponBehavior behavior, WeaponTargetMode mode,
            float damage, float cooldown, float range, int projectiles, float area, float speed, float lifetime, Sprite sprite)
        {
            var weapon = Load<WeaponDefinition>(Root + "/Weapons/Prototype/" + id + ".asset");
            Identity(weapon, id, name);
            Set(weapon, "behavior", p => p.enumValueIndex = (int)behavior); Set(weapon, "targetMode", p => p.enumValueIndex = (int)mode);
            Set(weapon, "compatiblePassiveId", p => p.stringValue = ""); Set(weapon, "evolutionWeaponId", p => p.stringValue = "");
            Set(weapon, "projectileSpeed", p => p.floatValue = speed); Set(weapon, "projectileLifetime", p => p.floatValue = lifetime);
            Set(weapon, "hitRadius", p => p.floatValue = 0.2f); Set(weapon, "turnSpeed", p => p.floatValue = 180);
            Set(weapon, "coneAngle", p => p.floatValue = id == "quack-supersonico" || id == "tornado-penas" ? 120 : 60);
            Set(weapon, "projectileSprite", p => p.objectReferenceValue = sprite);
            Set(weapon, "hasTerrainAffinity", p => p.boolValue = behavior == WeaponBehavior.Eggs || behavior == WeaponBehavior.Bubbles);
            Set(weapon, "landDamageMultiplier", p => p.floatValue = behavior == WeaponBehavior.Eggs ? 1.25f : 1);
            Set(weapon, "waterDamageMultiplier", p => p.floatValue = behavior == WeaponBehavior.Bubbles ? 1.25f : 1);
            Set(weapon, "landCooldownMultiplier", p => p.floatValue = 1); Set(weapon, "waterCooldownMultiplier", p => p.floatValue = 1);
            Set(weapon, "levels", p =>
            {
                p.arraySize = 5;
                for (int level = 0; level < 5; level++)
                {
                    var data = p.GetArrayElementAtIndex(level);
                    data.FindPropertyRelative("damage").floatValue = damage * (1 + level * 0.2f);
                    data.FindPropertyRelative("cooldown").floatValue = cooldown / (1 + level * 0.08f);
                    data.FindPropertyRelative("range").floatValue = range + level * 0.15f;
                    data.FindPropertyRelative("projectileCount").intValue = projectiles;
                    data.FindPropertyRelative("areaRadius").floatValue = area * (1 + level * 0.1f);
                }
            });
            return weapon;
        }
        private static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            Folder(Path.GetDirectoryName(path).Replace('\\', '/'));
            AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
        }
        private static T Load<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); }
            return asset;
        }
        private static void Identity(ContentDefinition asset, string id, string name)
        { Set(asset, "id", p => p.stringValue = id); Set(asset, "displayName", p => p.stringValue = name); }
        private static void Set(UnityEngine.Object asset, string field, Action<SerializedProperty> change)
        {
            var data = new SerializedObject(asset); change(data.FindProperty(field));
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(asset);
        }
        private static void References<T>(UnityEngine.Object asset, string field, T[] values) where T : UnityEngine.Object =>
            Set(asset, field, p => { p.arraySize = values.Length; for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; });
    }
}
