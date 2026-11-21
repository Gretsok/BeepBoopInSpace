using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XInput;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// Plays the game like four local players would: virtual gamepads, menu navigation with the d-pad, and flow observation.
    /// Game code lives in Assembly-CSharp, which test assemblies can't reference, so game state is read by reflection.
    /// </summary>
    public class ScenarioDriver
    {
        public const float k_DefaultTimeout = 120f; // real seconds

        public readonly List<Gamepad> Gamepads = new();
        public Gamepad MainPad => Gamepads[0];

        private readonly List<string> m_errors = new();
        private readonly Dictionary<string, int> m_knownErrorCounts = new();
        private string[] m_knownErrors = Array.Empty<string>();
        private string m_step = "Setup";

        #region Devices

        // Pad 1 is an Xbox pad: it is the main player, so it drives the UI with the d-pad.
        public void AddGamepads()
        {
            Gamepads.Add(InputSystem.AddDevice<XInputController>("Scenario Xbox 1"));
            Gamepads.Add(InputSystem.AddDevice<DualShock4GamepadHID>("Scenario PS 2"));
            Gamepads.Add(InputSystem.AddDevice<XInputController>("Scenario Xbox 3"));
            Gamepads.Add(InputSystem.AddDevice<DualShock4GamepadHID>("Scenario PS 4"));
        }

        public void RemoveGamepads()
        {
            foreach (var gamepad in Gamepads)
            {
                if (gamepad.added)
                    InputSystem.RemoveDevice(gamepad);
            }
            Gamepads.Clear();
        }

        // Writes into a copy of the device state, so it works whatever the device's memory format is.
        public static void Set(ButtonControl button, float value)
        {
            using (StateEvent.From(button.device, out var eventPtr))
            {
                button.WriteValueIntoEvent(value, eventPtr);
                InputSystem.QueueEvent(eventPtr);
            }
        }

        public IEnumerator Press(ButtonControl button, int holdFrames = 2)
        {
            Set(button, 1f);
            yield return Frames(holdFrames);
            Set(button, 0f);
            yield return Frames(2);
        }

        public IEnumerator Submit() => Press(MainPad.buttonSouth);

        public Keyboard Keyboard { get; private set; }
        public Mouse Mouse { get; private set; }

        private InputSettings.BackgroundBehavior m_backgroundBehavior;
#if UNITY_EDITOR
        private InputSettings.EditorInputBehaviorInPlayMode m_editorInputBehavior;
#endif

        // Keyboard and mouse drive the menus until a player joins; then only the main player's devices do.
        public void AddKeyboardAndMouse()
        {
            // A batch mode application never has the focus: keep keyboard and mouse enabled anyway.
            m_backgroundBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            // By default the editor gives keyboard and mouse input to the game only while the Game view has the focus,
            // and batch mode has no Game view.
            m_editorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            Keyboard = InputSystem.AddDevice<Keyboard>("Scenario Keyboard");
            Mouse = InputSystem.AddDevice<Mouse>("Scenario Mouse");
        }

        public void RemoveKeyboardAndMouse()
        {
            if (Keyboard == null)
                return;
            if (Keyboard.added)
                InputSystem.RemoveDevice(Keyboard);
            if (Mouse.added)
                InputSystem.RemoveDevice(Mouse);
            Keyboard = null;
            Mouse = null;
            InputSystem.settings.backgroundBehavior = m_backgroundBehavior;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = m_editorInputBehavior;
#endif
        }

        /// <summary>Moves the virtual mouse over the middle of <paramref name="element"/>.</summary>
        public IEnumerator PointAt(VisualElement element) => PointAt(element.panel, element.worldBound.center);

        /// <summary>Moves the virtual mouse to a position of the panel (1920 wide, y growing downward).</summary>
        public IEnumerator PointAt(IPanel panel, Vector2 panelPosition)
        {
            // Screen coordinates are in pixels and grow upward from the bottom left.
            var scale = Screen.width / panel.visualTree.layout.width;
            var position = new Vector2(panelPosition.x * scale, Screen.height - panelPosition.y * scale);
            InputSystem.QueueStateEvent(Mouse, new MouseState { position = position });
            yield return Frames(3);
        }

        public IEnumerator Click()
        {
            var position = Mouse.position.ReadValue();
            // WithButton changes the state it's called on: the release needs a state of its own.
            InputSystem.QueueStateEvent(Mouse, new MouseState { position = position }.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left));
            yield return Frames(2);
            InputSystem.QueueStateEvent(Mouse, new MouseState { position = position });
            yield return Frames(2);
        }

        #endregion

        #region Waiting

        public static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++)
                yield return null;
        }

        public static IEnumerator RealSeconds(float seconds)
        {
            var end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end)
                yield return null;
        }

        public IEnumerator WaitUntil(string what, Func<bool> condition, float timeout = k_DefaultTimeout)
        {
            var start = Time.realtimeSinceStartup;
            while (!condition())
            {
                if (Time.realtimeSinceStartup - start > timeout)
                    Fail($"Timed out after {timeout:0}s waiting for {what}.");
                yield return null;
            }
        }

        public void Fail(string message)
        {
            Assert.Fail($"[{m_step}] {message}\n{DescribeGame()}{ErrorsReport()}");
        }

        #endregion

        #region Steps and errors

        public void Step(string name)
        {
            m_step = name;
            Debug.Log($"[Scenario] {name}");
        }

        /// <param name="knownErrors">Message prefixes that are counted and reported instead of failing the scenario.</param>
        public void BeginTest(string[] knownErrors)
        {
            lock (m_errors)
            {
                m_errors.Clear();
                m_knownErrorCounts.Clear();
                m_knownErrors = knownErrors;
            }
            m_step = "Setup";
            Application.logMessageReceivedThreaded += HandleLog;
        }

        public void EndTest()
        {
            Application.logMessageReceivedThreaded -= HandleLog;
        }

        private void HandleLog(string message, string stackTrace, LogType type)
        {
            if (type is not (LogType.Error or LogType.Exception or LogType.Assert))
                return;

            lock (m_errors)
            {
                var known = m_knownErrors.FirstOrDefault(prefix => message.StartsWith(prefix, StringComparison.Ordinal));
                if (known != null)
                    m_knownErrorCounts[known] = m_knownErrorCounts.GetValueOrDefault(known) + 1;
                else
                    m_errors.Add($"[{m_step}] {type}: {message}\n{stackTrace}");
            }
        }

        public void AssertNoErrors()
        {
            lock (m_errors)
            {
                foreach (var (message, count) in m_knownErrorCounts)
                    Debug.LogWarning($"[Scenario] Known issue logged {count} time(s): {message}");
                Assert.IsEmpty(m_errors, $"{m_errors.Count} error(s) logged during the scenario:{ErrorsReport()}");
            }
        }

        private string ErrorsReport()
        {
            lock (m_errors)
                return m_errors.Count == 0 ? "" : "\n" + string.Join("\n----\n", m_errors.Distinct());
        }

        #endregion

        #region UI navigation

        private const int k_MaxNavigationPresses = 20;

        /// <summary>The focused element of the UI Toolkit menus: the selection that gamepads, keyboard and mouse share.</summary>
        public static Focusable FocusedElement
        {
            get
            {
                foreach (var document in Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Exclude))
                {
                    var focused = document.rootVisualElement?.focusController?.focusedElement;
                    if (focused != null)
                        return focused;
                }
                return null;
            }
        }

        public static string NameOf(Focusable focusable) => focusable is VisualElement element ? element.name : null;

        private static readonly MoveDirection[] s_directions = { MoveDirection.Up, MoveDirection.Down, MoveDirection.Left, MoveDirection.Right };

        /// <summary>
        /// Moves the focus to <paramref name="target"/> with d-pad presses. UI Toolkit navigates by position on screen, so
        /// each press goes the way the target is; a press that doesn't move the focus is retried in another direction.
        /// </summary>
        public IEnumerator Select(VisualElement target)
        {
            yield return WaitUntil($"'{target.name}' and a focused element",
                () => target.panel != null && target.canGrabFocus && FocusedElement != null);

            var tried = new HashSet<(Focusable, MoveDirection)>();
            for (int presses = 0; FocusedElement != target; presses++)
            {
                var from = FocusedElement;
                var direction = DirectionsTowards((VisualElement)from, target)
                    .Where(d => !tried.Contains((from, d))).DefaultIfEmpty(MoveDirection.None).First();
                if (direction == MoveDirection.None || presses >= k_MaxNavigationPresses)
                    Fail($"No UI navigation path from '{NameOf(from)}' to '{target.name}'.");
                tried.Add((from, direction));

                yield return Press(DpadButton(direction));
                var pressed = Time.realtimeSinceStartup;
                while (FocusedElement == from && Time.realtimeSinceStartup - pressed < 1f)
                    yield return null;
            }
        }

        // The main axis toward the target first, then the other directions.
        private static IEnumerable<MoveDirection> DirectionsTowards(VisualElement from, VisualElement to)
        {
            var delta = to.worldBound.center - from.worldBound.center; // y grows downward in panel space
            var horizontal = delta.x < 0 ? MoveDirection.Left : MoveDirection.Right;
            var vertical = delta.y < 0 ? MoveDirection.Up : MoveDirection.Down;
            var main = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) ? horizontal : vertical;
            var second = main == horizontal ? vertical : horizontal;
            return new[] { main, second }.Concat(s_directions.Where(direction => direction != main && direction != second));
        }

        public ButtonControl DpadButton(MoveDirection direction) => direction switch
        {
            MoveDirection.Up => MainPad.dpad.up,
            MoveDirection.Down => MainPad.dpad.down,
            MoveDirection.Left => MainPad.dpad.left,
            _ => MainPad.dpad.right,
        };

        /// <summary>An element of the active main menu screen of this type, by name.</summary>
        public static VisualElement ScreenElement(string screenTypeName, string elementName) =>
            FindActive(screenTypeName)?.GetComponent<UIDocument>().rootVisualElement.Q(elementName)
            ?? throw new InvalidOperationException($"No element '{elementName}' in an active {screenTypeName}.");

        #endregion

        #region Screenshots

        private static Camera s_capturedCamera;
        private static PanelSettings s_capturedPanel;
        private static RenderTexture s_sceneTexture;
        private static RenderTexture s_uiTexture;

        /// <summary>
        /// Batch mode has no game view, and its screen isn't 16:9: until <see cref="StopRenderingToTextures"/>, the main
        /// camera and the menus' panel render into 1920x1080 textures, so the menus lay out as on a player's screen.
        /// </summary>
        public static void StartRenderingToTextures()
        {
            s_capturedCamera = Camera.main;
            s_capturedPanel = Object.FindAnyObjectByType<UIDocument>().panelSettings;
            s_sceneTexture = new RenderTexture(1920, 1080, 24);
            s_uiTexture = new RenderTexture(1920, 1080, 24);
            s_capturedCamera.targetTexture = s_sceneTexture;
            s_capturedPanel.targetTexture = s_uiTexture;
            s_capturedPanel.clearColor = true;
            s_capturedPanel.colorClearValue = Color.clear;
        }

        public static void StopRenderingToTextures()
        {
            if (s_capturedPanel)
            {
                s_capturedPanel.targetTexture = null;
                s_capturedPanel.clearColor = false;
            }
            if (s_capturedCamera)
                s_capturedCamera.targetTexture = null;
            if (s_sceneTexture)
                Object.Destroy(s_sceneTexture);
            if (s_uiTexture)
                Object.Destroy(s_uiTexture);
            s_capturedPanel = null;
            s_capturedCamera = null;
        }

        /// <summary>Saves what the player sees to Logs/screenshots/<paramref name="fileName"/>.png (see <see cref="StartRenderingToTextures"/>).</summary>
        public static IEnumerator CaptureScreen(string fileName)
        {
            yield return Frames(2);
            var scene = ReadPixels(s_sceneTexture);
            var ui = ReadPixels(s_uiTexture);

            // The panel renders premultiplied colors: UI over scene.
            var pixels = scene.GetPixels();
            var uiPixels = ui.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                var background = pixels[i] * (1f - uiPixels[i].a);
                pixels[i] = new Color(uiPixels[i].r + background.r, uiPixels[i].g + background.g, uiPixels[i].b + background.b, 1f);
            }
            scene.SetPixels(pixels);

            var folder = System.IO.Path.Combine(Application.dataPath, "..", "Logs", "screenshots");
            System.IO.Directory.CreateDirectory(folder);
            var path = System.IO.Path.GetFullPath(System.IO.Path.Combine(folder, fileName + ".png"));
            System.IO.File.WriteAllBytes(path, scene.EncodeToPNG());
            Object.Destroy(scene);
            Object.Destroy(ui);
            Debug.Log($"[Scenario] Screenshot saved to {path}");
        }

        private static Texture2D ReadPixels(RenderTexture renderTexture)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            var texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            return texture;
        }

        #endregion

        #region Gameplay input

        /// <summary>Every pad mashes random action buttons (face buttons and shoulders) until <paramref name="stop"/> is true.</summary>
        public IEnumerator MashButtonsUntil(Func<bool> stop, System.Random random, float timeout)
        {
            var buttons = Gamepads.Select(pad => new[]
            {
                pad.buttonSouth, pad.buttonNorth, pad.buttonEast, pad.buttonWest, pad.leftShoulder, pad.rightShoulder
            }).ToList();
            var held = new ButtonControl[Gamepads.Count];
            var nextPress = new float[Gamepads.Count];
            var start = Time.realtimeSinceStartup;
            var presses = 0;

            while (!stop())
            {
                if (Time.realtimeSinceStartup - start > timeout)
                    Fail($"Timed out after {timeout:0}s waiting for the game to end.");

                var now = Time.realtimeSinceStartup;
                for (int i = 0; i < Gamepads.Count; i++)
                {
                    // One event per pad per frame: a release and a press queued together would overwrite each other.
                    if (held[i] != null)
                    {
                        Set(held[i], 0f);
                        held[i] = null;
                    }
                    else if (now >= nextPress[i])
                    {
                        held[i] = buttons[i][random.Next(buttons[i].Length)];
                        Set(held[i], 1f);
                        presses++;
                        nextPress[i] = now + 0.05f + (float)random.NextDouble() * 0.25f;
                    }
                }
                yield return null;
            }

            foreach (var button in held.Where(button => button != null))
                Set(button, 0f);
            Debug.Log($"[Scenario] {presses} random button presses");
        }

        #endregion

        #region Game state (reflection)

        public const string k_MainMenuContext = "Game.MainMenu.MainMenuContext";
        public const string k_GameplayContext = "Game.Gameplay.GameplayContext";
        public const string k_GlobalContext = "Game.Global.GlobalContext";
        public const string k_TournamentContext = "Game.Tournament.TournamentContext";
        public const string k_TournamentResultsContext = "Game.Tournament.Results.ResultsScene.TournamentResultsSceneContext";

        public static Type GameType(string fullName) =>
            Type.GetType($"{fullName}, Assembly-CSharp") ?? throw new InvalidOperationException($"Game type '{fullName}' not found.");

        public static bool IsAlive(object obj) => obj is Object unityObject ? unityObject != null : obj != null;

        /// <summary>The live instance of an <c>AManager&lt;T&gt;</c>, or null.</summary>
        public static object Manager(string typeName)
        {
            var instance = GameType(typeName)
                .GetProperty("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                ?.GetValue(null);
            return IsAlive(instance) ? instance : null;
        }

        public static bool IsInitialized(object manager) => manager != null && (bool)Get(manager, "IsInitialized");

        /// <summary>Reads a property or field, public or not, declared on the type or any base type.</summary>
        public static object Get(object target, string member)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var property = type.GetProperty(member, flags);
                if (property != null)
                    return property.GetValue(target);
                var field = type.GetField(member, flags);
                if (field != null)
                    return field.GetValue(target);
            }
            throw new MissingMemberException(target.GetType().Name, member);
        }

        public static Component FindActive(string typeName) =>
            Object.FindAnyObjectByType(GameType(typeName), FindObjectsInactive.Exclude) as Component;

        public static string CurrentStateOf(string contextTypeName)
        {
            var context = Manager(contextTypeName);
            if (context == null)
                return null;
            var state = Get(Get(context, "FlowMachine"), "CurrentState");
            return IsAlive(state) ? state.GetType().Name : null;
        }

        public static string MenuState => CurrentStateOf(k_MainMenuContext);
        public static string GameplayState => CurrentStateOf(k_GameplayContext);
        public static bool IsOnMainMenuHome => SceneManager.GetActiveScene().name == "MainMenu" && MenuState == "HomeState";

        public static int PlayerCount
        {
            get
            {
                var global = Manager(k_GlobalContext);
                return global == null ? 0 : ((ICollection)Get(Get(global, "PlayerManager"), "Players")).Count;
            }
        }

        public static string CurrentLevelName
        {
            get
            {
                var context = Manager(k_GameplayContext);
                var level = context == null ? null : Get(Get(context, "CurrentLevelInfoManager"), "CurrentLevelDataAsset") as Object;
                return level ? level.name : null;
            }
        }

        public static string Scores
        {
            get
            {
                var characters = Manager("Game.Gameplay.CharactersManagement.CharactersManager");
                if (characters == null)
                    return "-";
                var scores = ((IEnumerable)Get(characters, "CharacterPawns")).Cast<object>()
                    .Select(pawn => Get(Get(Get(pawn, "ReferencesHolder"), "ScoringController"), "Score"));
                return string.Join(", ", scores);
            }
        }

        public static string DescribeGame()
        {
            string Safe(Func<object> read)
            {
                try { return read()?.ToString() ?? "-"; }
                catch (Exception e) { return $"<{e.GetType().Name}>"; }
            }

            return $"Scene='{SceneManager.GetActiveScene().name}', menu state={Safe(() => MenuState)}, " +
                   $"game state={Safe(() => GameplayState)}, level={Safe(() => CurrentLevelName)}, " +
                   $"players={Safe(() => PlayerCount)}, focused={Safe(() => NameOf(FocusedElement))}";
        }

        #endregion
    }
}
