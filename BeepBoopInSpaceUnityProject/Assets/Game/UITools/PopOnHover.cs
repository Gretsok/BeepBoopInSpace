using DG.Tweening;
using Game.SFXManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UITools
{
    public class PopOnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        private Transform m_target;

        [SerializeField]
        private AudioPlayer m_popAudioPlayer;

        private void OnEnable()
        {
            m_target.localScale = Vector3.one;
        }

        private void OnDisable()
        {
            m_target.localScale = Vector3.one;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            m_target.DOKill();
            m_target.DOScale(Vector3.one * 1.1f, 0.3f).SetEase(Ease.OutBack);
            
            m_popAudioPlayer?.Play();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            m_target.DOKill();
            m_target.DOScale(Vector3.one * 1f, 0.1f).SetEase(Ease.OutSine);
        }
    }
}
