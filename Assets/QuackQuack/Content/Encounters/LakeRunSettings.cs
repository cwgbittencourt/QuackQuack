using QuackQuack.Content.Catalogs;
using QuackQuack.Content.Enemies;
using QuackQuack.Content.Pickups;
using QuackQuack.Gameplay.Bosses;
using QuackQuack.Gameplay.Pickups;
using UnityEngine;
namespace QuackQuack.Content.Encounters
{
    [CreateAssetMenu(menuName = "QuackQuack/Lake run settings")]
    public sealed class LakeRunSettings : ScriptableObject
    {
        public LakeEncounterCatalog Catalog;
        public FrogKingSettings BossSettings;
        public GameObject BossPrefab;
        public ExperiencePickup ExperiencePrefab;
        public PickupPolicyDefinition PickupPolicy;
        public int BossExperience;
        public Rect SpawnRegion = new Rect(-11.5f, -6, 23, 12);
        public Rect CameraBounds = new Rect(-12.5f, -7, 25, 14);
        public float MinimumSpawnDistance = 5;
        public int RequestLifetimeTicks = 60;
        public int PickupCapacity = 256;
        public int VictoryBonus = 50;
        public int BossCoins = 50;
        [Range(0, 1)] public float AbandonmentFactor = 0.5f;
        public int CalculateRewardDefeatCapacity()
        {
            if (Catalog == null || !Catalog.IsValid(out _)) throw new System.InvalidOperationException("A valid encounter catalog is required.");
            // Inclusive endpoints add a spare batch per wave. No assumptions about kills/collection.
            long capacity = checked(2L * Catalog.QueueCapacity + 1); // all queued mini requests, plus final boss
            foreach (var wave in Catalog.Waves)
            {
                long batches = checked((long)System.Math.Ceiling((wave.EndSeconds - wave.StartSeconds) / wave.SpawnInterval) + 1);
                long quantity = 0; foreach (int count in wave.Quantities) quantity = checked(quantity + count);
                capacity = checked(capacity + batches * quantity);
            }
            if (capacity <= 0 || capacity > int.MaxValue / 2) throw new System.InvalidOperationException("Unsupported reward ledger capacity.");
            return (int)capacity;
        }
        public bool IsValid => Catalog != null && Catalog.IsValid(out _) && BossSettings != null && BossSettings.IsValid(out _)
            && BossPrefab != null && BossPrefab.GetComponent<FrogKingController>() != null && ExperiencePrefab != null
            && PickupPolicy != null && PickupPolicy.IsValid(out _) && BossExperience > 0
            && Core.Run.SpawnRequest.ValidRegion(SpawnRegion) && Core.Run.SpawnRequest.ValidRegion(CameraBounds) && MinimumSpawnDistance > 0 && !float.IsInfinity(MinimumSpawnDistance)
            && RequestLifetimeTicks > 0 && PickupCapacity > 0 && VictoryBonus >= 0 && BossCoins >= 0
            && AbandonmentFactor >= 0 && AbandonmentFactor <= 1;
    }
}
