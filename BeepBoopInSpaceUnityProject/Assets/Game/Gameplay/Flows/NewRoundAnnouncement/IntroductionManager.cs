using System;
using DG.Tweening;
using Game.ArchitectureTools.Manager;
using Game.Gameplay.CharactersManagement;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay.Flows.NewRoundAnnouncement
{
    public class IntroductionManager : AManager<IntroductionManager>
    {
        [SerializeField]
        private CinemachineCamera m_cam01;
        [SerializeField]
        private CinemachineCamera m_cam02;

        [SerializeField]
        private Transform m_canvasPositionnerRoot;

        [SerializeField]
        private Canvas m_nameCanvas;

        private Vector3 m_defaultNameCanvasScale;

        private void Start()
        {
            m_defaultNameCanvasScale = m_nameCanvas.transform.localScale;
            m_nameCanvas.transform.localScale = new Vector3(1f, 0f, 1f) * m_defaultNameCanvasScale.x;
            IsNameCanvasHidden = true;

        }

        public void InflateCharacterPawn(CharacterPawn characterPawn)
        {
            if (!IsNameCanvasHidden)
            {
                HideCanvas(() => ShowCanvas(characterPawn));
            }
            else
            {
                ShowCanvas(characterPawn);
            }
        }

        private void ShowCanvas(CharacterPawn characterPawn)
        {
            if (!m_cam01.gameObject.activeSelf)
            {
                m_cam01.Target.TrackingTarget = characterPawn.transform;
                m_cam01.UpdateCameraState(Vector3.up, 100f);
                
                m_cam01.gameObject.SetActive(true);
                m_cam02.gameObject.SetActive(false);
            }
            else
            {
                m_cam02.Target.TrackingTarget = characterPawn.transform;
                m_cam02.UpdateCameraState(Vector3.up, 100f);

                m_cam02.gameObject.SetActive(true);
                m_cam01.gameObject.SetActive(false);
            }
            
            characterPawn.ReferencesHolder.RumbleHandler.PlayItsMeRumble();
            m_canvasPositionnerRoot.transform.position = characterPawn.transform.position;
            m_nameCanvas.GetComponent<Image>().sprite = characterPawn.ReferencesHolder.CharacterDataAsset.NameplateSprite;
            m_nameCanvas.transform.DOKill(true);
            m_nameCanvas.transform.localScale = new Vector3(1f, 0f, 1f) * m_defaultNameCanvasScale.x;
            m_nameCanvas.transform.DOScale(Vector3.one * m_defaultNameCanvasScale.x, 0.3f).SetDelay(0.2f).SetEase(Ease.InOutSine);
        }
        
        private void HideCanvas(Action onComplete)
        {
            m_nameCanvas.transform.DOKill(true);
            m_nameCanvas.transform.localScale = Vector3.one * m_defaultNameCanvasScale.x;
            m_nameCanvas.transform.DOScale(new Vector3(1f, 0f, 1f) * m_defaultNameCanvasScale.x, 0.1f)
                .SetEase(Ease.InCubic)
                .onComplete += () =>
            {
                onComplete?.Invoke();
                IsNameCanvasHidden = true;
            };
        }

        public bool IsNameCanvasHidden { get; private set; }

        public void Stop()
        {
            m_cam02.gameObject.SetActive(false);
            m_cam01.gameObject.SetActive(false);
            m_cam02.Target.TrackingTarget = null;
            m_cam01.Target.TrackingTarget = null;
            HideCanvas(null);
        }
    }
}