using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Game.Global.Save.Serialization
{
    public class PCSerializer : ASerializer
    {
        private static string SaveFilePath => Path.Combine(Application.persistentDataPath, "saveProfile.sav");

        public override void SaveProfile(SaveProfile profile, Action<ERequestResult> onCompleted = null)
        {
            var saveFilePath = SaveFilePath;
            var fileContent = JsonUtility.ToJson(profile, true);
            var bytes = Encoding.ASCII.GetBytes(fileContent);

            try
            {
                // Written aside then swapped in: a crash or a power cut mid-write leaves the previous save whole.
                var tempFilePath = saveFilePath + ".tmp";
                File.WriteAllBytes(tempFilePath, bytes);
                if (File.Exists(saveFilePath))
                    File.Replace(tempFilePath, saveFilePath, null);
                else
                    File.Move(tempFilePath, saveFilePath);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Debug.LogWarning($"Couldn't save the profile to {saveFilePath}: {exception.Message}");
                onCompleted?.Invoke(ERequestResult.Failed);
                return;
            }

            onCompleted?.Invoke(ERequestResult.Success);
        }

        public override void LoadProfile(Action<ERequestResult, SaveProfile> onResult = null)
        {
            var saveFilePath = SaveFilePath;

            if (!File.Exists(saveFilePath))
            {
                onResult?.Invoke(ERequestResult.Success, new SaveProfile());
                return;
            }

            SaveProfile profile = null;
            try
            {
                var fileContent = Encoding.ASCII.GetString(File.ReadAllBytes(saveFilePath));
                profile = JsonUtility.FromJson<SaveProfile>(fileContent);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                Debug.LogWarning($"Couldn't read the profile at {saveFilePath}, starting from the default one: {exception.Message}");
            }

            // An unreadable or empty file must not keep the game from starting: the default settings instead.
            onResult?.Invoke(profile != null ? ERequestResult.Success : ERequestResult.Failed, profile ?? new SaveProfile());
        }
    }
}
