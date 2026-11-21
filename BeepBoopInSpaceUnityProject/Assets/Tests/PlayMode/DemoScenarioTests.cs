using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static Game.Tests.PlayMode.ScenarioDriver;

namespace Game.Tests.PlayMode
{
    /// <summary>What a public demo needs: the language, the question about statistics, the gamepads of everybody.</summary>
    public partial class GameModesScenarioTests
    {
        private const string k_LocType = "Game.Global.Localization.Loc, Assembly-CSharp";
        private const string k_HomeScreen = "Game.MainMenu.Home.HomeScreen";
        private const string k_SettingsScreen = "Game.MainMenu.SettingsScreen.MainMenuSettingsScreen";
        private const string k_AnalyticsManager = "Game.Global.Analytics.AnalyticsManager";

        private Gamepad m_otherGamepad;

        // The scenarios read French texts; SCENARIO_LANGUAGE=English plays them in English, for the screenshots.
        private static void SetTestsLanguage()
        {
            var isEnglish = Environment.GetEnvironmentVariable("SCENARIO_LANGUAGE") == "English";
            SetLanguage(isEnglish ? 1 : 0);
        }

        private static string InTestsLanguage(string french, string english)
        {
            return Environment.GetEnvironmentVariable("SCENARIO_LANGUAGE") == "English" ? english : french;
        }

        private static void SetLanguage(int language)
        {
            var loc = Type.GetType(k_LocType);
            var languageType = loc.Assembly.GetType("Game.Global.Localization.ELanguage");
            loc.GetMethod("SetLanguage").Invoke(null, new[] { Enum.ToObject(languageType, language) });
        }

        private void RemoveOtherGamepad()
        {
            if (m_otherGamepad != null && m_otherGamepad.added)
                InputSystem.RemoveDevice(m_otherGamepad);
            m_otherGamepad = null;
        }

        /// <summary>The settings' language button: everything on the screen changes at once, and the next screens follow.</summary>
        [UnityTest, Order(3), Timeout(5 * 60 * 1000)]
        public IEnumerator Language_SettingsSwitchTheScreensToEnglish()
        {
            LogAssert.ignoreFailingMessages = true;
            StartRenderingToTextures();
            yield return RealSeconds(1f);

            m_driver.Step("Settings: the language button");
            yield return m_driver.Select(ScreenElement(k_HomeScreen, "settings-button"));
            yield return m_driver.Submit();
            yield return m_driver.WaitUntil("the settings", () => MenuState == "MainMenuSettingsState");
            var languageButton = (Button)ScreenElement(k_SettingsScreen, "language-button");
            StringAssert.Contains("Français", languageButton.text);
            var masterVolumeValue = ScreenElement(k_SettingsScreen, "master-volume").Q<Label>(className: "ui-volume-row__value");
            var shownVolume = masterVolumeValue.text;
            Assert.IsNotEmpty(shownVolume, "The settings show no volume.");
            yield return m_driver.Select(languageButton);
            yield return m_driver.Submit();
            StringAssert.Contains("English", languageButton.text);
            var masterVolume = ScreenElement(k_SettingsScreen, "master-volume").Q<Label>("label");
            Assert.AreEqual("Master volume", masterVolume.text, "The settings' own texts didn't change.");
            Assert.AreEqual(shownVolume, masterVolumeValue.text, "Changing the language emptied the volumes.");
            yield return RealSeconds(0.3f);
            yield return CaptureScreen("34-settings-english");

            m_driver.Step("Back home, in English");
            yield return m_driver.Press(m_driver.MainPad.buttonEast);
            yield return m_driver.WaitUntil("the home screen", () => MenuState == "HomeState");
            yield return Frames(3);
            Assert.AreEqual("Play", ((Button)ScreenElement(k_HomeScreen, "play-button")).text, "The home screen stayed in French.");
            yield return CaptureScreen("35-home-english");

            m_driver.Step("Play, in English: local or online");
            yield return m_driver.Select(ScreenElement(k_HomeScreen, "play-button"));
            yield return m_driver.Submit();
            yield return m_driver.WaitUntil("the choice between local and online", () => IsOnLocalOrOnline && FocusedElement != null);
            Assert.AreEqual("Local", ScreenElement(k_LocalOrOnlineScreen, "local-button").Q<Label>(className: "ui-tile__label").text,
                "The choice between local and online stayed in French.");
            yield return RealSeconds(0.3f);
            yield return CaptureScreen("35b-local-or-online-english");
            yield return m_driver.Press(m_driver.MainPad.buttonEast);
            yield return m_driver.WaitUntil("the home screen", () => MenuState == "HomeState");

            m_driver.Step("Back to French, saved as such");
            yield return m_driver.Select(ScreenElement(k_HomeScreen, "settings-button"));
            yield return m_driver.Submit();
            yield return m_driver.WaitUntil("the settings", () => MenuState == "MainMenuSettingsState");
            languageButton = (Button)ScreenElement(k_SettingsScreen, "language-button");
            yield return m_driver.Select(languageButton);
            yield return m_driver.Submit();
            StringAssert.Contains("Français", languageButton.text);
            yield return m_driver.Press(m_driver.MainPad.buttonEast);
            yield return m_driver.WaitUntil("the home screen", () => MenuState == "HomeState");

            m_driver.AssertNoErrors();
        }

        /// <summary>
        /// The first launch of a copy that can send statistics asks once, on the home screen. No sets the answer, the
        /// settings change it. Nothing is sent from the editor either way: the SDK's object stays asleep.
        /// </summary>
        [UnityTest, Order(3), Timeout(5 * 60 * 1000)]
        public IEnumerator Statistics_AskedOnceThenChangedInTheSettings()
        {
            LogAssert.ignoreFailingMessages = true;
            var managerType = Type.GetType($"{k_AnalyticsManager}, Assembly-CSharp");
            var profile = Get(Get(Manager("Game.Global.GlobalContext"), "SaveManager"), "Profile");
            var consentField = profile.GetType().GetField("AnalyticsConsent");
            var previousConsent = consentField.GetValue(profile);
            var sdk = (Component)Get(Get(Manager("Game.Global.GlobalContext"), "AnalyticsManager"), "GameAnalytics");
            var forceAvailable = managerType.GetProperty("ForceAvailableInTheEditor");
            forceAvailable.SetValue(null, true);
            consentField.SetValue(profile, 0);
            try
            {
                StartRenderingToTextures();
                yield return RealSeconds(1f);

                m_driver.Step("Home again: the question");
                yield return m_driver.Select(ScreenElement(k_HomeScreen, "credits-button"));
                yield return m_driver.Submit();
                yield return m_driver.WaitUntil("the credits", () => MenuState == "CreditsState");
                yield return RealSeconds(0.3f);
                yield return CaptureScreen("36-credits");
                yield return m_driver.Press(m_driver.MainPad.buttonEast);
                yield return m_driver.WaitUntil("the home screen", () => MenuState == "HomeState");
                yield return m_driver.WaitUntil("the question, on No", () => NameOf(FocusedElement) == "cancel-button", 5f);
                var question = ScreenElement(k_HomeScreen, "consent").Q<Label>("message");
                foreach (var collected in new[] { InTestsLanguage("identifiant aléatoire", "random identifier"), InTestsLanguage("adresse IP", "IP address") })
                    StringAssert.Contains(collected, question.text);
                Assert.IsFalse(sdk.gameObject.activeSelf, "The SDK's object lives without the player's agreement.");
                yield return RealSeconds(0.5f);
                yield return CaptureScreen("37-statistics-question");

                m_driver.Step("No: nothing asked again");
                yield return m_driver.Press(m_driver.MainPad.buttonEast);
                yield return m_driver.WaitUntil("the answer", () => (int)consentField.GetValue(profile) == -1, 5f);

                m_driver.Step("The settings change the answer");
                yield return RealSeconds(0.3f);
                yield return m_driver.Select(ScreenElement(k_HomeScreen, "settings-button"));
                yield return m_driver.Submit();
                yield return m_driver.WaitUntil("the settings", () => MenuState == "MainMenuSettingsState");
                yield return RealSeconds(0.3f);
                yield return CaptureScreen("38-settings-options");
                var statistics = (Button)ScreenElement(k_SettingsScreen, "statistics-button");
                StringAssert.Contains(InTestsLanguage("non", "off"), statistics.text);
                yield return m_driver.Select(statistics);
                yield return m_driver.Submit();
                StringAssert.Contains(InTestsLanguage("oui", "on"), statistics.text);
                Assert.AreEqual(1, (int)consentField.GetValue(profile), "The settings' button didn't give the consent.");
                yield return m_driver.Press(m_driver.MainPad.buttonEast);
                yield return m_driver.WaitUntil("the home screen", () => MenuState == "HomeState");
                yield return RealSeconds(0.5f);
                Assert.AreNotEqual("cancel-button", NameOf(FocusedElement), "The question came back after an answer.");
            }
            finally
            {
                forceAvailable.SetValue(null, false);
                consentField.SetValue(profile, previousConsent);
            }

            m_driver.AssertNoErrors();
        }

        /// <summary>A gamepad that is neither an Xbox nor a PlayStation one (a Switch controller...): it joins and gets its scheme.</summary>
        [UnityTest, Order(3), Timeout(5 * 60 * 1000)]
        public IEnumerator Controls_AnotherGamepadJoinsAndGetsReady()
        {
            LogAssert.ignoreFailingMessages = true;
            m_otherGamepad = InputSystem.AddDevice<Gamepad>("Scenario Other");

            yield return ChooseFromHome(online: false);

            m_driver.Step("The gamepad joins and gets ready");
            yield return m_driver.Press(m_otherGamepad.buttonSouth);
            yield return m_driver.WaitUntil("the player to join", () => PlayerCount == 1, 10f);
            yield return RealSeconds(0.5f);
            Assert.AreEqual("OtherGamepad", PlayerInput.all[0].currentControlScheme, "The controller has no control scheme.");
            yield return m_driver.Press(m_otherGamepad.buttonNorth);
            yield return m_driver.WaitUntil("the hub", () => MenuState == "HubState", 10f);

            m_driver.AssertNoErrors();
        }
    }
}
