#if UNITY_EDITOR
using QuackQuack.Content.Characters;
using QuackQuack.Content.Enemies;
using QuackQuack.Core.Run;
using QuackQuack.Gameplay.Player;
using QuackQuack.Platform.Input;
using QuackQuack.Systems.Combat;
using QuackQuack.Systems.Pooling;
using UnityEngine;

namespace QuackQuack.Gameplay.Bosses
{
    // Editor-only playable fixture, deliberately outside the production run composition.
    [DefaultExecutionOrder(-100)]
    public sealed class FrogKingPracticeArena : MonoBehaviour
    {
        public FrogKingSettings Settings;
        public CharacterDefinition Character;
        public GameObject BossPrefab;
        public PlayerController Player;
        public FrogKingSafeArea[] Refuges;
        private PoolService pools;
        private RunState run;
        private GameplayEventBus events;
        private KeyboardInputAdapter input;
        private FrogKingController boss;
        private void Start()
        {
            run = new RunState(57); run.TryTransition(RunPhase.Playing); run.TryTransition(RunPhase.Boss);
            pools = new PoolService(); events = new GameplayEventBus(); input = new KeyboardInputAdapter();
            Player.Initialize(Character, input, run, events);
            pools.Register("practice/boss", PoolFamily.Enemy, () => Instantiate(BossPrefab, transform).GetComponent<FrogKingController>(), 1);
            boss = pools.Acquire<FrogKingController>("practice/boss");
            boss.transform.position = new Vector2(0, 3);
            boss.InitializeEncounter(run, events, pools, Player, Settings, boss.GetComponentInChildren<LineRenderer>(), boss.GetComponent<AudioSource>(), Refuges);
        }
        private void Update() { run?.Advance(Time.deltaTime); }
        private void OnGUI()
        {
            if (boss == null) return;
            GUI.Box(new Rect(12, 12, 510, 110), "Treino Sapo-Rei — WASD / setas para esquivar");
            GUI.Label(new Rect(24, 38, 480, 24), $"Pato: {Player.State.Health:0}   Sapo-Rei: {boss.Health:0}   Fase: {boss.Phase}   {run.Phase}");
            if (GUI.Button(new Rect(24, 72, 220, 32), "Aplicar 10% de dano ao chefe")) boss.TakeDamage(Settings.Health * 0.1f, Player.GetInstanceID(), 0);
            if (GUI.Button(new Rect(254, 72, 250, 32), "Abandonar treino")) run.TryTransition(RunPhase.Abandoned);
        }
        private void OnDestroy() { input?.Dispose(); pools?.Dispose(); events?.Clear(); }
    }
}
#endif
