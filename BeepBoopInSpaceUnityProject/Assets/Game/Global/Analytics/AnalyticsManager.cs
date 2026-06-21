using System;
using GameAnalyticsSDK;
using UnityEngine;

namespace Game.Global.Analytics
{
    public class AnalyticsManager : MonoBehaviour
    {
        [field: SerializeField]
        public GameAnalytics GameAnalytics { get; private set; }
        
        public void Initialize()
        {
            // We may need to set up a custom user id to link it to some other data. => GameAnalytics.SetCustomId("customId");
            GameAnalytics.Initialize();
        }


        public async void NotifyLevelStarted(string levelID)
        {
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
