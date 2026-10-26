using System.Collections;
using System.Collections.Generic;
using QuackQuack.Content.Catalogs;
using QuackQuack.Core.Progression;
using QuackQuack.Gameplay.Pickups;
using QuackQuack.Gameplay.Player;
using QuackQuack.Gameplay.Weapons;
using QuackQuack.Platform.Input;
using QuackQuack.Presentation.UI;
using QuackQuack.Systems.Pooling;
using QuackQuack.Systems.Run;
using QuackQuack.Systems.Upgrades;
using UnityEngine;

namespace QuackQuack.Gameplay.Upgrades
{
    public sealed class LakeBuildProgression : MonoBehaviour
    {
        [SerializeField] private PrototypeBuildCatalog catalog;
        [SerializeField] private UpgradeSelectionView view;
        [SerializeField] private PlayerController player;
        [SerializeField] private ExperiencePickup experiencePrefab;
        [SerializeField] private Transform[] experienceSpawns;
        [SerializeField] private int[] experienceThresholds;
        [SerializeField] private int experiencePerPickup, projectileCapacity;
        [SerializeField] private ExperiencePickup rareRewardPrefab;
        [SerializeField] private Transform rareRewardSpawn;
        [SerializeField] private int rareRewardExperience;
        private ExperiencePickup rarePickup;
        private ExperiencePickup[] pickups;
        private WeaponRunner runner;
        private GameBootstrap bootstrap;
        public UpgradeSystem Upgrades { get; private set; }
        public UpgradeSelectionBridge Bridge { get; private set; }
        public bool Ready { get; private set; }

        private IEnumerator Start()
        {
            bootstrap = GameBootstrap.Instance;
            if (player == null) player = Object.FindAnyObjectByType<PlayerController>();
            for (int i = 0; i < 120 && player != null && player.State == null; i++) yield return null;
            if (bootstrap == null || player == null || player.State == null) { enabled = false; yield break; }
            var build = new BuildState(); build.TryAddWeapon(catalog.Weapons[0].Id);
            Upgrades = catalog.CreateUpgradeSystem(bootstrap.Run.Seed, build);
            var choices = new List<string>();
            foreach (var weapon in catalog.Weapons) choices.Add(weapon.Id);
            foreach (var passive in catalog.Passives) choices.Add(passive.Id);
            foreach (var evolution in catalog.Evolutions) choices.Add(evolution.Id);
            Upgrades.InitializeProgression(bootstrap.Run, experienceThresholds, choices.ToArray());
            UpgradeUiEventSystem.Ensure(transform);
            Bridge = gameObject.AddComponent<UpgradeSelectionBridge>(); Bridge.Initialize(catalog, Upgrades, bootstrap.Run, view);
            string key = "lake-build-" + GetInstanceID();
            WeaponRunner.RegisterProjectilePool(bootstrap.Pools, key, projectileCapacity, transform);
            runner = gameObject.AddComponent<WeaponRunner>();
            runner.Initialize(bootstrap.Run, build, bootstrap.Pools, bootstrap.Events, player.transform,
                () => player.State.Terrain, catalog.GetAllWeapons(), key, projectileCapacity);
            string xpKey = key + "-xp";
            bootstrap.Pools.Register(xpKey, PoolFamily.Pickup, () => Instantiate(experiencePrefab, transform), experienceSpawns.Length);
            pickups = new ExperiencePickup[experienceSpawns.Length];
            for (int i = 0; i < pickups.Length; i++)
                pickups[i] = ExperiencePickup.Spawn(bootstrap.Pools, xpKey, Upgrades, experiencePerPickup, experienceSpawns[i].position);
            if (rareRewardPrefab != null && rareRewardSpawn != null)
            {
                string rareKey = key + "-rare";
                bootstrap.Pools.Register(rareKey, PoolFamily.Pickup, () => Instantiate(rareRewardPrefab, transform), 1);
                rarePickup = ExperiencePickup.SpawnRare(bootstrap.Pools, rareKey, Upgrades, rareRewardExperience,
                    rareRewardSpawn.position, bootstrap.Events);
            }
            Ready = true;
        }
        private void Update()
        {
            if (!Ready || player == null) return;
            Vector2 velocity = player.GetComponent<Rigidbody2D>().linearVelocity;
            if (velocity.sqrMagnitude > 0.01f) runner.Facing = velocity.normalized;
        }
        private void OnDestroy()
        {
            Ready = false;
            if (view != null) view.Unbind();
            Upgrades?.Dispose();
            if (runner != null) runner.enabled = false;
            if (bootstrap != null && pickups != null) foreach (var pickup in pickups) bootstrap.Pools.Release(pickup);
            if (bootstrap != null) bootstrap.Pools.Release(rarePickup);
        }
    }
}
