using System;
using QuackQuack.Core.Run;
using QuackQuack.Systems.Combat;
using QuackQuack.Systems.Pooling;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QuackQuack.Systems.Run
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private int seed = 42;
        [SerializeField] private bool loadLake = true;
        [SerializeField] private string lakeScene = "Lake";
        public bool ExternalClock { get; set; }
        public static GameBootstrap Instance { get; private set; }
        public RunState Run { get; private set; }
        public System.Random Random { get; private set; }
        public GameplayEventBus Events { get; private set; }
        public PoolService Pools { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Events = new GameplayEventBus();
            Pools = new PoolService();
            Run = new RunState(seed);
            Random = new System.Random(seed);
        }

        private void Start()
        {
            if (Instance != this) return;
            if (loadLake && SceneManager.GetActiveScene().name != lakeScene) SceneManager.LoadScene(lakeScene);
            if (lakeScene != "LakeRun") Run.TryTransition(RunPhase.Playing);
        }

        private void Update() { if (Instance == this && !ExternalClock) Run.Advance(Time.deltaTime); }

        private void OnDestroy()
        {
            if (Instance != this) return;
            Pools.Dispose();
            Events.Clear();
            Instance = null;
        }
    }
}
