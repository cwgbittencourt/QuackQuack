using System;
using QuackQuack.Content.Encounters;
using QuackQuack.Content.Pickups;
using UnityEditor;
using UnityEngine;

namespace QuackQuack.Editor
{
    public static class PickupPolicySetup
    {
        public const string PolicyPath = "Assets/QuackQuack/Content/Pickups/LakePickupPolicy.asset";
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
            if (settings.BossExperience <= 0) settings.BossExperience = PrototypeBossExperience;
            EditorUtility.SetDirty(settings);
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
