using QuackQuack.Content.Catalogs;
using QuackQuack.Content.Enemies;
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
        public Rect SpawnRegion = new Rect(-11.5f, -6, 23, 12);
        public Rect CameraBounds = new Rect(-12.5f, -7, 25, 14);
        public float MinimumSpawnDistance = 5;
        public int RequestLifetimeTicks = 60;
        public int PickupCapacity = 256;
        public int VictoryBonus = 50;
        public int BossCoins = 50;
        [Range(0, 1)] public float AbandonmentFactor = 0.5f;
        public bool IsValid => Catalog != null && Catalog.IsValid(out _) && BossSettings != null && BossSettings.IsValid(out _)
            && BossPrefab != null && BossPrefab.GetComponent<FrogKingController>() != null && ExperiencePrefab != null
            && Core.Run.SpawnRequest.ValidRegion(SpawnRegion) && Core.Run.SpawnRequest.ValidRegion(CameraBounds) && MinimumSpawnDistance > 0 && !float.IsInfinity(MinimumSpawnDistance)
            && RequestLifetimeTicks > 0 && PickupCapacity > 0 && VictoryBonus >= 0 && BossCoins >= 0
            && AbandonmentFactor >= 0 && AbandonmentFactor <= 1;
    }
}
