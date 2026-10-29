using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QuackQuack.Core.Run;
using QuackQuack.Gameplay.Upgrades;
using QuackQuack.Platform.Input;
using QuackQuack.Presentation.UI;
using QuackQuack.Systems.Run;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace QuackQuack.Tests.PlayMode
{
    [Category("IntegratedUiRuntime")]
    public class IntegratedUiLayoutTests
    {
        private static readonly Vector2Int[] Viewports = {
            new Vector2Int(1920,1080), new Vector2Int(2160,1080),
            new Vector2Int(2340,1080), new Vector2Int(1440,1080)
        };
        private Touchscreen touch, previousTouch;
        private Camera camera;
        private RenderTexture target;
        private RunDirector director;
        private Func<string> previousPath;
        private string directory;
        private Rect safe;
        private Vector2 viewport;

        [UnitySetUp] public IEnumerator SetUp()
        {
            if (GameBootstrap.Instance != null) Object.Destroy(GameBootstrap.Instance.gameObject);
            yield return null;
            directory = Path.Combine(Application.temporaryCachePath, "QuackQuack-T075Ui-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            previousPath = RunDirector.SavePathProvider;
            RunDirector.SavePathProvider = () => Path.Combine(directory, "save.json");
            previousTouch = Touchscreen.current;
            touch = InputSystem.AddDevice<Touchscreen>();
            yield return SceneManager.LoadSceneAsync("RunBootstrap");
            double deadline = Time.realtimeSinceStartupAsDouble + 30;
            do { director = Object.FindAnyObjectByType<RunDirector>(); if (director != null && director.Ready) break; yield return null; }
            while (Time.realtimeSinceStartupAsDouble < deadline);
            Assert.That(director != null && director.Ready, Is.True);
            director.AutomaticTick = false;
            director.Scheduler.enabled = false;
            foreach (var runner in Object.FindObjectsByType<QuackQuack.Gameplay.Weapons.WeaponRunner>(FindObjectsSortMode.None)) runner.enabled = false;
            director.TogglePause();
            camera = new GameObject("Integrated UI viewport").AddComponent<Camera>();
            camera.enabled = false; camera.orthographic = true; camera.transform.position = new Vector3(0,0,-20);
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10; }
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (director != null && director.Ready) director.TryAbandon();
            if (GameBootstrap.Instance != null) Object.Destroy(GameBootstrap.Instance.gameObject);
            yield return null;
            var scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("After integrated UI"));
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            if (touch != null && touch.added) InputSystem.RemoveDevice(touch);
            if (previousTouch != null && previousTouch.added) previousTouch.MakeCurrent();
            if (camera != null) { camera.targetTexture = null; Object.Destroy(camera.gameObject); }
            if (target != null) { target.Release(); Object.Destroy(target); }
            RunDirector.SavePathProvider = previousPath;
            string resolved = Path.GetFullPath(directory), prefix = Path.GetFullPath(Application.temporaryCachePath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            Assert.That(resolved.StartsWith(prefix, StringComparison.OrdinalIgnoreCase), Is.True);
            Assert.That(Path.GetFileName(resolved), Does.StartWith("QuackQuack-T075Ui-"));
            Directory.Delete(resolved, true);
        }

        [UnityTest] public IEnumerator ProductionHudTouchAndModalsFitAndRemainClickable([ValueSource(nameof(Viewports))] Vector2Int size)
        {
            viewport = new Vector2(size.x,size.y);
            target = new RenderTexture(size.x,size.y,24); target.Create(); camera.targetTexture = target; camera.aspect = (float)size.x / size.y;
            var relics = Object.FindAnyObjectByType<RelicLoadoutView>();
            var open = Field<Button>(relics,"open");
            var mobile = Object.FindAnyObjectByType<MobileInputAdapter>();
            Assert.That(mobile, Is.Not.Null);
            foreach (var button in mobile.GetComponentsInChildren<MobileActionButton>(true))
                if (button.Action == MobileAction.Pause) Assert.That(button.gameObject.activeSelf, Is.False, "Use the run's single PAUSAR/CONTINUAR button.");
            var pauseButton = Field<Button>(director,"pauseButton");
            Assert.That(pauseButton.GetComponentInChildren<Text>().text, Is.EqualTo("CONTINUAR"));
            pauseButton.onClick.Invoke(); yield return null;
            Assert.That(GameBootstrap.Instance.Run.Phase, Is.EqualTo(RunPhase.Playing));
            Assert.That(pauseButton.GetComponentInChildren<Text>().text, Is.EqualTo("PAUSAR"));
            pauseButton.onClick.Invoke(); yield return null;
            Assert.That(GameBootstrap.Instance.Run.Phase, Is.EqualTo(RunPhase.Paused));
            Assert.That(pauseButton.GetComponentInChildren<Text>().text, Is.EqualTo("CONTINUAR"));
            foreach (bool right in new[] { false,true })
            {
                yield return ApplyInsets(right);
                var hud = Object.FindAnyObjectByType<GameplayHud>();
                var rects = new List<RectTransform> {
                    Field<Text>(hud,"timeLabel").transform.parent as RectTransform,
                    Field<Text>(hud,"healthLabel").transform.parent as RectTransform,
                    Field<Text>(hud,"statusLabel").transform.parent as RectTransform,
                    open.GetComponent<RectTransform>(),
                    Field<Button>(director,"pauseButton").GetComponent<RectTransform>(),
                    Field<Button>(director,"abandonButton").GetComponent<RectTransform>()
                };
                foreach (string name in new[] { "Joystick","QUACK" })
                    foreach (var rect in mobile.GetComponentsInChildren<RectTransform>()) if (rect.name == name) rects.Add(rect);
                Assert.That(rects.Count, Is.EqualTo(8));
                foreach (var rect in rects) AssertInside(rect);
                for (int i=0;i<rects.Count;i++) for (int j=i+1;j<rects.Count;j++)
                    Assert.That(ScreenRect(rects[i]).Overlaps(ScreenRect(rects[j])), Is.False, rects[i].name + " overlaps " + rects[j].name);
                AssertClickable(open.transform);
                AssertClickable(Field<Button>(director,"abandonButton").transform);
                Capture(size,"gameplay-"+right);
            }
            open.onClick.Invoke(); yield return null;
            Assert.That(relics.IsVisible, Is.True);
            foreach (bool right in new[] { false,true }) { yield return ApplyInsets(right); AssertModal(Field<GameObject>(relics,"panel")); Capture(size,"relics-"+right); }
            Field<Button>(relics,"close").onClick.Invoke(); yield return null;
            Assert.That(GameBootstrap.Instance.Run.Phase, Is.EqualTo(RunPhase.Paused));
            director.TogglePause();
            Assert.That(Object.FindAnyObjectByType<LakeBuildProgression>().Upgrades.TryCollectExperience(10), Is.True);
            yield return null; yield return null;
            var choices = Object.FindAnyObjectByType<UpgradeSelectionView>();
            Assert.That(choices.IsVisible, Is.True);
            foreach (bool right in new[] { false,true }) { yield return ApplyInsets(right); AssertModal(Field<GameObject>(choices,"panel")); Capture(size,"cards-"+right); }
            Field<Button>(choices,"skip").onClick.Invoke(); yield return null;
            Assert.That(director.TryAbandon(), Is.True); yield return null;
            var results = Object.FindAnyObjectByType<ResultsView>();
            foreach (bool right in new[] { false,true }) { yield return ApplyInsets(right); AssertModal(Field<GameObject>(results,"panel")); Capture(size,"results-"+right); }
        }

        private IEnumerator ApplyInsets(bool right)
        {
            safe = new Rect(right ? 24 : 72,32,viewport.x-96,viewport.y-80);
            foreach (var area in Object.FindObjectsByType<SafeAreaController>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                area.SetViewportProvider(() => viewport, () => safe);
            yield return null; yield return null; Canvas.ForceUpdateCanvases();
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
                Assert.That(canvas.pixelRect.size, Is.EqualTo(viewport), "Canvas must use the actual test viewport.");
        }
        private Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            var min = RectTransformUtility.WorldToScreenPoint(camera,corners[0]);
            var max = RectTransformUtility.WorldToScreenPoint(camera,corners[2]);
            return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }
        private void AssertInside(RectTransform rect)
        {
            Assert.That(rect.gameObject.activeInHierarchy, Is.True, rect.name);
            var r = ScreenRect(rect);
            Assert.That(r.xMin, Is.GreaterThanOrEqualTo(safe.xMin-0.5f), rect.name);
            Assert.That(r.yMin, Is.GreaterThanOrEqualTo(safe.yMin-0.5f), rect.name);
            Assert.That(r.xMax, Is.LessThanOrEqualTo(safe.xMax+0.5f), rect.name);
            Assert.That(r.yMax, Is.LessThanOrEqualTo(safe.yMax+0.5f), rect.name);
        }
        private void AssertClickable(Transform item)
        {
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position=ScreenRect(item.GetComponent<RectTransform>()).center },hits);
            Assert.That(hits.Count, Is.GreaterThan(0),item.name);
            Assert.That(hits[0].gameObject.transform == item || hits[0].gameObject.transform.IsChildOf(item), Is.True, item.name + " is covered by " + hits[0].gameObject.name);
        }
        private void AssertModal(GameObject panel)
        {
            Assert.That(panel.activeInHierarchy, Is.True);
            foreach (var button in panel.GetComponentsInChildren<Button>()) { AssertInside(button.GetComponent<RectTransform>()); if (button.interactable) AssertClickable(button.transform); }
        }
        private void Capture(Vector2Int size,string state)
        {
            var previous = RenderTexture.active;
            var pixels = new Texture2D(size.x,size.y,TextureFormat.RGB24,false);
            try
            {
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,size.x,size.y),0,0); pixels.Apply();
#if UNITY_EDITOR
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../Logs"));
#else
                string folder=Application.temporaryCachePath;
#endif
                Directory.CreateDirectory(folder); File.WriteAllBytes(Path.Combine(folder,$"t075-ui-{size.x}x{size.y}-{state}.png"),pixels.EncodeToPNG());
            }
            finally { RenderTexture.active=previous; Object.DestroyImmediate(pixels); }
        }
        private static T Field<T>(object owner,string name) => (T)owner.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(owner);
    }
}
