using System;

namespace Game.Global.Save
{
    [Serializable]
    public class SaveProfile
    {
        public int MasterVolume = 100;
        public int MusicVolume = 100;
        public int SfxVolume = 100;
        // ELanguage; -1: the system's, until the player chooses.
        public int Language = -1;
        // 1: the player accepts the usage statistics, -1: refuses, 0: hasn't been asked yet.
        public int AnalyticsConsent = 0;
    }
}
