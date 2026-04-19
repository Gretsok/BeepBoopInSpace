using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UITools
{
    [RequireComponent(typeof(ScrollRect))]
    public class ScrollingToSelectedHandler : MonoBehaviour
    {
        [SerializeField] private float _scrollSpeed = 10f;
    
        private ScrollRect _scrollRect;
        private RectTransform _content;
        private RectTransform _viewport;

        private void Awake()
        {
            _scrollRect = GetComponent<ScrollRect>();
            _content  = _scrollRect.content;
            _viewport = _scrollRect.viewport != null 
                        ? _scrollRect.viewport 
                        : (RectTransform)transform;
        }

        private void Update()
        {
            var selected = EventSystem.current?.currentSelectedGameObject;
            if (selected == null) return;

            // Vérifie que le sélectionné est bien un enfant du content
            if (!selected.transform.IsChildOf(_content)) return;

            ScrollToItem((RectTransform)selected.transform);
        }

        private void ScrollToItem(RectTransform target)
        {
            var viewportCorners = new Vector3[4];
            var targetCorners   = new Vector3[4];
            _viewport.GetWorldCorners(viewportCorners);
            target.GetWorldCorners(targetCorners);

            float viewportCenterY = (viewportCorners[0].y + viewportCorners[1].y) * 0.5f;
            float itemCenterY     = (targetCorners[0].y   + targetCorners[1].y)   * 0.5f;

            float offset = itemCenterY - viewportCenterY;

            float contentHeight = _content.rect.height - _viewport.rect.height;
            if (contentHeight <= 0f) return;

            float normalizedDelta = offset / contentHeight;
            float targetNormalized = Mathf.Clamp01(
                _scrollRect.verticalNormalizedPosition + normalizedDelta
            );

            _scrollRect.verticalNormalizedPosition = Mathf.Lerp(
                _scrollRect.verticalNormalizedPosition,
                targetNormalized,
                Time.deltaTime * _scrollSpeed
            );
        }
    }
}
