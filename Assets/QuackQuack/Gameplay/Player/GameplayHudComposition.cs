using System.Collections;
using QuackQuack.Core.Shared;
using QuackQuack.Presentation.UI;
using QuackQuack.Systems.Run;
using UnityEngine;

namespace QuackQuack.Gameplay.Player
{
    // Serialized composition keeps Presentation independent of actors and run services.
    public sealed class GameplayHudComposition : MonoBehaviour
    {
        [SerializeField] private GameplayHud hud;
        [SerializeField] private ContextualHintsView hints;
        [SerializeField] private SpecialEnemyBarsView specials;
        [SerializeField] private RectTransform specialArea;
        [SerializeField] private PlayerController player;
        [SerializeField] private RunDirector director;
        [SerializeField] private Camera worldCamera;
        private SpecialEnemyRegistry empty;
        private bool ready;
        public bool Ready => ready;
        private IEnumerator Start()
        {
            while (GameBootstrap.Instance == null || player == null || player.State == null || (director != null && !director.Ready)) yield return null;
            ready = true;
            if (isActiveAndEnabled) Bind();
        }
        private void Bind()
        {
            hud.Bind(player.Snapshot);
            hints.Bind(GameBootstrap.Instance.Run, () => player.HintInputSource);
            if (director == null && empty == null) empty = new SpecialEnemyRegistry(1);
            specials.Bind(director != null ? director.SpecialEnemies : empty, worldCamera, specialArea);
        }
        private void OnEnable() { if (ready && GameBootstrap.Instance != null) Bind(); }
        private void OnDisable()
        {
            if (hud != null) hud.Unbind();
            if (hints != null) hints.Unbind();
            if (specials != null) specials.Unbind();
        }
    }
}
