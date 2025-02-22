using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay.CharactersManagement.Rumble
{
    public static class RumbleReaderUtils
    {
        private static float GetCurrentTime()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
#endif
            {
                return Time.time;
            }
#if UNITY_EDITOR
            else
            {
                return (float)UnityEditor.EditorApplication.timeSinceStartup;
            }
#endif
        }
        public static IEnumerator RumbleRoutine(Gamepad gamepad, RumbleData rumbleData, Action onComplete)
        {
            float startTime = GetCurrentTime();
 
            var duration = rumbleData.RumbleDuration;

            float elapsedTime = 0f;
            do
            {
                yield return null;
                elapsedTime = GetCurrentTime() - startTime;
                var durationRatio = elapsedTime / duration;
                gamepad.SetMotorSpeeds(
                    Mathf.Lerp(rumbleData.LowFrequenciesLimits.x, 
                        rumbleData.LowFrequenciesLimits.y, 
                        rumbleData.LowFrequenciesCurve.Evaluate(durationRatio)),
                    Mathf.Lerp(rumbleData.HighFrequenciesLimits.x, 
                        rumbleData.HighFrequenciesLimits.y, 
                        rumbleData.HighFrequenciesCurve.Evaluate(durationRatio)));
            } while (elapsedTime < duration);

            gamepad.SetMotorSpeeds(0f, 0f);
            onComplete?.Invoke();
        }
    }
}