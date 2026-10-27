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
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// Plays the game like four local players would: virtual gamepads, UI navigation with the d-pad, and flow observation.
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

        public static GameObject CurrentSelection => EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;

        private static readonly MoveDirection[] s_directions = { MoveDirection.Up, MoveDirection.Down, MoveDirection.Left, MoveDirection.Right };

        /// <summary>Moves the UI selection to <paramref name="target"/> with d-pad presses, following Unity's navigation graph.</summary>
        public IEnumerator Select(Selectable target)
        {
            yield return WaitUntil($"'{target.name}' and a current selection",
                () => target.IsActive() && target.IsInteractable() && CurrentSelection);

            var from = CurrentSelection.GetComponent<Selectable>();
            var path = FindPath(from, target);
            if (path == null)
                Fail($"No UI navigation path from '{CurrentSelection.name}' to '{target.name}'.");

            foreach (var (direction, next) in path)
            {
                yield return Press(DpadButton(direction));
                yield return WaitUntil($"'{next.name}' to be selected after pressing {direction}",
                    () => CurrentSelection == next.gameObject, 5f);
            }
        }

        private static List<(MoveDirection, Selectable)> FindPath(Selectable from, Selectable to)
        {
            if (!from)
                return null;
            var previous = new Dictionary<Selectable, (Selectable, MoveDirection)> { [from] = (null, MoveDirection.None) };
            var queue = new Queue<Selectable>();
            queue.Enqueue(from);
            while (queue.Count > 0 && !previous.ContainsKey(to))
            {
                var current = queue.Dequeue();
                foreach (var direction in s_directions)
                {
                    var neighbour = Neighbour(current, direction);
                    if (neighbour && !previous.ContainsKey(neighbour))
                    {
                        previous[neighbour] = (current, direction);
                        queue.Enqueue(neighbour);
                    }
                }
            }
            if (!previous.ContainsKey(to))
                return null;

            var path = new List<(MoveDirection, Selectable)>();
            for (var node = to; node != from; node = previous[node].Item1)
                path.Insert(0, (previous[node].Item2, node));
            return path;
        }

        private static Selectable Neighbour(Selectable selectable, MoveDirection direction) => direction switch
        {
            MoveDirection.Up => selectable.FindSelectableOnUp(),
            MoveDirection.Down => selectable.FindSelectableOnDown(),
            MoveDirection.Left => selectable.FindSelectableOnLeft(),
            _ => selectable.FindSelectableOnRight(),
        };

        private ButtonControl DpadButton(MoveDirection direction) => direction switch
        {
            MoveDirection.Up => MainPad.dpad.up,
            MoveDirection.Down => MainPad.dpad.down,
            MoveDirection.Left => MainPad.dpad.left,
            _ => MainPad.dpad.right,
        };

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
                   $"players={Safe(() => PlayerCount)}, selected={Safe(() => CurrentSelection ? CurrentSelection.name : null)}";
        }

        #endregion
    }
}
