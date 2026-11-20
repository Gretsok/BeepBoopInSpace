using System;
using System.Linq;
using GameAnalyticsSDK;
using Game.Global.Save;
using UnityEngine;

namespace Game.Global.Analytics
{
    public class AnalyticsManager : MonoBehaviour
    {
        [field: SerializeField]
        public GameAnalytics GameAnalytics { get; private set; }
        
        private const int k_ConsentGiven = 1;
        private const int k_ConsentRefused = -1;

        // The scenario tests set it to see the question, which the editor never asks.
        public static bool ForceAvailableInTheEditor { get; set; }

        // GameAnalytics sends nothing from the editor, and warns about it at each start: only builds talk to it, but not
        // the copies the tests launch (-no-analytics).
        private static bool IsBuildSending => !Application.isEditor && !Environment.GetCommandLineArgs().Contains("-no-analytics");

        /// <summary>This copy of the game can send statistics, so it asks the player.</summary>
        public bool IsAvailable => IsBuildSending || ForceAvailableInTheEditor;

        private SaveManager m_saveManager;
        private bool m_hasStarted;

        public bool HasToAskConsent => IsAvailable && m_saveManager.Profile.AnalyticsConsent == 0;
        public bool IsConsentGiven => m_saveManager.Profile.AnalyticsConsent == k_ConsentGiven;

        // Nothing leaves this machine before the player said yes, nor after a no.
        private bool IsEnabled => IsBuildSending && IsConsentGiven;

        public void SetConsent(bool isGiven)
        {
            m_saveManager.Profile.AnalyticsConsent = isGiven ? k_ConsentGiven : k_ConsentRefused;
            if (!IsBuildSending)
                return;
            if (isGiven)
                StartSendingOnce();
            else if (m_hasStarted)
            {
                GameAnalytics.SettingsGA.SubmitErrors = false;
                GameAnalytics.SetEnabledEventSubmission(false);
            }
        }

        private void StartSendingOnce()
        {
            if (m_hasStarted)
            {
                GameAnalytics.SettingsGA.SubmitErrors = true;
                GameAnalytics.SetEnabledEventSubmission(true);
                return;
            }
            m_hasStarted = true;
            GameAnalytics.gameObject.SetActive(true);
            GameAnalytics.SettingsGA.SubmitErrors = true;
            GameAnalytics.SetBuildAllPlatforms(Application.version);
            // We may need to set up a custom user id to link it to some other data. => GameAnalytics.SetCustomId("customId");
            GameAnalytics.Initialize();
        }

        /// <summary>Once the save is loaded: what the player answered.</summary>
        public void SetSaveManager(SaveManager saveManager)
        {
            m_saveManager = saveManager;
            if (IsEnabled)
                StartSendingOnce();
        }

        public void Initialize()
        {
            // GameAnalytics keeps its object across scenes itself, which Unity only allows for a root object: it leaves
            // the context before its Awake (the context wakes up first).
            GameAnalytics.transform.SetParent(null, false);
            DontDestroyOnLoad(GameAnalytics.gameObject);

            // The SDK's object acts as soon as it lives, even if the SDK never started: it submits the logged errors, and when
            // the game quits it calls its native library and holds the exit for 1.5 s. Not until the player agrees.
            GameAnalytics.gameObject.SetActive(false);
        }


        public void NotifyLevelStarted(string levelID)
        {
            if (!IsEnabled)
                return;
            try
            {
                GameAnalytics.NewProgressionEvent(GAProgressionStatus.Start, levelID);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }

        public void NotifyLevelFinished(string levelID)
        {
            if (!IsEnabled)
                return;
            try
            {
                GameAnalytics.NewProgressionEvent(GAProgressionStatus.Complete, levelID);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }

        public void NotifyLevelCanceled(string levelID)
        {
            if (!IsEnabled)
                return;
            try
            {
                GameAnalytics.NewProgressionEvent(GAProgressionStatus.Fail, levelID);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }
    }
}
