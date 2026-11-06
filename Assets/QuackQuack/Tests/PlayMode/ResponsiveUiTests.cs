using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QuackQuack.Core.Run;
using QuackQuack.Core.Shared;
using QuackQuack.Core.Progression;
using QuackQuack.Presentation.UI;
using QuackQuack.Systems.Run;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace QuackQuack.Tests.PlayMode
{
    // Uses the authored HUD and production SafeAreaController.
    public class ResponsiveUiTests
    {
        private GameplayHud hud;
        private Canvas canvas;
        private Camera camera;
        private RenderTexture target;
        private GameObject cameraObject, safeObject;
        private Vector2 viewport;
        private Rect safeArea;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            for (int i = 0; i < 120 && SceneManager.GetActiveScene().name != "Lake"; i++) yield return null;
            yield return null;
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Lake"));
            Assert.That(GameBootstrap.Instance, Is.Not.Null);
            Assert.That(GameBootstrap.Instance.Run.TryTransition(RunPhase.Paused), Is.True);
            hud = Object.FindAnyObjectByType<GameplayHud>();
            Assert.That(hud, Is.Not.Null, "Lake must contain the authored HUD.");
            hud.Unbind();
            var experience = new ExperienceState(new[] { 10, 30, 60 }); experience.TryAdd(35, out _);
            hud.Render(new HudSnapshot(75, 100, 65, 100, "",
                "Sem Fôlego para usar o Quack.", 920, false, "QUACK INDISPONÍVEL", experience.Progress));
            canvas = hud.GetComponentInParent<Canvas>();
            Assert.That(canvas, Is.Not.Null);
            cameraObject = new GameObject("T063 viewport camera");
            camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (canvas != null) canvas.worldCamera = null;
            if (camera != null) camera.targetTexture = null;
            if (target != null) { target.Release(); Object.Destroy(target); }
            if (safeObject != null) Object.Destroy(safeObject);
            if (cameraObject != null) Object.Destroy(cameraObject);
            if (GameBootstrap.Instance != null) Object.Destroy(GameBootstrap.Instance.gameObject);
            yield return null;
            var scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("After responsive UI test"));
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTest]
        public IEnumerator AuthoredHudEssentialsFitTheUncutLandscapeViewport([ValueSource(nameof(Viewports))] Vector2Int size)
        {
            int width = size.x, height = size.y;
            yield return SetViewport(width, height);
            AssertEssentialsInside(new Rect(0, 0, width, height));
        }

        [UnityTest]
        public IEnumerator AuthoredHudHasReferenceScalerAndKeepsEssentialsInsideCutout([ValueSource(nameof(Viewports))] Vector2Int size)
        {
            int width = size.x, height = size.y;
            yield return SetViewport(width, height);
            var scaler = canvas.GetComponent<CanvasScaler>();
            Assert.That(scaler, Is.Not.Null, "T069 pending: authored HUD needs CanvasScaler, not a test-created replacement.");
            Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920, 1080)));
            var controller = canvas.GetComponentInChildren<SafeAreaController>(true);
            Assert.That(controller, Is.Not.Null, "T068/T069 pending: authored HUD must have a bound SafeAreaController.");
            var safeRoot = controller.GetComponent<RectTransform>();
            foreach (var essential in Essentials())
                Assert.That(essential.IsChildOf(safeRoot), Is.True, essential.name + " must be inside the safe hierarchy.");
            safeArea = Insets(width, height);
            SetProvider(controller);
            // The injected source is also used by the controller's normal refresh path.
            controller.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);
            Canvas.ForceUpdateCanvases();
            AssertNormalizedSafeRoot(safeRoot);
            AssertEssentialsInside(safeArea);
            CaptureViewport();
        }

        [UnityTest]
        public IEnumerator SafeAreaNormalizesAsymmetricInsetsIsIdempotentAndResets([ValueSource(nameof(Viewports))] Vector2Int size)
        {
            int width = size.x, height = size.y;
            yield return SetViewport(width, height);
            var controller = CreateSafeController();
            var rect = controller.GetComponent<RectTransform>();
            safeArea = Insets(width, height);
            SetProvider(controller);
            controller.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);
            AssertNormalizedSafeRoot(rect);
            for (int i = 0; i < 3; i++) controller.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);
            AssertNormalizedSafeRoot(rect); // Insets must not accumulate on repeated refresh.
            safeArea = new Rect(0, 0, width, height);
            controller.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);
            AssertNormalizedSafeRoot(rect);
        }

        [UnityTest]
        public IEnumerator SafeAreaRefreshesAfterLandscapeResizeAndCutoutMovesSides()
        {
            yield return SetViewport(1920, 1080);
            var controller = CreateSafeController();
            var rect = controller.GetComponent<RectTransform>();
            safeArea = Insets(1920, 1080);
            SetProvider(controller);
            yield return null; yield return null;
            AssertNormalizedSafeRoot(rect);
            yield return SetViewport(2340, 1080);
            safeArea = new Rect(24, 32, 2340 - 96, 1080 - 80);
            yield return null; yield return null;
            AssertNormalizedSafeRoot(rect);
            // A cutout change without a resolution change must also invalidate cached anchors.
            safeArea = Insets(2340, 1080);
            yield return null; yield return null;
            AssertNormalizedSafeRoot(rect);
            safeArea = new Rect(0, 0, 2340, 1080);
            yield return null; yield return null;
            AssertNormalizedSafeRoot(rect);
        }

        [UnityTest]
        public IEnumerator SafeAreaKeepsLastValidLayoutWhileScreenGeometryIsUnavailable()
        {
            yield return SetViewport(1920, 1080);
            var controller = CreateSafeController();
            var rect = controller.GetComponent<RectTransform>();
            safeArea = Insets(1920, 1080);
            SetProvider(controller);
            var min = rect.anchorMin;
            var max = rect.anchorMax;
            foreach (var invalid in new[] { Vector2.zero, new Vector2(float.NaN, 1080), new Vector2(1920, float.PositiveInfinity) })
            {
                viewport = invalid;
                yield return null; yield return null;
                Assert.That(rect.anchorMin, Is.EqualTo(min));
                Assert.That(rect.anchorMax, Is.EqualTo(max));
            }
            viewport = new Vector2(1920, 1080);
            foreach (var invalid in new[] { Rect.zero, new Rect(float.NaN, 0, 100, 100), new Rect(2000, 0, 100, 100) })
            {
                safeArea = invalid;
                yield return null; yield return null;
                Assert.That(rect.anchorMin, Is.EqualTo(min));
                Assert.That(rect.anchorMax, Is.EqualTo(max));
            }
            safeArea = new Rect(0, 0, 1920, 1080);
            yield return null; yield return null;
            AssertNormalizedSafeRoot(rect);
        }

        [UnityTest]
        public IEnumerator SafeAreaClipsToViewportAndRefreshesWhenReenabled()
        {
            yield return SetViewport(1920, 1080);
            var controller = CreateSafeController();
            var rect = controller.GetComponent<RectTransform>();
            safeArea = new Rect(-20, -10, 1960, 1100);
            SetProvider(controller);
            Assert.That(rect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one));
            controller.enabled = false;
            safeArea = Insets(1920, 1080);
            yield return null;
            Assert.That(rect.anchorMin, Is.EqualTo(Vector2.zero));
            controller.enabled = true;
            AssertNormalizedSafeRoot(rect);
            controller.SetViewportProvider(null, null);
            var screenMin = new Vector2(Screen.safeArea.xMin / Screen.width, Screen.safeArea.yMin / Screen.height);
            var screenMax = new Vector2(Screen.safeArea.xMax / Screen.width, Screen.safeArea.yMax / Screen.height);
            Assert.That(Vector2.Distance(rect.anchorMin, screenMin), Is.LessThan(0.0001));
            Assert.That(Vector2.Distance(rect.anchorMax, screenMax), Is.LessThan(0.0001));
        }

        private static readonly Vector2Int[] Viewports = {
            new Vector2Int(1920, 1080), new Vector2Int(2160, 1080),
            new Vector2Int(2340, 1080), new Vector2Int(1440, 1080)
        };

        private IEnumerator SetViewport(int width, int height)
        {
            Assert.That(width, Is.GreaterThan(height), "The required matrix is landscape.");
            viewport = new Vector2(width, height);
            camera.targetTexture = null;
            if (target != null) { target.Release(); Object.DestroyImmediate(target); }
            target = new RenderTexture(width, height, 0);
            target.Create();
            camera.targetTexture = target;
            camera.rect = new Rect(0, 0, 1, 1);
            camera.aspect = (float)width / height;
            yield return null; yield return null;
            canvas.SendMessage("Update", SendMessageOptions.DontRequireReceiver);
            Canvas.ForceUpdateCanvases();
            Assert.That(canvas.pixelRect.width, Is.EqualTo(width).Within(1), "The real canvas must use the test viewport.");
            Assert.That(canvas.pixelRect.height, Is.EqualTo(height).Within(1));
            var corners = new Vector3[4];
            ((RectTransform)canvas.transform).GetWorldCorners(corners);
            var min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            var max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            Assert.That(Vector2.Distance(min, Vector2.zero), Is.LessThan(1), "Fixture canvas projection must start at viewport origin.");
            Assert.That(Vector2.Distance(max, viewport), Is.LessThan(1), "Fixture canvas projection must match viewport dimensions.");
        }

        private static Rect Insets(int width, int height) => new Rect(72, 32, width - 96, height - 80);

        private void CaptureViewport()
        {
            var previous = RenderTexture.active;
            var pixels = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            try
            {
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                pixels.Apply();
                var directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../Logs"));
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, $"t069-hud-{target.width}x{target.height}.png"), pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                Object.Destroy(pixels);
            }
        }

        private SafeAreaController CreateSafeController()
        {
            safeObject = new GameObject("T068 safe root", typeof(RectTransform));
            safeObject.transform.SetParent(canvas.transform, false);
            return safeObject.AddComponent<SafeAreaController>();
        }

        private void SetProvider(SafeAreaController controller)
        {
            controller.SetViewportProvider(() => viewport, () => safeArea);
        }

        private void AssertNormalizedSafeRoot(RectTransform rect)
        {
            Assert.That(rect, Is.Not.Null);
            var min = new Vector2(safeArea.xMin / viewport.x, safeArea.yMin / viewport.y);
            var max = new Vector2(safeArea.xMax / viewport.x, safeArea.yMax / viewport.y);
            Assert.That(Vector2.Distance(rect.anchorMin, min), Is.LessThan(0.0001));
            Assert.That(Vector2.Distance(rect.anchorMax, max), Is.LessThan(0.0001));
            Assert.That(rect.offsetMin.sqrMagnitude, Is.LessThan(0.0001));
            Assert.That(rect.offsetMax.sqrMagnitude, Is.LessThan(0.0001));
        }

        private IEnumerable<RectTransform> Essentials()
        {
            foreach (var name in new[] { "healthIcon", "breathIcon", "quackLabel", "timeLabel", "healthFill", "breathFill", "experienceFill", "levelLabel" })
            {
                var field = typeof(GameplayHud).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(field, Is.Not.Null, "Missing HUD binding: " + name);
                var graphic = field.GetValue(hud) as Graphic;
                Assert.That(graphic, Is.Not.Null, "Missing essential graphic: " + name);
                Assert.That(graphic.isActiveAndEnabled, Is.True, name + " must remain visible.");
                yield return graphic.rectTransform;
            }
        }

        private void AssertEssentialsInside(Rect bounds)
        {
            var corners = new Vector3[4];
            foreach (var essential in Essentials())
            {
                essential.GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    var point = RectTransformUtility.WorldToScreenPoint(camera, corner);
                    Assert.That(point.x, Is.InRange(bounds.xMin - 0.5f, bounds.xMax + 0.5f), essential.name + " exceeds horizontal safe bounds.");
                    Assert.That(point.y, Is.InRange(bounds.yMin - 0.5f, bounds.yMax + 0.5f), essential.name + " exceeds vertical safe bounds.");
                }
                var text = essential.GetComponent<Text>();
                if (text == null) continue;
                Assert.That(text.text, Is.Not.Empty);
                Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(essential.rect.height + 0.5f), text.name + " must not truncate essential text vertically.");
            }
        }
    }
}
