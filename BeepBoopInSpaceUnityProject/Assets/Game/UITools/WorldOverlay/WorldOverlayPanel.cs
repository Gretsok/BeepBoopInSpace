using DG.Tweening;
using Game.UITools.Screens;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.UITools.WorldOverlay
{
    /// <summary>Texts over points of the 3D world, drawn above the game: the "+1" of a point scored.</summary>
    public class WorldOverlayPanel : AUIToolkitScreen
    {
        private const string k_AnchorClass = "floating-text";
        private const string k_LabelClasses = "ui-text floating-text__label";

        [SerializeField, Tooltip("How far a floating text rises, in pixels of the 1920x1080 screen.")]
        private Vector2 m_floatingTextMove = new(-10f, -40f);
        [SerializeField]
        private float m_floatingTextDuration = 1f;
        [SerializeField, Range(0f, 1f)]
        private float m_fadeStartRatio = 0.8f;

        /// <summary>Shows a text over the world position, rising then fading out.</summary>
        public void PlayFloatingText(string text, Color color, Vector3 worldPosition)
        {
            var camera = Camera.main;
            if (!IsShown || !camera || Root.panel == null)
                return;

            // The anchor moves; its label stays centered on it.
            var anchor = new VisualElement { pickingMode = PickingMode.Ignore };
            anchor.AddToClassList(k_AnchorClass);
            var position = RuntimePanelUtils.CameraTransformWorldToPanel(Root.panel, worldPosition, camera);
            anchor.style.left = position.x;
            anchor.style.top = position.y;

            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            foreach (var className in k_LabelClasses.Split(' '))
                label.AddToClassList(className);
            label.style.color = color;
            anchor.Add(label);
            Query<VisualElement>("world-overlay").Add(anchor);

            anchor.TweenTranslate(m_floatingTextMove, m_floatingTextDuration)
                .SetEase(Ease.OutCubic)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnKill(anchor.RemoveFromHierarchy);
            anchor.TweenOpacity(0f, m_floatingTextDuration * (1f - m_fadeStartRatio))
                .SetDelay(m_fadeStartRatio * m_floatingTextDuration)
                .SetEase(Ease.OutCubic)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }
    }
}
