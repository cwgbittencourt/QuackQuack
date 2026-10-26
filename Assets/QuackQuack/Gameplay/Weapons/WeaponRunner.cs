using System;
using System.Collections.Generic;
using QuackQuack.Content.Weapons;
using QuackQuack.Core.Combat;
using QuackQuack.Core.Progression;
using QuackQuack.Core.Run;
using QuackQuack.Gameplay.Player;
using QuackQuack.Systems.Combat;
using QuackQuack.Systems.Pooling;
using UnityEngine;

namespace QuackQuack.Gameplay.Weapons
{
    public sealed class WeaponRunner : MonoBehaviour
    {
        private interface IAttackPattern
        { void Fire(WeaponRunner runner, WeaponDefinition definition, WeaponLevel stats, Vector2 direction, QuackTarget target); }
        private sealed class ProjectilePattern : IAttackPattern
        {
            private readonly ProjectileMotion motion;
            public ProjectilePattern(ProjectileMotion motion) { this.motion = motion; }
            public void Fire(WeaponRunner r, WeaponDefinition d, WeaponLevel s, Vector2 direction, QuackTarget target)
            {
                int count = Mathf.Min(s.ProjectileCount, r.capacity - r.active.Count);
                for (int i = 0; i < count; i++)
                {
                    float angle = count <= 1 ? 0 : d.ConeAngle * (i / (float)(count - 1) - 0.5f);
                    Vector2 shotDirection = Quaternion.Euler(0, 0, angle) * direction;
                    r.Launch(d, s, motion, shotDirection, target);
                }
            }
        }
        private sealed class AreaPattern : IAttackPattern
        {
            private readonly bool cone;
            public AreaPattern(bool cone) { this.cone = cone; }
            public void Fire(WeaponRunner r, WeaponDefinition d, WeaponLevel s, Vector2 direction, QuackTarget target) =>
                r.AreaHit(d, s, r.owner.position, direction, cone, null);
        }
        private static readonly IAttackPattern[] patterns =
        {
            new ProjectilePattern(ProjectileMotion.Returning), new ProjectilePattern(ProjectileMotion.Trap),
            new AreaPattern(true), new AreaPattern(false), new ProjectilePattern(ProjectileMotion.Homing),
            new ProjectilePattern(ProjectileMotion.Straight)
        };
        private struct Slot { public WeaponDefinition Definition; public int Level; public double Next; }
        private readonly Slot[] slots = new Slot[BuildState.SlotLimit];
        private readonly Dictionary<string, WeaponDefinition> catalog = new Dictionary<string, WeaponDefinition>(StringComparer.Ordinal);
        private readonly Collider2D[] overlaps = new Collider2D[128];
        private readonly RaycastHit2D[] contacts = new RaycastHit2D[128];
        private readonly int[] areaHits = new int[128];
        private List<WeaponProjectile> active;
        private PoolService pools;
        private RunState run;
        private BuildState build;
        private GameplayEventBus events;
        private Transform owner;
        private Func<TerrainType> terrain;
        private string poolKey;
        private int capacity, historyVersion = -1, areaHitCount;
        private long tick, correlation;
        private double clock;
        private bool initialized;
        public bool AutomaticTick { get; set; } = true;
        public Vector2 Facing { get; set; } = Vector2.right;
        public int ActiveProjectileCount => active == null ? 0 : active.Count;
        private static ContactFilter2D TargetFilter => new ContactFilter2D { useTriggers = true, useLayerMask = true, layerMask = 1 << 9 };
        private static ContactFilter2D TravelFilter => new ContactFilter2D { useTriggers = true, useLayerMask = true, layerMask = (1 << 8) | (1 << 9) };

        public static void RegisterProjectilePool(PoolService pools, string key, int capacity, Transform parent = null)
        {
            pools.Register(key, PoolFamily.Projectile, () =>
            {
                var instance = new GameObject("Weapon projectile");
                instance.SetActive(false);
                if (parent != null) instance.transform.SetParent(parent, false);
                return instance.AddComponent<WeaponProjectile>();
            }, capacity);
        }

        public void Initialize(RunState run, BuildState build, PoolService pools, GameplayEventBus events,
            Transform owner, Func<TerrainType> terrain, IEnumerable<WeaponDefinition> definitions, string poolKey, int capacity)
        {
            if (initialized) throw new InvalidOperationException("Weapon runner is already initialized.");
            if (run == null || build == null || pools == null || events == null || owner == null || terrain == null || definitions == null)
                throw new ArgumentNullException("Weapon runner services");
            if (capacity <= 0 || string.IsNullOrWhiteSpace(poolKey)) throw new ArgumentException("A bounded projectile pool is required.");
            foreach (var definition in definitions)
            {
                if (definition == null || !definition.IsValid(out _)) throw new ArgumentException("Invalid weapon definition.");
                catalog.Add(definition.Id, definition);
            }
            this.run = run; this.build = build; this.pools = pools; this.events = events; this.owner = owner;
            this.terrain = terrain; this.poolKey = poolKey; this.capacity = capacity;
            active = new List<WeaponProjectile>(capacity);
            initialized = true;
        }

        private void Update() { if (AutomaticTick) Advance(Time.deltaTime); }

        public void Advance(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (!initialized || !isActiveAndEnabled) return;
            if (owner == null || run.Outcome.HasValue || run.Phase == RunPhase.Results) { ReleaseAll(); return; }
            if (!run.IsSimulating) return;
            clock += seconds; tick++;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (i >= active.Count) continue;
                var projectile = active[i];
                if (!pools.IsActive(projectile, PoolFamily.Projectile) || Step(projectile, seconds))
                {
                    pools.Release(projectile);
                    if (i < active.Count && active[i] == projectile) active.RemoveAt(i);
                }
                if (!isActiveAndEnabled || !run.IsSimulating)
                { if (run.Outcome.HasValue) ReleaseAll(); return; }
            }
            RefreshSlots();
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot.Definition == null || clock < slot.Next) continue;
                var stats = slot.Definition.GetLevel(slot.Level, terrain());
                var target = Nearest(owner.position, stats.Range);
                Vector2 direction = Facing.sqrMagnitude > 0 && Finite(Facing) ? Facing.normalized : Vector2.right;
                if (slot.Definition.TargetMode == WeaponTargetMode.NearestEnemy)
                {
                    if (target == null) continue;
                    direction = ((Vector2)target.transform.position - (Vector2)owner.position).normalized;
                }
                patterns[(int)slot.Definition.Behavior].Fire(this, slot.Definition, stats, direction, target);
                slot.Next = clock + stats.Cooldown; slots[i] = slot;
                if (!isActiveAndEnabled || !run.IsSimulating)
                { if (run.Outcome.HasValue) ReleaseAll(); return; }
            }
        }

        private void RefreshSlots()
        {
            if (historyVersion == build.History.Count) return;
            historyVersion = build.History.Count;
            for (int i = 0; i < slots.Length; i++)
                if (slots[i].Definition != null && !build.Weapons.ContainsKey(slots[i].Definition.Id)) slots[i] = default;
            foreach (var entry in build.Weapons)
            {
                if (!catalog.TryGetValue(entry.Key, out var definition)) continue;
                int slot = -1;
                for (int i = 0; i < slots.Length; i++) if (slots[i].Definition == definition) { slot = i; break; }
                if (slot < 0) for (int i = 0; i < slots.Length; i++) if (slots[i].Definition == null) { slot = i; break; }
                if (slot < 0) continue;
                slots[slot].Definition = definition; slots[slot].Level = entry.Value;
            }
        }

        private void Launch(WeaponDefinition definition, WeaponLevel stats, ProjectileMotion motion, Vector2 direction, QuackTarget target)
        {
            if (active.Count >= capacity) return;
            var projectile = pools.Acquire<WeaponProjectile>(poolKey);
            if (projectile == null) return;
            projectile.Begin(definition, stats, motion, owner.position, direction, target);
            active.Add(projectile);
        }

        private bool Step(WeaponProjectile p, float seconds)
        {
            if (p.Definition == null) return true;
            float half = p.Definition.ProjectileLifetime * 0.5f;
            if (p.Motion == ProjectileMotion.Returning && p.Age < half && seconds > half - p.Age)
            {
                float outbound = half - p.Age;
                if (StepSegment(p, outbound)) return true;
                return StepSegment(p, seconds - outbound);
            }
            return StepSegment(p, seconds);
        }

        private bool StepSegment(WeaponProjectile p, float seconds)
        {
            if (p.Definition == null) return true;
            float step = Mathf.Min(seconds, p.Definition.ProjectileLifetime - p.Age);
            Vector2 from = p.transform.position;
            if (p.Motion == ProjectileMotion.Returning && p.Age >= p.Definition.ProjectileLifetime * 0.5f)
                p.Direction = ((Vector2)owner.position - from).normalized;
            if (p.Motion == ProjectileMotion.Homing)
            {
                if (!ValidTarget(p.Target)) p.Target = Nearest(from, p.Stats.Range);
                if (p.Target != null)
                {
                    Vector2 desired = (Vector2)p.Target.transform.position - from;
                    float angle = Vector2.SignedAngle(p.Direction, desired);
                    float turn = Mathf.Clamp(angle, -p.Definition.TurnSpeed * step, p.Definition.TurnSpeed * step);
                    p.Direction = Quaternion.Euler(0, 0, turn) * p.Direction;
                }
            }
            Vector2 to = p.Motion == ProjectileMotion.Trap ? from :
                p.Motion == ProjectileMotion.Returning && p.Age >= p.Definition.ProjectileLifetime * 0.5f ?
                Vector2.MoveTowards(from, owner.position, p.Definition.ProjectileSpeed * step) :
                from + p.Direction * (p.Definition.ProjectileSpeed * step);
            p.Age += step;
            if (p.Motion == ProjectileMotion.Trap)
            {
                var target = Nearest(from, Mathf.Max(p.Stats.AreaRadius, p.Definition.HitRadius));
                if (target != null) { AreaHit(p.Definition, p.Stats, from, p.Direction, false, p); return true; }
            }
            else
            {
                Vector2 delta = to - from;
                int count = Physics2D.CircleCast(from, p.Definition.HitRadius, delta.normalized, TravelFilter, contacts, delta.magnitude);
                // Stable nearest-first order, so an obstacle blocks targets behind it.
                for (int i = 1; i < count; i++)
                {
                    var value = contacts[i]; int j = i - 1;
                    while (j >= 0 && contacts[j].distance > value.distance) { contacts[j + 1] = contacts[j]; j--; }
                    contacts[j + 1] = value;
                }
                for (int i = 0; i < count; i++)
                {
                    var contact = contacts[i];
                    if (contact.collider.gameObject.layer == 8) { p.transform.position = contact.centroid; return true; }
                    var target = contact.collider.GetComponentInParent<QuackTarget>();
                    if (!ValidTarget(target)) continue;
                    if (p.Motion == ProjectileMotion.Homing)
                    { AreaHit(p.Definition, p.Stats, contact.point, p.Direction, false, p); return true; }
                    if (p.MarkHit(target.GetInstanceID())) Hit(p.Definition, p.Stats.Damage, target);
                    if (!pools.IsActive(p, PoolFamily.Projectile) || !run.IsSimulating) return true;
                }
            }
            p.transform.position = to;
            return p.Age >= p.Definition.ProjectileLifetime;
        }

        private QuackTarget Nearest(Vector2 position, float range)
        {
            int count = Physics2D.OverlapCircle(position, range, TargetFilter, overlaps);
            QuackTarget nearest = null; float distance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var target = overlaps[i].GetComponentInParent<QuackTarget>();
                if (!ValidTarget(target)) continue;
                float candidate = ((Vector2)target.transform.position - position).sqrMagnitude;
                if (candidate < distance || (candidate == distance && (nearest == null || target.GetInstanceID() < nearest.GetInstanceID())))
                { nearest = target; distance = candidate; }
            }
            return nearest;
        }

        private void AreaHit(WeaponDefinition definition, WeaponLevel stats, Vector2 position, Vector2 direction, bool cone, WeaponProjectile projectile)
        {
            float radius = cone ? stats.Range : Mathf.Max(stats.AreaRadius, definition.HitRadius);
            int count = Physics2D.OverlapCircle(position, radius, TargetFilter, overlaps);
            areaHitCount = 0;
            for (int i = 0; i < count; i++)
            {
                var target = overlaps[i].GetComponentInParent<QuackTarget>();
                if (!ValidTarget(target)) continue;
                Vector2 delta = (Vector2)target.transform.position - position;
                if (cone && Vector2.Angle(direction, delta) > definition.ConeAngle * 0.5f) continue;
                if (Physics2D.Linecast(position, target.transform.position, 1 << 8).collider != null) continue;
                int id = target.GetInstanceID(); bool seen = false;
                for (int j = 0; j < areaHitCount; j++) if (areaHits[j] == id) { seen = true; break; }
                if (seen || (projectile != null && !projectile.MarkHit(id))) continue;
                areaHits[areaHitCount++] = id;
                Hit(definition, stats.Damage, target);
                if (!isActiveAndEnabled || !run.IsSimulating) return;
            }
        }

        private void Hit(WeaponDefinition definition, float damage, QuackTarget target)
        {
            if (!ValidTarget(target)) return;
            float applied = Mathf.Min(damage, target.Health);
            target.Health -= applied;
            bool defeated = target.Health <= 0;
            long id = ++correlation;
            int targetId = target.GetInstanceID();
            Vector2 position = target.transform.position;
            events.Publish(new CombatEvent(tick, id, definition.GetInstanceID(), targetId, position, applied, CombatEventType.DamageApplied));
            if (defeated)
                events.Publish(new CombatEvent(tick, id, definition.GetInstanceID(), targetId, position, 0, CombatEventType.EntityDefeated));
        }
        private static bool ValidTarget(QuackTarget target) => target != null && target.isActiveAndEnabled && !target.IsProjectile && target.Health > 0;
        private static bool Finite(Vector2 v) => !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsInfinity(v.x) && !float.IsInfinity(v.y);
        private void ReleaseAll()
        {
            if (active == null) return;
            foreach (var projectile in active) pools.Release(projectile);
            active.Clear();
        }
        private void OnDisable() { ReleaseAll(); }
        private void OnDestroy() { ReleaseAll(); }
    }
}
