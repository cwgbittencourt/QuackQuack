using System;
using System.Collections;
using System.Collections.Generic;
using QuackQuack.Content.Encounters;
using QuackQuack.Core.Run;
using QuackQuack.Presentation.UI;
using QuackQuack.Systems.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace QuackQuack.Systems.Run
{
    public sealed class ResultsBridge : MonoBehaviour
    {
        [SerializeField] private ResultsView view;
        [SerializeField] private ResultsProgressionDefinition progression;
        private RunDirector director;
        private ResultChallengeRule[] rules;
        private readonly Dictionary<string, string> names = new Dictionary<string, string>(StringComparer.Ordinal);
        private RunResult result;
        private RunProgressCommitResult committed;
        private bool restarting;
        public ResultsView View => view;
        public RunProgressCommitResult Committed => committed;
        private IEnumerator Start()
        {
            for (int i = 0; i < 240; i++)
            { director = GetComponent<RunDirector>(); if (director != null && director.Ready) break; yield return null; }
            if (director == null || !director.Ready) yield break;
            rules = progression.CreateRules(); CacheNames(); view.Bind(Retry, Restart);
            director.ResultAvailable += Show;
            if (director.Result != null) Show(director.Result);
        }
        private void CacheNames()
        {
            names["quack"] = "Quack"; names["sapo-rei"] = "Sapo-Rei";
            names["survival-seconds"] = "Tempo sobrevivido"; names["enemies-defeated"] = "Inimigos derrotados";
            names["longest-streak"] = "Maior sequência"; names["total-damage"] = "Dano total";
            foreach (var d in director.BuildCatalog.GetAllWeapons()) names[d.Id] = d.DisplayName;
            foreach (var d in director.BuildCatalog.Passives) names[d.Id] = d.DisplayName;
            foreach (var d in director.BuildCatalog.Evolutions) names[d.Id] = d.BaseWeapon.DisplayName + " + " + d.RequiredPassive.DisplayName;
            foreach (var d in director.RunSettings.Catalog.GetAllEnemies()) names[d.Id] = d.DisplayName;
            if (director.RelicCatalog != null) foreach (var d in director.RelicCatalog.Relics) names[d.Id] = d.DisplayName;
            foreach (var d in progression.Challenges) names[d.Id] = d.Title;
        }
        private string Name(string id) => id != null && names.TryGetValue(id, out var value) ? value : id;
        private void Show(RunResult value)
        {
            if (restarting) return;
            if (!ReferenceEquals(result, value)) { result = value; committed = director.CommitProgress(rules); }
            view.Show(result, committed.Receipt, Name, committed.Save.Status != SaveStatus.Failed);
            var state = GameBootstrap.Instance?.Run;
            if (state != null && state.Outcome.HasValue && state.Phase != RunPhase.Results) state.TryTransition(RunPhase.Results);
        }
        public void Retry()
        {
            if (result == null || restarting) return;
            committed = director.CommitProgress(rules); Show(result);
        }
        public void Restart()
        {
            if (restarting || result == null) return;
            restarting = true; view.BeginRestart(); StartCoroutine(RestartRun());
        }
        private IEnumerator RestartRun()
        {
            if (GameBootstrap.Instance != null) Destroy(GameBootstrap.Instance.gameObject);
            yield return null; // Release the singleton before a new bootstrap's Awake.
            SceneManager.LoadScene("RunBootstrap");
        }
        private void OnDestroy()
        {
            if (director != null) director.ResultAvailable -= Show;
            if (view != null) view.Unbind();
        }
    }
}
