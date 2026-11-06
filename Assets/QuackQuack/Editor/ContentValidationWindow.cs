using System;
using System.Collections.Generic;
using System.Linq;
using QuackQuack.Content.Catalogs;
using QuackQuack.Content.Characters;
using QuackQuack.Content.Encounters;
using QuackQuack.Content.Enemies;
using QuackQuack.Content.Passives;
using QuackQuack.Content.Pickups;
using QuackQuack.Content.Relics;
using QuackQuack.Content.Shared;
using QuackQuack.Content.Weapons;
using QuackQuack.Platform.Input;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace QuackQuack.Editor
{
    public enum ContentIssueSeverity { Error, Warning }
    public sealed class ContentValidationIssue
    {
        public readonly ContentIssueSeverity Severity;
        public readonly string Code, Message, Path, Property;
        public readonly Object Asset;
        public ContentValidationIssue(Object asset, string code, string message, string property = "", ContentIssueSeverity severity = ContentIssueSeverity.Error)
        {
            Asset = asset; Code = code; Message = message; Property = property; Severity = severity;
            Path = asset == null ? "" : AssetDatabase.GetAssetPath(asset);
        }
        public override string ToString() => $"[{Code}] {Path} ({(Asset != null ? Asset.name : "missing")}) {Property}: {Message}";
    }
    public sealed class ContentValidationReport
    {
        public int AssetCount { get; internal set; }
        public readonly List<ContentValidationIssue> Issues = new List<ContentValidationIssue>();
        public int Errors => Issues.Count(i => i.Severity == ContentIssueSeverity.Error);
        public int Warnings => Issues.Count(i => i.Severity == ContentIssueSeverity.Warning);
    }

    // Read-only authoring tool: domain validators remain the source of numeric limits.
    public sealed class ContentValidationWindow : EditorWindow
    {
        public const string DefaultRoot = "Assets/QuackQuack";
        private ContentValidationReport report;
        private Vector2 scroll;
        [MenuItem("QuackQuack/Validate content")]
        public static void Open()
        {
            var window = GetWindow<ContentValidationWindow>("Validação de conteúdo");
            window.report = ValidateProject(); window.Show();
        }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("ScriptableObjects em " + DefaultRoot, EditorStyles.boldLabel);
            if (GUILayout.Button("Validar novamente")) report = ValidateProject();
            if (report == null) return;
            EditorGUILayout.LabelField($"{report.AssetCount} assets / {report.Errors} erros / {report.Warnings} avisos");
            if (report.Issues.Count == 0) EditorGUILayout.HelpBox("Conteúdo válido. Nenhum asset foi alterado.", MessageType.Info);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var issue in report.Issues)
            {
                EditorGUILayout.HelpBox(issue.ToString(), issue.Severity == ContentIssueSeverity.Error ? MessageType.Error : MessageType.Warning);
                if (issue.Asset != null && GUILayout.Button("Selecionar " + issue.Asset.name))
                { Selection.activeObject = issue.Asset; EditorGUIUtility.PingObject(issue.Asset); }
            }
            EditorGUILayout.EndScrollView();
        }

        public static ContentValidationReport ValidateProject(string root = DefaultRoot)
        {
            if (string.IsNullOrWhiteSpace(root) || !AssetDatabase.IsValidFolder(root))
            {
                var invalid = new ContentValidationReport();
                invalid.Issues.Add(new ContentValidationIssue(null, "InvalidRoot", "Validation folder does not exist: " + root));
                return invalid;
            }
            var assets = new List<ScriptableObject>();
            var unreadable = new List<string>();
            foreach (var path in AssetDatabase.FindAssets("", new[] { root }).Select(AssetDatabase.GUIDToAssetPath).Distinct().OrderBy(p => p, StringComparer.Ordinal))
            {
                if (!path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) && !path.EndsWith(".inputactions", StringComparison.OrdinalIgnoreCase)) continue;
                var loaded = AssetDatabase.LoadAllAssetsAtPath(path);
                assets.AddRange(loaded.OfType<ScriptableObject>());
                if (loaded.Length == 0 || loaded.Any(a => a == null)) unreadable.Add(path);
            }
            var result = Validate(assets);
            foreach (var path in unreadable) result.Issues.Add(new ContentValidationIssue(null, "UnreadableAsset", "Unable to load asset or its script: " + path));
            if (result.AssetCount == 0) result.Issues.Add(new ContentValidationIssue(null, "EmptyScope", "No ScriptableObjects found in " + root));
            Sort(result); return result;
        }

        public static ContentValidationReport Validate(IEnumerable<ScriptableObject> input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            var result = new ContentValidationReport();
            var supplied = input.ToArray();
            if (supplied.Any(a => a == null)) Add(result, null, "MissingAsset", "The validation set contains a missing asset.");
            var assets = supplied.Where(a => a != null).Distinct().OrderBy(a => AssetDatabase.GetAssetPath(a), StringComparer.Ordinal)
                .ThenBy(a => a.name, StringComparer.Ordinal).ToArray();
            result.AssetCount = assets.Length;
            var ids = new Dictionary<string, List<ContentDefinition>>(StringComparer.Ordinal);
            foreach (var asset in assets)
            {
                if (asset is ContentDefinition definition && !string.IsNullOrWhiteSpace(definition.Id))
                {
                    if (!ids.TryGetValue(definition.Id, out var matches)) ids.Add(definition.Id, matches = new List<ContentDefinition>());
                    matches.Add(definition);
                }
                try
                {
                    ValidateDomain(asset, result);
                    ValidateSerialized(asset, result);
                }
                catch (Exception e) { Add(result, asset, "ValidationException", e.GetType().Name + ": " + e.Message); }
            }
            foreach (var pair in ids.Where(p => p.Value.Count > 1))
                foreach (var asset in pair.Value) Add(result, asset, "DuplicateId", "ID is shared by distinct assets: " + pair.Key, "id");
            var recipes = new Dictionary<(string, string), EvolutionDefinition>();
            foreach (var asset in assets)
            {
                try
                {
                    if (asset is WeaponDefinition weapon)
                    {
                        var passive = Resolve<PassiveDefinition>(ids, weapon.CompatiblePassiveId, asset, "compatiblePassiveId", result);
                        Resolve<WeaponDefinition>(ids, weapon.EvolutionWeaponId, asset, "evolutionWeaponId", result);
                        if (passive != null && passive.CompatibleWeaponId != weapon.Id)
                            Add(result, asset, "ReciprocalReference", "Compatible passive points to another weapon.", "compatiblePassiveId");
                    }
                    if (asset is PassiveDefinition p)
                    {
                        var compatibleWeapon = Resolve<WeaponDefinition>(ids, p.CompatibleWeaponId, asset, "compatibleWeaponId", result);
                        if (compatibleWeapon != null && !string.IsNullOrEmpty(compatibleWeapon.CompatiblePassiveId) && compatibleWeapon.CompatiblePassiveId != p.Id)
                            Add(result, asset, "ReciprocalReference", "Compatible weapon points to another passive.", "compatibleWeaponId");
                    }
                    if (asset is EncounterDefinition encounter)
                    {
                        var array = new SerializedObject(asset).FindProperty("enemyIds");
                        for (int i = 0; i < array.arraySize; i++)
                        {
                            string id = array.GetArrayElementAtIndex(i).stringValue;
                            // The final boss is an external controller, not an EnemyDefinition.
                            if (encounter.Kind == EncounterKind.FrogKing && id == LakeEncounterCatalog.FinalBossId) continue;
                            Resolve<EnemyDefinition>(ids, id, asset, "enemyIds.Array.data[" + i + "]", result);
                        }
                    }
                    if (asset is EvolutionDefinition evolution && evolution.BaseWeapon != null && evolution.RequiredPassive != null)
                    {
                        var key = (evolution.BaseWeapon.Id, evolution.RequiredPassive.Id);
                        if (recipes.TryGetValue(key, out var first))
                        {
                            Add(result, first, "DuplicateRecipe", "Repeated base weapon/passive combination.");
                            Add(result, asset, "DuplicateRecipe", "Repeated base weapon/passive combination.");
                        }
                        else recipes.Add(key, evolution);
                    }
                }
                catch (Exception e) { Add(result, asset, "ReferenceException", e.GetType().Name + ": " + e.Message); }
            }
            Sort(result); return result;
        }

        private static void ValidateDomain(ScriptableObject asset, ContentValidationReport report)
        {
            string reason = null; bool valid = true;
            switch (asset)
            {
                case PickupPolicyDefinition p: valid = p.IsValid(out reason); break;
                case ContentDefinition d: valid = d.IsValid(out reason); break;
                case LakeEncounterCatalog c: valid = c.IsValid(out reason); break;
                case PrototypeRelicCatalog c: valid = c.IsValid(out reason); break;
                case FrogKingSettings s: valid = s.IsValid(out reason); break;
                case EnemyBehaviorSettings s: valid = s.IsValid; break;
                case LakeRunSettings s: valid = s.IsValid; break;
                case ResultsProgressionDefinition d:
                    try { d.CreateRules(); } catch (ArgumentException e) { valid = false; reason = e.Message; }
                    break;
                case PlayerInputConfiguration c: valid = c.Actions != null && c.MobileControlsPrefab != null; break;
                case InputActionAsset a:
                    var map = a.FindActionMap("Gameplay", false);
                    valid = map != null && map.FindAction("Move", false) != null && map.FindAction("Quack", false) != null && map.FindAction("Pause", false) != null && map.FindAction("Relics", false) != null;
                    break;
                case InputActionReference r: valid = r.asset != null && r.action != null; break;
                default:
                    report.Issues.Add(new ContentValidationIssue(asset, "UnsupportedType", "No domain validator registered for " + asset.GetType().FullName, severity: ContentIssueSeverity.Warning));
                    break;
            }
            if (!valid) Add(report, asset, "InvalidAsset", reason ?? "Required references or authored limits are invalid.");
            if (asset is EnemyDefinition enemy && enemy.ExperienceReward <= 0)
                Add(report, asset, "NonPositiveExperience", "Every enemy defeat requires positive authored XP.", "experienceReward");
            if (asset is LakeRunSettings run)
            {
                if (run.PickupPolicy == null)
                    Add(report, asset, "MissingPickupPolicy", "A pickup policy is required.", "PickupPolicy");
                else if (!run.PickupPolicy.IsValid(out var policyReason))
                    Add(report, asset, "InvalidPickupPolicy", policyReason, "PickupPolicy");
            }
        }

        private static void ValidateSerialized(ScriptableObject asset, ContentValidationReport report)
        {
            var data = new SerializedObject(asset);
            var property = data.GetIterator();
            while (property.Next(true))
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
                    Add(report, asset, "MissingReference", "Serialized reference is missing.", property.propertyPath);
                if (property.propertyType == SerializedPropertyType.Float && (double.IsNaN(property.doubleValue) || double.IsInfinity(property.doubleValue)))
                    Add(report, asset, "NonFinite", "Numeric value must be finite.", property.propertyPath);
            }
        }
        private static T Resolve<T>(Dictionary<string, List<ContentDefinition>> ids, string id, Object asset, string property, ContentValidationReport report) where T : ContentDefinition
        {
            if (string.IsNullOrWhiteSpace(id)) return null; // Required empty fields are reported by domain validation.
            if (!ids.TryGetValue(id, out var matches)) { Add(report, asset, "UnknownId", "Unknown " + typeof(T).Name + " ID: " + id, property); return null; }
            if (matches.Count != 1) { Add(report, asset, "AmbiguousId", "Referenced ID is duplicated: " + id, property); return null; }
            if (!(matches[0] is T typed)) { Add(report, asset, "ReferenceType", "ID has the wrong content type: " + id, property); return null; }
            return typed;
        }
        private static void Add(ContentValidationReport report, Object asset, string code, string message, string property = "") =>
            report.Issues.Add(new ContentValidationIssue(asset, code, message, property));
        private static void Sort(ContentValidationReport report) => report.Issues.Sort((a, b) =>
            StringComparer.Ordinal.Compare(a.Path + "|" + a.Code + "|" + a.Property + "|" + a.Message, b.Path + "|" + b.Code + "|" + b.Property + "|" + b.Message));

        public static void ValidateForCI()
        {
            var result = ValidateProject();
            foreach (var issue in result.Issues)
            {
                if (issue.Severity == ContentIssueSeverity.Error) Debug.LogError("T073 " + issue, issue.Asset);
                else Debug.LogWarning("T073 " + issue, issue.Asset);
            }
            if (result.Errors != 0) throw new BuildFailedException("Content validation failed: " + result.Errors + " errors.");
            Debug.Log($"T073_CONTENT_VALIDATION_OK assets={result.AssetCount} warnings={result.Warnings}");
        }
    }
}
