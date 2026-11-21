using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Game.Gameplay.GameModes.ObjectiveManagement.PointsRushObjectiveManagement
{
    public class FloatingPointWidget : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text m_text;
        [SerializeField]
        private Vector3 m_moveOffset = new Vector3(-10f, 40f, 0f);
        [SerializeField]
        private float m_tweenDuration = 1f;

        [SerializeField] private float m_fadeStartRatio = 0.8f;

        public async UniTask SetPointAndPlayTween(string text, Color color)
        {
            Debug.Log($"PointWidget SetPointAndPlayTween called with text: {text} and color: {color}");
            m_text.text = text;
            m_text.color = color;

            m_text.transform.DOLocalMove(m_moveOffset, m_tweenDuration).SetEase(Ease.OutCubic);
            m_text.DOFade(0f, m_tweenDuration * (1f - m_fadeStartRatio))
                .SetDelay(m_fadeStartRatio * m_tweenDuration)
                .SetEase(Ease.OutCubic);
            
            await UniTask.WaitForSeconds(m_tweenDuration);

            await UniTask.WaitForEndOfFrame();
            
            Debug.Log($"PointWidget SetPointAndPlayTween completed");
            Destroy(gameObject);
        }
    }
}