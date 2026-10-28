using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.UITools.Screens
{
    /// <summary>
    /// A volume of the settings and the pause menu, from the VolumeRow UXML: a 0-100 slider and its value. Left and
    /// right change the volume right away (Unity's sliders wait for a Submit first), up and down move to the other
    /// rows; the row shows the selection of its slider.
    /// </summary>
    public class VolumeRow
    {
        private const int k_Step = 5;
        private const string k_RowClass = "ui-volume-row";
        private const string k_FocusedClass = "ui-volume-row--focused";
        private const string k_ValueClass = "ui-volume-row__value";

        private readonly Label m_value;

        public SliderInt Slider { get; }

        /// <param name="element">The row, or the instance of the VolumeRow template that holds it.</param>
        /// <param name="onVolumeChanged">Called with the new volume when the player changes it.</param>
        public VolumeRow(VisualElement element, Action<int> onVolumeChanged)
        {
            var row = element.ClassListContains(k_RowClass) ? element : element.Q(className: k_RowClass);
            Slider = row.Q<SliderInt>();
            m_value = row.Q<Label>(className: k_ValueClass);

            Slider.RegisterValueChangedCallback(evt =>
            {
                m_value.text = evt.newValue.ToString();
                onVolumeChanged(evt.newValue);
            });

            Slider.RegisterCallback<NavigationMoveEvent>(evt =>
            {
                var step = evt.direction switch
                {
                    NavigationMoveEvent.Direction.Left => -k_Step,
                    NavigationMoveEvent.Direction.Right => k_Step,
                    _ => 0,
                };
                if (step == 0)
                    return;
                Slider.value = Mathf.Clamp(Slider.value + step, Slider.lowValue, Slider.highValue);
                evt.StopPropagation();
                Slider.focusController?.IgnoreEvent(evt);
            }, TrickleDown.TrickleDown);
            Slider.RegisterCallback<NavigationSubmitEvent>(evt => evt.StopPropagation(), TrickleDown.TrickleDown);

            row.RegisterCallback<FocusInEvent>(_ => row.AddToClassList(k_FocusedClass));
            row.RegisterCallback<FocusOutEvent>(_ => row.RemoveFromClassList(k_FocusedClass));
        }

        /// <summary>Shows a volume without calling back, for the value saved in the settings.</summary>
        public void SetVolume(int volume)
        {
            Slider.SetValueWithoutNotify(volume);
            m_value.text = volume.ToString();
        }
    }
}
