using System;
using System.Text;
using UnityEngine;
using UnityEngine.Windows;

namespace Game.Global.Save.Serialization
{
    public class PCSerializer : ASerializer
    {
        public override void SaveProfile(SaveProfile profile, Action<ERequestResult> onCompleted = null)
        {
            var saveFilePath = Application.persistentDataPath + "\\saveProfile.sav";
            
            var fileContent = JsonUtility.ToJson(profile, true);

            var bytes = Encoding.ASCII.GetBytes(fileContent);
            File.WriteAllBytes(saveFilePath, bytes);
            
            onCompleted?.Invoke(ERequestResult.Success);
        }

        public override void LoadProfile(Action<ERequestResult, SaveProfile> onResult = null)
        {
            var saveFilePath = Application.persistentDataPath + "\\saveProfile.sav";

            if (!File.Exists(saveFilePath))
            {
                onResult?.Invoke(ERequestResult.Success, new SaveProfile());
                return;
            }
            var bytes = File.ReadAllBytes(saveFilePath);
            var fileContent = Encoding.ASCII.GetString(bytes);

            var profile = JsonUtility.FromJson<SaveProfile>(fileContent);
            
            onResult?.Invoke(ERequestResult.Success, profile);
        }
    }
}
