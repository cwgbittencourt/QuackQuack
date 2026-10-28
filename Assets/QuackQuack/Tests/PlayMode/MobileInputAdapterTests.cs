using System.Collections;
using NUnit.Framework;
using QuackQuack.Core.Shared;
using QuackQuack.Platform.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace QuackQuack.Tests.PlayMode
{
    public class MobileInputAdapterTests
    {
        private GameObject root, eventObject;
        private EventSystem previousEventSystem, events;
        private bool previousEnabled;
        private MobileInputAdapter input;
        private MobileJoystick joystick;
        private MobileActionButton quack, pause;
        private RectTransform stick, knob;
        private InputSettings.BackgroundBehavior background;
#if UNITY_EDITOR
        private InputSettings.EditorInputBehaviorInPlayMode editorInput;
#endif

        [SetUp]
        public void SetUp()
        {
            background = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            previousEventSystem = EventSystem.current;
            if (previousEventSystem != null) { previousEnabled = previousEventSystem.enabled; previousEventSystem.enabled = false; }
            eventObject = new GameObject("T066 events", typeof(EventSystem)); events = eventObject.GetComponent<EventSystem>();
            root = new GameObject("T066 controls", typeof(RectTransform), typeof(Canvas), typeof(MobileInputAdapter));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            input = root.GetComponent<MobileInputAdapter>();
            stick = Rect("Stick", root.transform, new Vector2(200, 200));
            knob = Rect("Knob", stick, new Vector2(40, 40));
            joystick = stick.gameObject.AddComponent<MobileJoystick>(); joystick.Configure(input, knob);
            quack = Rect("Quack", root.transform, new Vector2(100, 100)).gameObject.AddComponent<MobileActionButton>(); quack.Configure(input, MobileAction.Quack);
            pause = Rect("Pause", root.transform, new Vector2(100, 100)).gameObject.AddComponent<MobileActionButton>(); pause.Configure(input, MobileAction.Pause);
            Canvas.ForceUpdateCanvases();
        }
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root); Object.DestroyImmediate(eventObject);
            if (previousEventSystem != null) { previousEventSystem.enabled = previousEnabled; EventSystem.current = previousEventSystem; }
            InputSystem.settings.backgroundBehavior = background;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
#endif
        }

        [Test]
        public void CallbacksPreservePartialMovementNormalizeAndConsumeTapEdgesOnce()
        {
            input.SetMove(new Vector2(0.3f, 0.4f)); input.SetQuack(true); input.SetPause(true);
            var command = input.Read();
            Assert.That(command.Move.magnitude, Is.EqualTo(0.5f).Within(0.001));
            Assert.That(command.Source, Is.EqualTo(InputSource.Touch));
            Assert.That(command.QuackPressed && command.PausePressed, Is.True);
            input.SetQuack(true); input.SetPause(true);
            Assert.That(input.Read().QuackPressed, Is.False); Assert.That(input.Read().PausePressed, Is.False);
            input.SetQuack(false); input.SetPause(false); input.SetQuack(true); input.SetPause(true);
            input.SetQuack(false); input.SetPause(false);
            command = input.Read(); Assert.That(command.QuackPressed && command.PausePressed, Is.True);
            input.SetMove(Vector2.one * 3); Assert.That(input.Read().Move.magnitude, Is.EqualTo(1).Within(0.001));
        }

        [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)] [TestCase(float.NegativeInfinity)]
        public void NonFiniteMovementBecomesNeutral(float value)
        { input.SetMove(new Vector2(value, 1)); Assert.That(input.Read().Move, Is.EqualTo(Vector2.zero)); }

        [Test]
        public void JoystickCapturesOneFingerAndClampsDragToItsRadius()
        {
            var first = Pointer(1, new Vector2(40, 0));
            ExecuteEvents.Execute(stick.gameObject, first, ExecuteEvents.pointerDownHandler);
            Assert.That(input.Read().Move.x, Is.EqualTo(0.5f).Within(0.001));
            var other = Pointer(2, new Vector2(-80, 0));
            ExecuteEvents.Execute(stick.gameObject, other, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(stick.gameObject, other, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(stick.gameObject, other, ExecuteEvents.pointerUpHandler);
            Assert.That(input.Read().Move.x, Is.EqualTo(0.5f).Within(0.001));
            first.position = ScreenPoint(new Vector2(500, 500));
            ExecuteEvents.Execute(stick.gameObject, first, ExecuteEvents.dragHandler);
            Assert.That(input.Read().Move.magnitude, Is.EqualTo(1).Within(0.001));
            Assert.That(knob.anchoredPosition.magnitude, Is.EqualTo(80).Within(0.001));
            ExecuteEvents.Execute(stick.gameObject, first, ExecuteEvents.pointerUpHandler);
            Assert.That(input.Read().Move, Is.EqualTo(Vector2.zero)); Assert.That(knob.anchoredPosition, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void IndependentFingersCanMoveQuackAndPauseWithoutStealingButtons()
        {
            joystick.OnPointerDown(Pointer(1, new Vector2(80, 0)));
            quack.OnPointerDown(Pointer(2, Vector2.zero)); pause.OnPointerDown(Pointer(3, Vector2.zero));
            var command = input.Read(); Assert.That(command.Move, Is.EqualTo(Vector2.right));
            Assert.That(command.QuackPressed && command.PausePressed, Is.True);
            quack.OnPointerUp(Pointer(99, Vector2.zero)); quack.OnPointerDown(Pointer(99, Vector2.zero));
            Assert.That(input.Read().QuackPressed, Is.False);
            quack.OnPointerUp(Pointer(2, Vector2.zero)); quack.OnPointerDown(Pointer(2, Vector2.zero));
            Assert.That(input.Read().QuackPressed, Is.True);
            Assert.That(input.Read().Move, Is.EqualTo(Vector2.right));
        }

        [Test]
        public void CancelAndDisableDiscardUnreadCommandsAndReleaseOwnership()
        {
            quack.OnPointerDown(Pointer(1, Vector2.zero)); quack.OnCancel(new BaseEventData(events));
            Assert.That(input.Read().QuackPressed, Is.False);
            joystick.OnPointerDown(Pointer(2, new Vector2(80, 0))); joystick.enabled = false;
            Assert.That(input.Read().Move, Is.EqualTo(Vector2.zero));
            pause.OnPointerDown(Pointer(3, Vector2.zero)); pause.enabled = false;
            Assert.That(input.Read().PausePressed, Is.False);
            quack.OnPointerDown(Pointer(4, Vector2.zero)); root.SetActive(false);
            input.SetMove(Vector2.one); input.SetPause(true);
            AssertNeutral(input.Read()); root.SetActive(true); AssertNeutral(input.Read());
            quack.OnPointerDown(Pointer(5, Vector2.zero)); Assert.That(input.Read().QuackPressed, Is.True);
        }

        [TestCase("OnApplicationFocus", false)] [TestCase("OnApplicationPause", true)]
        public void FocusOrSuspensionDiscardsPendingInput(string message, bool value)
        {
            input.SetMove(Vector2.one); input.SetQuack(true); input.SetPause(true);
            input.SendMessage(message, value); AssertNeutral(input.Read());
        }

        private PointerEventData Pointer(int id, Vector2 local) => new PointerEventData(events) { pointerId = id, position = ScreenPoint(local), button = PointerEventData.InputButton.Left };
        private Vector2 ScreenPoint(Vector2 local) => RectTransformUtility.WorldToScreenPoint(null, stick.TransformPoint(local));
        private static RectTransform Rect(string name, Transform parent, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f); rect.sizeDelta = size; return rect;
        }
        private static void AssertNeutral(PlayerCommand command)
        { Assert.That(command.Move, Is.EqualTo(Vector2.zero)); Assert.That(command.QuackPressed || command.PausePressed, Is.False); Assert.That(command.Source, Is.EqualTo(InputSource.Touch)); }
    }
}
