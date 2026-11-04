using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using QuackQuack.Core.Run;
using QuackQuack.Gameplay.Upgrades;
using QuackQuack.Platform.Input;
using QuackQuack.Presentation.UI;
using QuackQuack.Systems.Run;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace QuackQuack.Tests.PlayMode
{
    public sealed class ControlSettingsFlowTests
    {
        private static readonly Vector2Int[] Sizes = { new Vector2Int(1920,1080), new Vector2Int(2160,1080), new Vector2Int(2340,1080), new Vector2Int(1440,1080) };
        private static readonly string[] VerticalInputs = { "DpadDown", "DpadUp", "StickDown", "StickUp" };
        private ControlSettingsMenu menu;
        private MenuRegressionInput driver;
        private RunDirector director;
        private Func<string> previousSave;
        private string directory;
        private Camera camera;
        private RenderTexture target;
        private RunState Run => GameBootstrap.Instance.Run;
        private Button Button(string name) => menu.GetComponentsInChildren<Button>(true).Single(b => b.name == name);
        [UnitySetUp] public IEnumerator SetUp()
        {
            directory = Path.Combine(Application.temporaryCachePath,"T013-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
            previousSave = RunDirector.SavePathProvider; RunDirector.SavePathProvider = () => Path.Combine(directory,"progress.json");
            yield return SceneManager.LoadSceneAsync("RunBootstrap");
            for(int i=0;i<600;i++) { director=Object.FindAnyObjectByType<RunDirector>(); menu=Object.FindAnyObjectByType<ControlSettingsMenu>(); if(director != null && director.Ready && menu != null) break; yield return null; }
            Assert.That(director != null && director.Ready && menu != null, Is.True); director.AutomaticTick=false; director.Scheduler.enabled=false;
            GameBootstrap.Instance.ExternalClock=true; driver=new MenuRegressionInput(); yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            driver?.Dispose(); driver=null;
            if(director != null && director.Ready) director.TryAbandon();
            if(GameBootstrap.Instance != null) Object.Destroy(GameBootstrap.Instance.gameObject); yield return null;
            var scene=SceneManager.GetActiveScene(); SceneManager.SetActiveScene(SceneManager.CreateScene("After T013 "+Guid.NewGuid().ToString("N")));
            if(scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            if(camera != null) Object.Destroy(camera.gameObject);
            if(target != null) {target.Release(); Object.Destroy(target);}
            RunDirector.SavePathProvider=previousSave;
            Assert.That(Path.GetFullPath(directory).StartsWith(Path.GetFullPath(Application.temporaryCachePath),StringComparison.OrdinalIgnoreCase),Is.True);
            if(Directory.Exists(directory)) Directory.Delete(directory,true);
        }
        [UnityTest] public IEnumerator PauseExposesProfileWithoutSelectAndCancelPreservesPause()
        {
            yield return driver.Press(GamepadButton.Start); Assert.That(Run.Phase,Is.EqualTo(RunPhase.Paused));
            Assert.That(Button("Open settings").isActiveAndEnabled,Is.True);
            Button("Open settings").Select(); yield return driver.Press(GamepadButton.South);
            Assert.That(menu.IsVisible,Is.True); var panel=menu.GetComponentInChildren<ControlBindingsView>(true).transform;
            MenuRegressionInput.AssertUsableFocus(panel); MenuRegressionInput.AssertReachableActions(panel,Button("quack"));
            yield return driver.Press(GamepadButton.East); Assert.That(menu.IsVisible,Is.False); Assert.That(Run.Phase,Is.EqualTo(RunPhase.Paused));
            Assert.That(EventSystem.current.currentSelectedGameObject,Is.EqualTo(Button("Open settings").gameObject));
            yield return driver.Press(GamepadButton.East); Assert.That(Run.IsSimulating,Is.True);
        }
        [UnityTest] public IEnumerator PauseVerticalNavigationMatchesScreenOrder([ValueSource(nameof(VerticalInputs))] string source)
        {
            director.TogglePause(); yield return null; Canvas.ForceUpdateCanvases();
            var controls=GameObject.Find("Run controls").GetComponentsInChildren<Button>();
            var ordered=new[] { Object.FindAnyObjectByType<RelicLoadoutView>().OpenButton,
                controls.Single(b=>b.name=="PAUSAR"), controls.Single(b=>b.name=="ABANDONAR"), Button("Open settings") };
            for(int i=1;i<ordered.Length;i++) Assert.That(ordered[i].transform.position.y,Is.LessThan(ordered[i-1].transform.position.y),"fixture must follow visual top-to-bottom order");
            bool down=source.EndsWith("Down",StringComparison.Ordinal);
            for(int i=0;i<ordered.Length;i++) {
                ordered[i].Select();
                if(source.StartsWith("Dpad",StringComparison.Ordinal)) yield return driver.Press(down?GamepadButton.DpadDown:GamepadButton.DpadUp);
                else yield return driver.Tilt(down?Vector2.down:Vector2.up);
                Assert.That(EventSystem.current.currentSelectedGameObject,Is.EqualTo(ordered[(i+(down?1:ordered.Length-1))%ordered.Length].gameObject),source+" from "+ordered[i].name);
                Assert.That(Run.Phase,Is.EqualTo(RunPhase.Paused));
            }
        }
        [UnityTest] public IEnumerator ProfileOwnedPauseReturnsToBossAndDoesNotQuack()
        {
            Assert.That(Run.TryTransition(RunPhase.Boss),Is.True); int quacks=0;
            GameBootstrap.Instance.Events.Combat += e => {if(e.Type==QuackQuack.Core.Combat.CombatEventType.QuackActivated)quacks++;};
            menu.Open(); yield return null; Assert.That(Run.Phase,Is.EqualTo(RunPhase.Paused));
            yield return driver.Press(GamepadButton.Start); Assert.That(menu.IsVisible,Is.True); Assert.That(Run.Phase,Is.EqualTo(RunPhase.Paused));
            yield return driver.Press(GamepadButton.East); Assert.That(Run.Phase,Is.EqualTo(RunPhase.Boss)); Assert.That(quacks,Is.Zero);
        }
        [UnityTest] public IEnumerator ProfileRelicsButtonRemapsWithoutSelectAndNewShortcutWorks()
        {
            director.TogglePause(); menu.Open(); yield return null;
            var store=new ControlProfileStore(Path.Combine(directory,"controls")); var view=menu.GetComponentInChildren<ControlBindingsView>(true); view.Bind(store);
            Button("relics").Select(); yield return driver.Press(GamepadButton.South);
            Assert.That(store.Current.Binding(ProfileAction.Relics),Is.EqualTo("<Gamepad>/buttonNorth"));
            Assert.That(new ControlProfileStore(Path.Combine(directory,"controls")).Current.Binding(ProfileAction.Relics),Is.EqualTo("<Gamepad>/buttonNorth"));
            Assert.That(view.Assign(ProfileAction.Relics,"<Gamepad>/start"),Is.False,"conflicting Pause binding cannot replace Relics");
            yield return driver.Press(GamepadButton.East); Assert.That(Run.Phase,Is.EqualTo(RunPhase.Paused));
            // Shared production store is untouched; verify the custom profile using its own live adapter.
            using(var input=new GamepadInputAdapter(store)) { yield return driver.Press(GamepadButton.North); Assert.That(input.Read().RelicsPressed,Is.True); }
        }
        [UnityTest] public IEnumerator ProfileRepairsFocusAfterHotplugAndInvalidSelection()
        {
            menu.Open(); yield return null; EventSystem.current.SetSelectedGameObject(null);
            driver.Dispose(); driver=new MenuRegressionInput(); yield return null;
            MenuRegressionInput.AssertUsableFocus(menu.GetComponentInChildren<ControlBindingsView>(true).transform);
            yield return driver.Press(GamepadButton.East); Assert.That(menu.IsVisible,Is.False); Assert.That(Run.IsSimulating,Is.True);
        }
        [UnityTest] public IEnumerator ProfileFitsSafeAreaInSupportedRatios([ValueSource(nameof(Sizes))] Vector2Int size)
        {
            menu.Open(); yield return null;
            camera=new GameObject("T013 UI capture").AddComponent<Camera>(); camera.enabled=false; camera.orthographic=true; camera.transform.position=new Vector3(0,0,-20);
            target=new RenderTexture(size.x,size.y,24); target.Create(); camera.targetTexture=target; camera.aspect=(float)size.x/size.y;
            var canvas=menu.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=10;
            foreach(bool right in new[]{false,true}) {
                var safe=new Rect(right?0:84,0,size.x-84,size.y);
                menu.GetComponentInChildren<SafeAreaController>().SetViewportProvider(()=>new Vector2(size.x,size.y),()=>safe);
                Canvas.ForceUpdateCanvases(); yield return null; Canvas.ForceUpdateCanvases();
                var panel=menu.GetComponentInChildren<ControlBindingsView>(true).transform;
                foreach(var rect in panel.GetComponentsInChildren<RectTransform>()) {
                    if(rect==panel || rect.name=="Reference content")continue;
                    var corners=new Vector3[4]; rect.GetWorldCorners(corners);
                    foreach(var corner in corners) {var p=RectTransformUtility.WorldToScreenPoint(camera,corner); Assert.That(p.x,Is.InRange(safe.xMin-2,safe.xMax+2),rect.name); Assert.That(p.y,Is.InRange(safe.yMin-2,safe.yMax+2),rect.name);}
                }
                if(size.x==1920 && !right) {
                    string captureDirectory = Application.isEditor ? "Logs/Polimento002/T013" : Path.Combine(Application.temporaryCachePath,"T013-preview");
                    Directory.CreateDirectory(captureDirectory);
                    RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
                    var old=RenderTexture.active; RenderTexture.active=target; var image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,size.x,size.y),0,0); image.Apply();
                    File.WriteAllBytes(Path.Combine(captureDirectory,"profile-preview.png"),image.EncodeToPNG()); RenderTexture.active=old; Object.Destroy(image);
                }
            }
        }
    }
}
