using System;
using QuackQuack.Content.Encounters;
using QuackQuack.Content.Pickups;
using QuackQuack.Gameplay.Pickups;
using UnityEditor;
using UnityEngine;

namespace QuackQuack.Editor
{
    public static class PickupPolicySetup
    {
        public const string PolicyPath = "Assets/QuackQuack/Content/Pickups/LakePickupPolicy.asset";
        public const string HealingPath = "Assets/QuackQuack/Prefabs/Prototype/HealingPickup.prefab";
        public const string ExperiencePath = "Assets/QuackQuack/Prefabs/Prototype/ExperiencePickup.prefab";
        // Authoring choice for T026: reuse the existing special-enemy reward tier.
        public const int PrototypeBossExperience = 50;

        [MenuItem("QuackQuack/Build pickup policy (002 T026)")]
        public static void Build()
        {
            var settings = AssetDatabase.LoadAssetAtPath<LakeRunSettings>(LakeRunSetup.SettingsPath);
            if (settings == null) throw new InvalidOperationException("Generate LakeRunSettings before the pickup policy.");
            var policy = EnsurePolicy();
            SetDefaults(policy);
            Configure(settings);
            if (!settings.IsValid) throw new InvalidOperationException("Invalid lake settings after pickup policy generation.");
            AssetDatabase.SaveAssets();
            Debug.Log("T026_PICKUP_POLICY_OK chance=0.10 fractions=0.15/0.50 capture=3 speed=1.25 bossXP=" + settings.BossExperience);
        }

        public static void Configure(LakeRunSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            settings.PickupPolicy = EnsurePolicy();
            settings.HealingPrefab = EnsureHealingPrefab();
            if (settings.BossExperience <= 0) settings.BossExperience = PrototypeBossExperience;
            EditorUtility.SetDirty(settings);
        }

        [MenuItem("QuackQuack/Build pickup prefabs (002 T028)")]
        public static void BuildPickups()
        {
            var xp = PrefabUtility.LoadPrefabContents(ExperiencePath);
            try
            {
                if (xp.GetComponent<ExperiencePickup>() == null) throw new InvalidOperationException("XP prefab requires ExperiencePickup.");
                xp.layer = ExperiencePickup.PhysicsLayer; xp.GetComponent<CircleCollider2D>().isTrigger = true;
                PrefabUtility.SaveAsPrefabAsset(xp, ExperiencePath);
            }
            finally { PrefabUtility.UnloadPrefabContents(xp); }
            var settings = AssetDatabase.LoadAssetAtPath<LakeRunSettings>(LakeRunSetup.SettingsPath);
            if (settings == null) throw new InvalidOperationException("Lake settings are required.");
            Configure(settings);
            if (!settings.IsValid) throw new InvalidOperationException("Invalid pickup composition.");
            AssetDatabase.SaveAssets(); Debug.Log("T028_PICKUP_PREFABS_OK");
        }

        private static HealingPickup EnsureHealingPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(HealingPath);
            if (existing != null)
            {
                var component = existing.GetComponent<HealingPickup>();
                if (component == null) throw new InvalidOperationException("Healing prefab requires HealingPickup.");
                return component;
            }
            var root = new GameObject("Healing pickup");
            try
            {
                root.layer = ExperiencePickup.PhysicsLayer; root.transform.localScale = Vector3.one * 0.4f;
                root.AddComponent<HealingPickup>(); root.GetComponent<CircleCollider2D>().isTrigger = true;
                var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/QuackQuack/Art/Prototype/CircleUnlit.mat");
                var renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/QuackQuack/Art/Prototype/Circle.png");
                renderer.sharedMaterial = material; renderer.color = new Color(0.25f, 0.85f, 0.35f); renderer.sortingOrder = 12;
                foreach (var scale in new[] { new Vector3(0.5f, 0.15f, 1), new Vector3(0.15f, 0.5f, 1) })
                {
                    var cross = new GameObject("Healing cross"); cross.transform.SetParent(root.transform, false); cross.transform.localScale = scale;
                    var image = cross.AddComponent<SpriteRenderer>(); image.sharedMaterial = material;
                    image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/QuackQuack/Art/Prototype/Square.png"); image.sortingOrder = 13;
                }
                return PrefabUtility.SaveAsPrefabAsset(root, HealingPath).GetComponent<HealingPickup>();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static PickupPolicyDefinition EnsurePolicy()
        {
            const string folder = "Assets/QuackQuack/Content/Pickups";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/QuackQuack/Content", "Pickups");
            var policy = AssetDatabase.LoadAssetAtPath<PickupPolicyDefinition>(PolicyPath);
            if (policy != null) return policy;
            policy = ScriptableObject.CreateInstance<PickupPolicyDefinition>();
            AssetDatabase.CreateAsset(policy, PolicyPath);
            SetDefaults(policy);
            return policy;
        }

        private static void SetDefaults(PickupPolicyDefinition policy)
        {
            var data = new SerializedObject(policy);
            data.FindProperty("id").stringValue = "lake-pickup-policy";
            data.FindProperty("displayName").stringValue = "Recompensas do Lago";
            data.FindProperty("commonHealChance").floatValue = 0.1f;
            data.FindProperty("commonHealFraction").floatValue = 0.15f;
            data.FindProperty("miniBossHealFraction").floatValue = 0.5f;
            data.FindProperty("captureBodyRadiusMultiplier").floatValue = 3;
            data.FindProperty("attractionWaterSpeedMultiplier").floatValue = 1.25f;
            data.ApplyModifiedPropertiesWithoutUndo();
            if (!policy.IsValid(out var reason)) throw new InvalidOperationException(reason);
        }
    }
}
