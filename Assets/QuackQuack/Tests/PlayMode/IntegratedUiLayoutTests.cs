using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QuackQuack.Core.Run;
using QuackQuack.Core.Shared;
using QuackQuack.Core.Progression;
using QuackQuack.Gameplay.Player;
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
        private Camera worldCamera;
        private RenderTexture target;
        private RunDirector director;
        private Func<string> previousPath;
        private string directory;
        private Rect safe;
        private Vector2 viewport;
        private readonly List<Gamepad> createdPads = new List<Gamepad>();
        private readonly List<Gamepad> disabledPads = new List<Gamepad>();
        private Gamepad previousPad;
        private InputSettings.BackgroundBehavior previousBackground;

        [UnitySetUp] public IEnumerator SetUp()
        {
            if (GameBootstrap.Instance != null) Object.Destroy(GameBootstrap.Instance.gameObject);
            yield return null;
            createdPads.Clear(); disabledPads.Clear(); previousPad = Gamepad.current;
            previousBackground = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            foreach (var pad in Gamepad.all) if (pad.enabled) disabledPads.Add(pad);
            foreach (var pad in disabledPads) InputSystem.DisableDevice(pad);
            directory = Path.Combine(Application.temporaryCachePath, "QuackQuack-T039Ui-" + Guid.NewGuid().ToString("N"));
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
            var water = Object.FindAnyObjectByType<QuackQuack.Presentation.Terrain.LakeWaterAnimation>();
            Assert.That(water, Is.Not.Null, "The production scene needs its surface presentation.");
            Assert.That(water.IsBound, Is.True, "Bootstrap must bind the shared simulation clock.");
            var composition = Object.FindAnyObjectByType<GameplayHudComposition>();
            for (int i = 0; i < 60 && !composition.Ready; i++) yield return null;
            Assert.That(composition.Ready, Is.True);
            director.AutomaticTick = false;
            director.Scheduler.enabled = false;
            foreach (var runner in Object.FindObjectsByType<QuackQuack.Gameplay.Weapons.WeaponRunner>(FindObjectsSortMode.None)) runner.enabled = false;
            director.TogglePause();
            worldCamera = Camera.main;
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
            foreach (var pad in createdPads) if (pad.added) InputSystem.RemoveDevice(pad);
            foreach (var pad in disabledPads) if (pad.added) InputSystem.EnableDevice(pad);
            if (previousPad != null && previousPad.added) previousPad.MakeCurrent();
            InputSystem.settings.backgroundBehavior = previousBackground;
            if (worldCamera != null) worldCamera.targetTexture = null;
            if (camera != null) { camera.targetTexture = null; Object.Destroy(camera.gameObject); }
            if (target != null) { target.Release(); Object.Destroy(target); }
            RunDirector.SavePathProvider = previousPath;
            string resolved = Path.GetFullPath(directory), prefix = Path.GetFullPath(Application.temporaryCachePath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            Assert.That(resolved.StartsWith(prefix, StringComparison.OrdinalIgnoreCase), Is.True);
            Assert.That(Path.GetFileName(resolved), Does.StartWith("QuackQuack-T039Ui-"));
            Directory.Delete(resolved, true);
        }

        [UnityTest] public IEnumerator ProductionHudTouchAndModalsFitAndRemainClickable([ValueSource(nameof(Viewports))] Vector2Int size)
        {
            yield return SetViewport(size);
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
                    Field<ResourceIconGraphic>(hud,"healthIcon").transform.parent as RectTransform,
                    open.GetComponent<RectTransform>(),
                    Field<Button>(director,"pauseButton").GetComponent<RectTransform>(),
                    Field<Button>(director,"abandonButton").GetComponent<RectTransform>()
                };
                foreach (string name in new[] { "Joystick","QUACK" })
                    foreach (var rect in mobile.GetComponentsInChildren<RectTransform>()) if (rect.name == name) rects.Add(rect);
                Assert.That(rects.Count, Is.EqualTo(7));
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
            var offer = choices.Displayed;
            var previewCards = new UpgradeCardSnapshot[offer.Cards.Count];
            for(int i=0;i<previewCards.Length;i++) previewCards[i] = offer.Cards[i];
            previewCards[0] = new UpgradeCardSnapshot("sabao-magico","Sabão Mágico","PASSIVA","Raio: 0,20 u","Raio: 0,232 u",
                "Amplia o raio de Bolhas.","Arma compatível: Bolhas.",.2f,.232f,"Bolhas: raio ampliado.");
            foreach (bool right in new[] { false,true })
            {
                yield return ApplyInsets(right);
                // A presentation-only offer guarantees preview coverage regardless of RNG; keep the live offer ID for Skip.
                choices.Render(new UpgradeSelectionSnapshot(offer.OfferId,true,offer.Level,offer.Rerolls,offer.Banishes,previewCards));
                Canvas.ForceUpdateCanvases();
                var card = Field<UpgradeCardWidgets[]>(choices,"cards")[0];
                Assert.That(card.areaPreview.gameObject.activeInHierarchy, Is.True);
                var current = Field<Image>(card.areaPreview,"current"); var next = Field<Image>(card.areaPreview,"next");
                AssertInside(current.rectTransform); AssertInside(next.rectTransform); AssertTextFits(Field<Text>(card.areaPreview,"caption"));
                Assert.That(next.rectTransform.rect.width, Is.GreaterThan(current.rectTransform.rect.width)); AssertTextFits(card.effects);
                AssertModal(Field<GameObject>(choices,"panel")); Capture(size,"cards-"+right);
            }
            Field<Button>(choices,"skip").onClick.Invoke(); yield return null;
            Assert.That(director.TryAbandon(), Is.True); yield return null;
            var results = Object.FindAnyObjectByType<ResultsView>();
            foreach (bool right in new[] { false,true }) { yield return ApplyInsets(right); AssertModal(Field<GameObject>(results,"panel")); Capture(size,"results-"+right); }
        }

        [UnityTest] public IEnumerator ResourceStatesProgressAndTemporaryHintsFitBothCutouts([ValueSource(nameof(Viewports))] Vector2Int size)
        {
            yield return SetViewport(size); director.TogglePause();
            var hud = Object.FindAnyObjectByType<GameplayHud>(); hud.Unbind();
            var hints = Object.FindAnyObjectByType<ContextualHintsView>();
            var profile = new ControlProfile(); hints.Bind(GameBootstrap.Instance.Run, () => InputSource.Touch, () => profile);
            var mobile = Object.FindAnyObjectByType<MobileInputAdapter>();
            foreach (bool right in new[] { false, true })
            {
                yield return ApplyInsets(right);
                // Synthetic read-only layout data exercises empty/partial/full resources with effective maxima.
                foreach (int state in new[] { 0, 1, 2 })
                {
                    float fraction = state * .5f;
                    var xp = new ExperienceState(new[] { 10, 30, 60 }); xp.TryAdd(state == 0 ? 0 : state == 1 ? 35 : 60, out _);
                    string quack = state == 0 ? "SEM FÔLEGO" : state == 1 ? "PRONTO • QUACK" : "PRONTO • APRIMORADO";
                    hud.Render(new HudSnapshot(200*fraction,200,140*fraction,140,"","Sem Fôlego para usar o Quack.",920,state > 0,quack,xp.Progress));
                    Canvas.ForceUpdateCanvases();
                    Assert.That(Field<Image>(hud,"healthFill").fillAmount, Is.EqualTo(fraction));
                    Assert.That(Field<Image>(hud,"breathFill").fillAmount, Is.EqualTo(fraction));
                    Assert.That(Field<Image>(hud,"experienceFill").fillAmount, Is.EqualTo(xp.Progress.Fraction));
                    Assert.That(Field<Text>(hud,"levelLabel").text, Is.EqualTo("NÍVEL " + xp.Level));
                    Assert.That(Field<Text>(hud,"quackLabel").text, Is.EqualTo(quack));
                    Assert.That(Field<Text>(hud,"healthLabel"), Is.Null); Assert.That(Field<Text>(hud,"terrainLabel"), Is.Null);
                    Assert.That(Field<Text>(hud,"breathLabel"), Is.Null);
                    var progressBounds = ScreenRect(Field<Text>(hud,"levelLabel").transform.parent as RectTransform);
                    Assert.That(progressBounds.center.x, Is.EqualTo(safe.center.x).Within(.5f));
                    Assert.That(progressBounds.yMax, Is.LessThan(safe.center.y));
                    Assert.That(hints.IsVisible, Is.True);
                    var panels = FixedPanels(hud, mobile, true); AssertSeparated(panels);
                    Assert.That(Field<Text>(hud,"statusLabel"), Is.Null);
                    Assert.That(Field<Text>(hud,"levelLabel").transform.parent.GetComponent<RectTransform>().anchoredPosition.y, Is.EqualTo(12));
                    foreach (var text in new[] { Field<Text>(hud,"levelLabel"), Field<Text>(hud,"quackLabel"), Field<Text>(hints,"label") }) AssertTextFits(text);
                    Capture(size,"resources-"+state+"-"+right);
                }
            }
        }

        [UnityTest] public IEnumerator ProfileFamiliesRemappedLabelsAndModalBlockingFitBothCutouts([ValueSource(nameof(Viewports))] Vector2Int size)
        {
            yield return SetViewport(size);
            var pad = InputSystem.AddDevice<Gamepad>(); createdPads.Add(pad); pad.MakeCurrent(); yield return null;
            var menu = Object.FindAnyObjectByType<ControlSettingsMenu>(); menu.Open(); yield return null;
            var bindings = Field<ControlBindingsView>(menu,"bindings");
            var store = new ControlProfileStore(Path.Combine(directory,"profiles")); bindings.Bind(store);
            var hints = Object.FindAnyObjectByType<ContextualHintsView>();
            hints.Bind(GameBootstrap.Instance.Run, () => InputSource.Gamepad, () => store.Current);
            foreach (bool right in new[] { false,true })
            {
                foreach (var family in new[] { ControllerFamily.Xbox, ControllerFamily.PlayStation, ControllerFamily.Generic })
                {
                    Assert.That(store.TrySave(new ControlProfile().WithFamily(family),true,true,out _), Is.True);
                    yield return ApplyInsets(right);
                    Assert.That(menu.IsVisible, Is.True); var panel = Field<GameObject>(menu,"panel"); AssertModal(panel);
                    foreach (var action in new[] { ProfileAction.Quack,ProfileAction.Pause,ProfileAction.Relics })
                    {
                        string field = action == ProfileAction.Quack ? "quack" : action == ProfileAction.Pause ? "pause" : "relics";
                        Assert.That(Field<Button>(bindings,field).GetComponentInChildren<Text>().text, Does.Contain(store.Current.Label(action,pad)));
                    }
                    Assert.That(bindings.Assign(ProfileAction.Relics,"<Gamepad>/buttonNorth"), Is.True);
                    yield return null; AssertModal(panel);
                    Assert.That(Field<Button>(bindings,"relics").GetComponentInChildren<Text>().text, Does.Contain(store.Current.Label(ProfileAction.Relics,pad)));
                    foreach (var text in panel.GetComponentsInChildren<Text>()) if (!string.IsNullOrEmpty(text.text)) AssertTextFits(text);
                    Assert.That(EventSystem.current.currentSelectedGameObject.transform.IsChildOf(panel.transform), Is.True);
                    Capture(size,"profile-"+family+"-"+right);
                    menu.Close(); if (GameBootstrap.Instance.Run.Phase == RunPhase.Paused) director.TogglePause();
                    yield return null; Assert.That(hints.IsVisible, Is.True); AssertTextFits(Field<Text>(hints,"label"));
                    Assert.That(Field<Text>(hints,"label").text, Does.Contain(store.Current.Label(ProfileAction.Relics,pad)));
                    menu.Open(); yield return null;
                }
            }
            menu.Close();
        }

        [UnityTest] public IEnumerator MultipleSpecialsAndControllerConditionalTouchFitBothCutouts([ValueSource(nameof(Viewports))] Vector2Int size)
        {
            yield return SetViewport(size); director.TogglePause();
            var mobile = Object.FindAnyObjectByType<MobileInputAdapter>();
            var hud = Object.FindAnyObjectByType<GameplayHud>();
            var composition = hud.GetComponent<GameplayHudComposition>();
            var specials = Field<SpecialEnemyBarsView>(composition,"specials");
            var source = new SpecialEnemyRegistry(3); var point = worldCamera.ViewportToWorldPoint(new Vector3(.5f,.48f,10));
            // Feed the authored view through its public snapshot binding; do not fabricate actor coexistence in gameplay.
            source.TryAdd(new SpecialEnemySnapshot(1,1,"alpha","Libélula Alfa",SpecialEnemyCategory.MiniBoss,point,25,100));
            source.TryAdd(new SpecialEnemySnapshot(2,1,"cururu","Sapo-Cururu",SpecialEnemyCategory.MiniBoss,point,60,100));
            source.TryAdd(new SpecialEnemySnapshot(3,1,"king","Sapo-Rei",SpecialEnemyCategory.Boss,point,100,100));
            specials.Bind(source,worldCamera,Field<RectTransform>(composition,"specialArea"));
            foreach (bool right in new[] { false,true })
            {
                yield return ApplyInsets(right); AssertTouch(mobile,true); AssertSpecials(specials,3,FixedPanels(hud,mobile,true));
                Capture(size,"specials-touch-"+right);
                var pad = InputSystem.AddDevice<Gamepad>(); createdPads.Add(pad); yield return null; yield return null;
                AssertTouch(mobile,false); AssertSpecials(specials,3,FixedPanels(hud,mobile,false));
                Capture(size,"specials-gamepad-"+right);
                InputSystem.DisableDevice(pad); yield return null; AssertTouch(mobile,true);
                InputSystem.EnableDevice(pad); yield return null; AssertTouch(mobile,false);
                InputSystem.RemoveDevice(pad); yield return null; yield return null; AssertTouch(mobile,true);
                AssertSpecials(specials,3,FixedPanels(hud,mobile,true));
                source.Clear();
                source.TryAdd(new SpecialEnemySnapshot(1,1,"alpha","Libélula Alfa",SpecialEnemyCategory.MiniBoss,point,25,100));
                source.TryAdd(new SpecialEnemySnapshot(2,1,"cururu","Sapo-Cururu",SpecialEnemyCategory.MiniBoss,point,60,100));
                var outside = worldCamera.ViewportToWorldPoint(new Vector3(right ? 1.1f : -.1f,.48f,10));
                source.TryAdd(new SpecialEnemySnapshot(3,1,"king","Sapo-Rei",SpecialEnemyCategory.Boss,outside,100,100));
                specials.Refresh(); AssertSpecials(specials,2,FixedPanels(hud,mobile,true)); Assert.That(specials.TryGetBar(3,1,out _), Is.False);
                source.Clear();
                source.TryAdd(new SpecialEnemySnapshot(1,1,"alpha","Libélula Alfa",SpecialEnemyCategory.MiniBoss,point,25,100));
                source.TryAdd(new SpecialEnemySnapshot(2,1,"cururu","Sapo-Cururu",SpecialEnemyCategory.MiniBoss,point,60,100));
                source.TryAdd(new SpecialEnemySnapshot(3,1,"king","Sapo-Rei",SpecialEnemyCategory.Boss,point,100,100));
            }
        }

        private IEnumerator SetViewport(Vector2Int size)
        {
            viewport = new Vector2(size.x,size.y);
            target = new RenderTexture(size.x,size.y,24); target.Create();
            camera.targetTexture = target; camera.aspect = (float)size.x / size.y; camera.rect = new Rect(0,0,1,1);
            // World projection must use the same real target as the UI; never alter the tested viewport to fit content.
            worldCamera.targetTexture = target; worldCamera.aspect = (float)size.x / size.y; worldCamera.rect = new Rect(0,0,1,1);
            yield return null; yield return null; Canvas.ForceUpdateCanvases();
            Assert.That(worldCamera.pixelRect.size, Is.EqualTo(viewport)); Assert.That(camera.pixelRect.size, Is.EqualTo(viewport));
            Assert.That(camera.aspect, Is.EqualTo((float)size.x/size.y).Within(.0001));
        }
        private List<RectTransform> FixedPanels(GameplayHud hud, MobileInputAdapter mobile, bool touchVisible)
        {
            var result = new List<RectTransform> {
                Field<ResourceIconGraphic>(hud,"healthIcon").transform.parent as RectTransform,
                Field<Text>(hud,"timeLabel").transform.parent as RectTransform,
                Field<Text>(hud,"levelLabel").transform.parent as RectTransform,
                Object.FindAnyObjectByType<RelicLoadoutView>().OpenButton.GetComponent<RectTransform>(),
                Field<Button>(director,"pauseButton").GetComponent<RectTransform>(),
                Field<Button>(director,"abandonButton").GetComponent<RectTransform>()
            };
            var hints = Object.FindAnyObjectByType<ContextualHintsView>(); if (hints.IsVisible) result.Add(Field<Text>(hints,"label").rectTransform);
            if(touchVisible) foreach(var rect in mobile.GetComponentsInChildren<RectTransform>()) if(rect.name == "Joystick" || rect.name == "QUACK") result.Add(rect);
            return result;
        }
        private void AssertSeparated(List<RectTransform> rects)
        {
            foreach(var rect in rects) AssertInside(rect);
            for(int i=0;i<rects.Count;i++) for(int j=0;j<i;j++)
                Assert.That(ScreenRect(rects[i]).Overlaps(ScreenRect(rects[j])), Is.False, rects[i].name+" overlaps "+rects[j].name);
        }
        private void AssertSpecials(SpecialEnemyBarsView specials,int count,List<RectTransform> fixedPanels)
        {
            Assert.That(specials.VisibleCount, Is.EqualTo(count)); Assert.That(specials.HiddenForSpace, Is.Zero);
            var all = new List<RectTransform>(fixedPanels);
            for(int id=1;id<=count;id++)
            {
                Assert.That(specials.TryGetBar(id,1,out var bar), Is.True); all.Add(bar); AssertTextFits(bar.GetComponentInChildren<Text>());
                Assert.That(bar.Find("Health track/Health fill").GetComponent<RectTransform>().anchorMax.x, Is.EqualTo(id == 1 ? .25f : id == 2 ? .6f : 1));
            }
            AssertSeparated(all);
        }
        private static void AssertTouch(MobileInputAdapter mobile,bool visible)
        {
            int count = 0;
            foreach(var rect in mobile.GetComponentsInChildren<RectTransform>(true)) if(rect.name == "Joystick" || rect.name == "QUACK")
            { count++; Assert.That(rect.gameObject.activeInHierarchy, Is.EqualTo(visible),rect.name); }
            Assert.That(count, Is.EqualTo(2));
        }
        private void AssertTextFits(Text text)
        {
            AssertInside(text.rectTransform);
            Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(text.rectTransform.rect.height+.5f), text.name+" truncates essential text");
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
            var bounds = ScreenRect(panel.GetComponent<RectTransform>());
            Assert.That(Vector2.Distance(bounds.min,safe.min), Is.LessThan(1)); Assert.That(Vector2.Distance(bounds.max,safe.max), Is.LessThan(1));
            foreach(var point in new[] {safe.min+Vector2.one*2, safe.max-Vector2.one*2})
            {
                var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
                Assert.That(hits.Count, Is.GreaterThan(0)); Assert.That(hits[0].gameObject.transform == panel.transform || hits[0].gameObject.transform.IsChildOf(panel.transform), Is.True,"Modal must block underlying gameplay");
            }
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
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../Logs/Polimento002/T039"));
#else
                string folder=Path.Combine(Application.temporaryCachePath,"Polimento002/T039");
#endif
                string captureOverride=Environment.GetEnvironmentVariable("QUACKQUACK_UI_CAPTURE_DIR");
                if(!string.IsNullOrWhiteSpace(captureOverride))folder=Path.GetFullPath(captureOverride);
                Directory.CreateDirectory(folder); File.WriteAllBytes(Path.Combine(folder,$"{size.x}x{size.y}-{state}.png"),pixels.EncodeToPNG());
            }
            finally { RenderTexture.active=previous; Object.DestroyImmediate(pixels); }
        }
        private static T Field<T>(object owner,string name) => (T)owner.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(owner);
    }
}
