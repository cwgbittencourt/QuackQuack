using System;
using System.Collections.Generic;
using QuackQuack.Content.Pickups;
using QuackQuack.Core.Progression;
using QuackQuack.Core.Run;
using QuackQuack.Gameplay.Pickups;
using QuackQuack.Gameplay.Player;
using QuackQuack.Systems.Upgrades;
using UnityEngine;

namespace QuackQuack.Systems.Pickups
{
    // One run-owned driver, no per-view Update, queries, coroutines or allocation in Advance.
    public sealed class PickupAttractionSystem
    {
        private readonly RunState run;
        private readonly PlayerController player;
        private readonly CircleCollider2D body;
        private readonly PickupPolicyDefinition policy;
        private readonly UpgradeSystem upgrades;
        private readonly RelicLoadout relics;
        public float CaptureRadius { get; private set; }
        public float Speed { get; private set; }
        public PickupAttractionSystem(RunState run, PlayerController player, PickupPolicyDefinition policy,
            UpgradeSystem upgrades = null, RelicLoadout relics = null)
        {
            if (run == null || player == null || player.State == null || policy == null || !policy.IsValid(out _))
                throw new ArgumentException("Valid attraction composition is required.");
            this.run = run; this.player = player; this.policy = policy; this.upgrades = upgrades; this.relics = relics;
            body = player.GetComponent<CircleCollider2D>();
        }
        public void Advance(float seconds, IReadOnlyList<ExperiencePickup> experience, IReadOnlyList<HealingPickup> healing)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            bool alive = player != null && player.isActiveAndEnabled && player.State.Health > 0 && !run.Outcome.HasValue;
            float radius = 0; Vector2 target = default;
            if (alive)
            {
                var scale = body.transform.lossyScale;
                if (scale.x <= 0 || scale.y <= 0 || !Mathf.Approximately(scale.x, scale.y) || float.IsInfinity(scale.x) || float.IsNaN(scale.x))
                    throw new InvalidOperationException("Player physics requires positive uniform scale.");
                radius = body.radius * scale.x;
                CaptureRadius = radius * policy.CaptureBodyRadiusMultiplier;
                float water = upgrades != null ? upgrades.ResolvePlayerAttributes(player.Definition, TerrainType.Water, relics).MovementSpeed :
                    relics?.ApplyEffects(player.Definition.WaterSpeed, RelicAttribute.MovementSpeed, TerrainType.Water) ?? player.Definition.WaterSpeed;
                Speed = water * player.EnemySlowMultiplier * policy.AttractionWaterSpeedMultiplier;
                target = body.transform.TransformPoint(body.offset);
            }
            if (experience != null) for (int i = 0; i < experience.Count; i++) if (experience[i] != null) AdvanceView(experience[i], seconds, alive, radius, target);
            if (healing != null) for (int i = 0; i < healing.Count; i++) if (healing[i] != null) AdvanceView(healing[i], seconds, alive, radius, target);
        }
        private void AdvanceView(IPickupAttractionView view, float seconds, bool alive, float bodyRadius, Vector2 target)
        {
            if (view.ViewLeaseId == 0 || (view.ViewState != RewardState.Grounded && view.ViewState != RewardState.Attracted)) return;
            if (!alive) { view.CancelAttraction(); return; }
            if (!run.IsSimulating) return;
            var state = view.Attraction;
            if (state.LeaseId != view.ViewLeaseId) state.Reset(view.ViewLeaseId);
            if (view.ViewState == RewardState.Attracted && !state.Captured) state.Capture();
            var shape = view.PickupCollider;
            if (shape == null || !shape.enabled || !view.PickupTransform.gameObject.activeInHierarchy) return;
            Vector2 center = shape.transform.TransformPoint(shape.offset);
            bool missing = player.State.Health < player.State.MaxHealth;
            var next = state.Advance(center, target, CaptureRadius, Speed, seconds, true, true, view.IsHealing, missing);
            if (state.Phase == AttractionPhase.Discarded) { view.DiscardCapturedHealing(); return; }
            if (state.Captured && view.ViewState == RewardState.Grounded && !view.TryMarkAttracted(player.transform)) return;
            view.Attraction = state;
            if (!state.Captured) return;
            view.PickupTransform.position += (Vector3)(next - center); // no obstacle casts; player physics is unchanged
            var scale = shape.transform.lossyScale;
            float contact = bodyRadius + shape.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
            if ((!view.IsHealing || missing) && (next - target).sqrMagnitude <= contact * contact)
                view.CollectContact(body);
        }
    }
}
