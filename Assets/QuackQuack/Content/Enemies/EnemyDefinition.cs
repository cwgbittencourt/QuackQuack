using System;
using System.Collections.Generic;
using QuackQuack.Content.Shared;
using QuackQuack.Systems.Pooling;
using UnityEngine;

namespace QuackQuack.Content.Enemies
{
    public enum EnemyRole { Swarm, WebCaster, Charger, FrontalArmored, SlowingTrail }

    [CreateAssetMenu(menuName = "QuackQuack/Enemy")]
    public sealed class EnemyDefinition : ContentDefinition
    {
        [SerializeField] private EnemyRole role;
        [SerializeField] private float health = 10, moveSpeed = 2, contactDamage = 1;
        [SerializeField] private float attackInterval = 2, attackRange = 3, attackDamage = 1;
        [SerializeField] private float effectDuration = 2, slowMultiplier = 0.5f;
        [SerializeField] private float chargeSpeed = 6, chargeDuration = 0.5f;
        [SerializeField] private float frontalDamageMultiplier = 0.25f, frontalArcDegrees = 120;
        [SerializeField] private float knockbackResistance, interruptionResistance;
        [SerializeField] private int experienceReward = 1, coinReward;
        [SerializeField] private GameObject prefab;
        [SerializeField] private int poolInitialSize = 16, poolMaxSize = 64;
        [SerializeField] private float telegraphSeconds = 0.5f;
        [SerializeField] private GameObject telegraphVisual;
        [SerializeField] private AudioClip telegraphAudio;

        public EnemyRole Role => role;
        public float Health => health;
        public float MoveSpeed => moveSpeed;
        public float ContactDamage => contactDamage;
        public float AttackInterval => attackInterval;
        public float AttackRange => attackRange;
        public float AttackDamage => attackDamage;
        public float EffectDuration => effectDuration;
        public float SlowMultiplier => slowMultiplier;
        public float ChargeSpeed => chargeSpeed;
        public float ChargeDuration => chargeDuration;
        public float FrontalDamageMultiplier => frontalDamageMultiplier;
        public float FrontalArcDegrees => frontalArcDegrees;
        public float KnockbackResistance => knockbackResistance;
        public float InterruptionResistance => interruptionResistance;
        public int ExperienceReward => experienceReward;
        public int CoinReward => coinReward;
        public GameObject Prefab => prefab;
        public int PoolInitialSize => poolInitialSize;
        public int PoolMaxSize => poolMaxSize;
        public PoolFamily PoolFamily => PoolFamily.Enemy;
        public string PoolKey => "enemy/" + Id;
        public string TelemetryId => Id;
        public bool HasTelegraphedAttack => role == EnemyRole.WebCaster || role == EnemyRole.Charger || role == EnemyRole.SlowingTrail;
        public float TelegraphSeconds => telegraphSeconds;
        public GameObject TelegraphVisual => telegraphVisual;
        public AudioClip TelegraphAudio => telegraphAudio;

        public override bool IsValid(out string reason)
        {
            if (!base.IsValid(out reason)) return false;
            if (string.IsNullOrWhiteSpace(DisplayName)) reason = "Enemy display name is required.";
            else if (!Enum.IsDefined(typeof(EnemyRole), role)) reason = "Enemy tactical role is invalid.";
            else if (!Positive(health) || !Positive(moveSpeed) || !Nonnegative(contactDamage))
                reason = "Enemy health/speed must be positive and contact damage nonnegative, all finite.";
            else if (!Unit(knockbackResistance) || !Unit(interruptionResistance))
                reason = "Enemy resistances must be finite normalized values.";
            else if (experienceReward < 0 || coinReward < 0) reason = "Enemy rewards cannot be negative.";
            else if (poolInitialSize <= 0 || poolMaxSize < poolInitialSize) reason = "Enemy pool requires 0 < initial <= maximum.";
            else if (prefab == null || !HasPoolLifecycle(prefab)) reason = "Enemy prefab requires a root IPoolLifecycle component.";
            else if (HasTelegraphedAttack && (!Positive(attackInterval) || !Positive(attackRange) || !Nonnegative(attackDamage)))
                reason = "Special attack interval/range must be positive and damage nonnegative, all finite.";
            else if (HasTelegraphedAttack && (!Positive(telegraphSeconds) || telegraphSeconds > attackInterval ||
                telegraphVisual == null || telegraphAudio == null || telegraphAudio.samples <= 0))
                reason = "Special attacks require visual/audio cues and positive lead time no greater than their interval.";
            else if ((role == EnemyRole.WebCaster || role == EnemyRole.SlowingTrail) &&
                (!Positive(effectDuration) || !Positive(slowMultiplier) || slowMultiplier >= 1))
                reason = "Web/trail requires positive duration and a finite speed multiplier between zero and one.";
            else if (role == EnemyRole.Charger && (!Positive(chargeSpeed) || chargeSpeed <= moveSpeed || !Positive(chargeDuration)))
                reason = "Charge speed must exceed movement speed, with positive finite duration.";
            else if (role == EnemyRole.FrontalArmored && (!Unit(frontalDamageMultiplier) || frontalDamageMultiplier >= 1 ||
                !Positive(frontalArcDegrees) || frontalArcDegrees > 180))
                reason = "Frontal armor requires a damage multiplier below one and an arc in (0, 180].";
            return reason == null;
        }

        // Validate subsets during authoring; the exact five-species roster belongs to T056.
        public static bool IsValidCatalog(IReadOnlyList<EnemyDefinition> definitions, out string reason)
        {
            reason = null;
            if (definitions == null || definitions.Count == 0) { reason = "Enemy catalog is empty."; return false; }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (EnemyDefinition definition in definitions)
            {
                if (definition == null) { reason = "Enemy definition is missing."; return false; }
                if (!definition.IsValid(out reason)) return false;
                if (!ids.Add(definition.Id)) { reason = "Enemy IDs must be unique (including pool and telemetry keys)."; return false; }
            }
            return true;
        }

        private static bool HasPoolLifecycle(GameObject value)
        {
            foreach (MonoBehaviour component in value.GetComponents<MonoBehaviour>())
                if (component is IPoolLifecycle) return true;
            return false;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Positive(float value) => Finite(value) && value > 0;
        private static bool Nonnegative(float value) => Finite(value) && value >= 0;
        private static bool Unit(float value) => Nonnegative(value) && value <= 1;
    }
}
