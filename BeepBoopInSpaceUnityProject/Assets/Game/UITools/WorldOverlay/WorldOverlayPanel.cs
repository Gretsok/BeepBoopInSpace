using Game.ArchitectureTools.FlowMachine;
using UnityEngine;

namespace Game.UITools.WorldOverlay
{
    public class WorldOverlayPanel : Panel
    {
        [SerializeField]
        private RectTransform m_rectTransform;

        public void AddToWorldOverlay(RectTransform element, Vector3 worldPosition)
        {
            element.SetParent(m_rectTransform, false);

            Camera camera = Camera.main;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldPosition);
            Vector2 relativeScreenPoint = new Vector2(screenPoint.x / Screen.width, screenPoint.y / Screen.height);
            element.anchorMin = relativeScreenPoint;
            element.anchorMax = relativeScreenPoint;
        }
    }
}