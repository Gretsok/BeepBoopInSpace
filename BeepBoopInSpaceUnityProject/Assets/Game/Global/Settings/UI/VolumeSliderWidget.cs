using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Global.Settings.UI
{
    public class VolumeSliderWidget : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text m_title;
        [SerializeField]
        private Slider m_slider;
        
        public delegate void DVolumeSliderValueChangedDelegate(float value);
        private DVolumeSliderValueChangedDelegate m_volumeSliderValueChangedDelegate;
        
        public void Initialize(float startingValue, DVolumeSliderValueChangedDelegate volumeSliderValueChangedDelegate)
        {
            m_slider.maxValue = 100f;
            m_slider.minValue = 0f;
            m_slider.value = startingValue;

            m_slider.onValueChanged.RemoveListener(HandleSliderValueChanged);
            m_slider.onValueChanged.AddListener(HandleSliderValueChanged);
            
            m_volumeSliderValueChangedDelegate = volumeSliderValueChangedDelegate;
        }

        private void HandleSliderValueChanged(float value)
        {
            m_volumeSliderValueChangedDelegate?.Invoke(value);
        }
    }
}
