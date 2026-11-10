using System;
using System.Globalization;
using System.Text;
using QuackQuack.Core.Run;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QuackQuack.Presentation.UI
{
    public sealed class ResultsView : MonoBehaviour, ICancelHandler
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text title, summary, combat, build, rewards, saveStatus;
        [SerializeField] private Button retry, restart;
        [SerializeField] private ScrollRect[] scrolls;
        private Action retryAction, restartAction;
        private readonly Selectable[] navigation = new Selectable[5];
        private int navigationCount;
        private bool closing;
        public bool IsVisible => panel != null && panel.activeSelf;
        public string TitleText => title.text;
        public string SummaryText => summary.text;
        public string CombatText => combat.text;
        public string BuildText => build.text;
        public string RewardsText => rewards.text;
        public string SaveText => saveStatus.text;
        public bool CanRetry => retry.interactable;
        public bool CanRestart => restart.interactable;
        public void Bind(Action retrySave, Action playAgain)
        {
            Unbind(); retryAction = retrySave; restartAction = playAgain;
            retry.onClick.AddListener(Retry); restart.onClick.AddListener(Restart);
            closing = false; panel.SetActive(false);
        }
        public void Show(RunResult result, RunProgressReceipt progress, Func<string, string> resolve, bool saved)
        {
            if (result == null || progress == null) throw new ArgumentNullException("Result presentation");
            // Scene teardown may destroy the panel before the director publishes abandonment.
            if (panel == null) return;
            string Name(string id) => Escape(resolve == null ? id : resolve(id));
            closing = false; panel.SetActive(true);
            title.text = result.IsVictory ? "VITÓRIA" : result.Outcome == RunPhase.Abandoned ? "PARTIDA ABANDONADA" : "DERROTA";
            title.color = result.IsVictory ? new Color(1, 0.85f, 0.35f) : result.Outcome == RunPhase.Abandoned ? new Color(0.68f, 0.85f, 0.9f) : new Color(1, 0.5f, 0.48f);
            bool themed=GetComponentInParent<CartoonUiSkin>()!=null;
            if(themed)title.color=result.IsVictory?CartoonUiTheme.Green:result.Outcome==RunPhase.Abandoned?CartoonUiTheme.Teal:CartoonUiTheme.Warning;
            summary.text = "TEMPO  " + Time(result.DurationSeconds) + "     •     CHEFE  " + (result.BossReached ? (string.IsNullOrEmpty(result.BossId) ? "Alcançado" : Name(result.BossId)) : "Não alcançado") + "     •     SEED  " + result.Seed;
            var text = new StringBuilder();
            text.Append("<b>COMBATE</b>\n\nInimigos derrotados  ").Append(result.EnemiesDefeated).Append("\nMaior sequência  ").Append(result.LongestStreak);
            text.Append("\n\n<b>DANO POR ARMA</b>\n");
            if (result.DamageByWeapon.Count == 0) text.Append("Nenhum dano registrado\n");
            foreach (var pair in result.DamageByWeapon) text.Append(Name(pair.Key)).Append("  ·  ").Append(Number(pair.Value)).Append('\n');
            text.Append("\n<b>INIMIGOS</b>\n");
            if (result.DefeatsByEnemy.Count == 0) text.Append("Nenhuma derrota registrada\n");
            foreach (var pair in result.DefeatsByEnemy) text.Append(Name(pair.Key)).Append("  × ").Append(pair.Value).Append('\n');
            combat.text = text.ToString(); text.Clear();
            text.Append("<b>BUILD FINAL</b>\n\n<b>Armas</b>\n");
            if (result.Build.Weapons.Count == 0) text.Append("Nenhuma arma\n");
            foreach (var pair in result.Build.Weapons) text.Append(Name(pair.Key)).Append("  ·  N").Append(pair.Value).Append('\n');
            text.Append("\n<b>Passivas</b>\n"); if (result.Build.Passives.Count == 0) text.Append("Nenhuma passiva\n");
            foreach (var pair in result.Build.Passives) text.Append(Name(pair.Key)).Append("  ·  N").Append(pair.Value).Append('\n');
            text.Append("\n<b>Evoluções</b>\n"); if (result.Build.Evolutions.Count == 0) text.Append("Nenhuma evolução\n");
            foreach (var pair in result.Build.Evolutions) text.Append(Name(pair.Key)).Append(" → ").Append(Name(pair.Value)).Append('\n');
            text.Append("\n<b>Sinergias</b>\n"); if (result.Synergies.Count == 0) text.Append("Nenhuma sinergia\n");
            foreach (string id in result.Synergies) text.Append(Name(id)).Append('\n');
            text.Append("\n<b>Relíquias equipadas</b>\n"); if (result.Build.EquippedRelicIds.Count == 0) text.Append("Nenhuma relíquia\n");
            foreach (string id in result.Build.EquippedRelicIds) text.Append(Name(id)).Append('\n');
            text.Append("\nRerrolagens restantes  ").Append(result.Build.RerollsRemaining).Append("\nBanimentos restantes  ").Append(result.Build.BanishRemaining);
            text.Append("\n\n<b>Histórico de escolhas</b>\n");
            foreach (var choice in result.Build.History) text.Append(Name(choice.ItemId)).Append("  ·  N").Append(choice.Level).Append('\n');
            build.text = text.ToString(); text.Clear();
            text.Append("<b>RECOMPENSAS</b>\n\nMoedas ganhas  ").Append(result.EarnedCoins).Append("\nBônus de vitória  ").Append(result.VictoryBonus);
            if (result.Outcome == RunPhase.Abandoned) text.Append("\nDesconto por abandono  −").Append(result.EarnedCoins - result.Coins);
            text.Append("\nRecebidas nesta partida  ").Append(result.Coins).Append(saved ? "\nSaldo total  " : "\nSaldo após salvar  ").Append(progress.Profile.Coins);
            text.Append("\n\n<b>ITENS DESBLOQUEADOS</b>\n"); if (result.Items.Count == 0) text.Append("Nenhum novo item\n");
            foreach (var pair in result.Items) text.Append(Name(pair.Key)).Append("  × ").Append(pair.Value).Append('\n');
            text.Append("\n<b>RECORDES</b>\n");
            foreach (var pair in progress.Records)
            {
                string previous = pair.Key == "survival-seconds" ? Time(pair.Value.Previous) : Number(pair.Value.Previous);
                double best = Math.Max(pair.Value.Previous, pair.Value.Current);
                text.Append(Name(pair.Key)).Append("\n").Append(previous).Append(" → ").Append(pair.Key == "survival-seconds" ? Time(best) : Number(best));
                if (pair.Value.IsNewRecord) text.Append("  <color=#F5D477>NOVO</color>"); text.Append('\n');
            }
            text.Append("\n<b>DESAFIOS</b>\n"); if (progress.Challenges.Count == 0) text.Append("Nenhum desafio configurado\n");
            foreach (var pair in progress.Challenges)
                text.Append(Name(pair.Key)).Append("\n").Append(pair.Value.Previous).Append(" → ").Append(pair.Value.Current).Append(" / ").Append(pair.Value.Target)
                    .Append(pair.Value.IsCompleted ? "  <color=#8ED4A5>COMPLETO</color>\n" : "\n");
            text.Append("\nPartidas  ").Append(progress.Profile.Runs).Append("  •  Vitórias  ").Append(progress.Profile.Victories);
            rewards.text = themed?text.ToString().Replace("#F5D477","#7C4A08").Replace("#8ED4A5","#245C3F"):text.ToString();
            saveStatus.text = saved ? "Progresso salvo" : "Progresso não salvo. Tente novamente antes de sair.";
            saveStatus.color = saved ? new Color(0.56f, 0.83f, 0.65f) : new Color(1, 0.7f, 0.45f);
            if(themed)saveStatus.color=saved?CartoonUiTheme.Green:CartoonUiTheme.Warning;
            retry.interactable = !saved; restart.interactable = true;
            Canvas.ForceUpdateCanvases(); if (scrolls != null) foreach (var scroll in scrolls) scroll.verticalNormalizedPosition = 1;
            RefreshNavigation(true);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject((saved ? restart : retry).gameObject);
        }
        private void OnEnable()
        {
            // Register after uGUI layout so AutoHide bars cannot leave stale links.
            CanvasUpdateRegistry.IsRebuildingLayout();
            Canvas.willRenderCanvases += RefreshFocus;
        }
        private void OnDisable() => Canvas.willRenderCanvases -= RefreshFocus;
        private void LateUpdate() => RefreshFocus();
        private void RefreshFocus()
        {
            if (!IsVisible || closing) return;
            RefreshNavigation(); MenuNavigation.Repair(navigation, navigationCount);
        }
        private void AddNavigation(Selectable value, ref int count, ref bool changed)
        {
            if (!MenuNavigation.Usable(value)) return;
            if (navigation[count] != value) changed = true;
            navigation[count++] = value;
        }
        private void RefreshNavigation(bool force = false)
        {
            int count = 0; bool changed = force; Selectable firstBar = null, lastBar = null;
            if (scrolls != null) foreach (var scroll in scrolls) {
                var bar = scroll != null ? scroll.verticalScrollbar : null;
                if (!MenuNavigation.Usable(bar)) continue;
                if (firstBar == null) firstBar = bar; lastBar = bar;
                AddNavigation(bar, ref count, ref changed);
            }
            AddNavigation(retry, ref count, ref changed); AddNavigation(restart, ref count, ref changed);
            if (count != navigationCount) changed = true;
            navigationCount = count;
            if (!changed || count == 0) return;
            MenuNavigation.Graph(navigation, count);
            // Scrollbar.OnMove needs null vertical links to retain native up/down scrolling.
            if (scrolls != null) foreach (var scroll in scrolls) {
                var bar = scroll != null ? scroll.verticalScrollbar : null;
                if (!MenuNavigation.Usable(bar)) continue;
                var links = bar.navigation; links.selectOnUp = links.selectOnDown = null; bar.navigation = links;
            }
            if (firstBar != null) foreach (var action in new[] { retry, restart }) {
                if (!MenuNavigation.Usable(action)) continue;
                var links = action.navigation; links.selectOnUp = lastBar; links.selectOnDown = firstBar; action.navigation = links;
            }
        }
        public void OnCancel(BaseEventData data)
        {
            if (!IsVisible || closing) return;
            if (MenuNavigation.Selected is Scrollbar) MenuNavigation.Restore(restart, retry);
            else MenuNavigation.Repair(navigation, navigationCount);
            data.Use();
        }
        public void BeginRestart() { closing = true; retry.interactable = restart.interactable = false; }
        private void Retry() { if (retry.interactable) retryAction?.Invoke(); }
        private void Restart() { if (restart.interactable) restartAction?.Invoke(); }
        public void Unbind() { if (retry != null) retry.onClick.RemoveListener(Retry); if (restart != null) restart.onClick.RemoveListener(Restart); retryAction = restartAction = null; }
        private void OnDestroy() => Unbind();
        private static string Escape(string text) => (text ?? "—").Replace("<", "‹").Replace(">", "›");
        private static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
        private static string Time(double seconds) { int value = (int)Math.Min(int.MaxValue, Math.Max(0, seconds)); return (value / 60).ToString("00") + ":" + (value % 60).ToString("00"); }
    }
}
