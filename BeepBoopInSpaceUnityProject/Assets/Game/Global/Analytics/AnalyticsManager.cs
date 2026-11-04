using System;
using System.Linq;
using GameAnalyticsSDK;
using UnityEngine;

namespace Game.Global.Analytics
{
    public class AnalyticsManager : MonoBehaviour
    {
        [field: SerializeField]
        public GameAnalytics GameAnalytics { get; private set; }
        
        // GameAnalytics sends nothing from the editor, and warns about it at each start: only builds talk to it, but not
        // the copies the tests launch (-no-analytics).
        private static bool IsEnabled => !Application.isEditor && !Environment.GetCommandLineArgs().Contains("-no-analytics");

        public void Initialize()
        {
            // GameAnalytics keeps its object across scenes itself, which Unity only allows for a root object: it leaves
            // the context before its Awake (the context wakes up first).
            GameAnalytics.transform.SetParent(null, false);
            DontDestroyOnLoad(GameAnalytics.gameObject);

            if (!IsEnabled)
                return;
            // We may need to set up a custom user id to link it to some other data. => GameAnalytics.SetCustomId("customId");
            GameAnalytics.Initialize();
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
