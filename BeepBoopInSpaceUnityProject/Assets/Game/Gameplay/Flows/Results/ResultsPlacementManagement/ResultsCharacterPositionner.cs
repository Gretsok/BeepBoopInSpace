using System.Collections.Generic;
using DG.Tweening;
using Game.Gameplay.CharactersManagement;
using Game.Global.SFXManagement;
using Game.SFXManagement;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.Gameplay.Flows.Results.ResultsPlacementManagement
{
    public class ResultsCharacterPositionner : MonoBehaviour
    {
        [SerializeField] 
        private TMP_Text m_scoreText;

        [SerializeField] 
        private List<Sprite> m_rankImages;
        [SerializeField] 
        private Image m_rankImage;

        [SerializeField] 
        private Transform m_modelContainer;

        [SerializeField]
        private Canvas m_nameCanvas;

        private Vector3 m_defaultNameCanvasScale;
        [SerializeField]
        private Canvas m_scoreCanvas;
        private Vector3 m_defaultScoreCanvasScale;

        [SerializeField]
        private Canvas m_rankCanvas;
        private Vector3 m_defaultRankCanvasScale;

        [SerializeField]
        private CinemachineCamera m_camera;

        [SerializeField]
        private AudioPlayer m_reactionAudioPlayer;
        
        [SerializeField]
        private AudioPlayer m_worstReactionAudioPlayer;

        [SerializeField]
        private AudioPlayer m_hologrammeAudioPlayer;
        public CharacterPawn Character { get; private set; }

        public CharacterAnimationsHandler ResultCharacter { get; private set; }

        [SerializeField]
        private UnityEvent m_onDisplayUI;
        
        private void Awake()
        {
            DOTween.Init();
            
            m_defaultNameCanvasScale = m_nameCanvas.transform.localScale;
            m_defaultScoreCanvasScale = m_scoreCanvas.transform.localScale;
            m_defaultRankCanvasScale = m_rankCanvas.transform.localScale;
            
            m_nameCanvas.transform.localScale = new Vector3(1f, 0f, 1f) * m_defaultNameCanvasScale.x;
            m_scoreCanvas.transform.localScale = new Vector3(1f, 0f, 1f) * m_defaultScoreCanvasScale.x;
            m_rankCanvas.transform.localScale = Vector3.zero;
            
            Display(false, false);
            for (int i = m_modelContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(m_modelContainer.GetChild(i).gameObject);
            }
        }

        public bool IsLast { get; private set; }
        public void InflateData(CharacterPawn pawn, int rankIndex, bool isLast)
        {
            Character = pawn;

            for (int i = m_modelContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(m_modelContainer.GetChild(i).gameObject);
            }

            ResultCharacter = Instantiate(pawn.ReferencesHolder.CharacterDataAsset.CharacterPrefab, m_modelContainer).GetComponent<CharacterAnimationsHandler>();


            m_nameCanvas.GetComponent<Image>().sprite = pawn.ReferencesHolder.CharacterDataAsset.NameplateSprite; 

            m_scoreText.text = $"{pawn.ReferencesHolder.ScoringController.Score.ToString()} Points";
            m_rankImage.sprite = m_rankImages[rankIndex];
            
            IsLast = isLast;
        }

        
        public void Display(bool displayUI, bool activateCam)
        {
            if (displayUI)
            {
                m_nameCanvas.transform.DOKill(true);
                m_nameCanvas.transform.localScale = new Vector3(1f, 0f, 1f) * m_defaultNameCanvasScale.x;
                m_nameCanvas.transform.DOScale(Vector3.one * m_defaultNameCanvasScale.x, 0.3f).SetDelay(0.2f).SetEase(Ease.InOutSine);
                m_scoreCanvas.transform.DOKill(true);
                m_nameCanvas.transform.localScale = new Vector3(1f, 0f, 1f) * m_defaultNameCanvasScale.x;
                m_scoreCanvas.transform.DOScale(Vector3.one * m_defaultScoreCanvasScale.x, 0.3f).SetDelay(0.2f).SetEase(Ease.InOutSine);
                
                m_rankCanvas.transform.DOKill(true);
                m_rankCanvas.transform.localScale = Vector3.zero;
                m_rankCanvas.transform.DOScale(Vector3.one * m_defaultRankCanvasScale.x, 0.3f).SetDelay(0.2f).SetEase(Ease.InOutSine);
                m_rankImage.transform.DOKill(true);
                m_rankImage.transform.DORotate(Vector3.forward * 360f, 0.2f, RotateMode.LocalAxisAdd).SetDelay(0.2f).SetEase(Ease.InOutSine);
                
                m_hologrammeAudioPlayer.Play();
                
                m_onDisplayUI?.Invoke();
            } 
            else
            {
                m_nameCanvas.transform.DOKill(true);
                m_nameCanvas.transform.DOScale(new Vector3(1f, 0f, 1f) * m_defaultNameCanvasScale.x, 0.1f).SetEase(Ease.InCubic);
                m_scoreCanvas.transform.DOKill(true);
                m_scoreCanvas.transform.DOScale(new Vector3(1f, 0f, 1f) * m_defaultScoreCanvasScale.x, 0.1f).SetEase(Ease.InCubic);
                
                m_rankCanvas.transform.DOKill(true);
                m_rankCanvas.transform.DOScale(Vector3.zero * m_defaultRankCanvasScale.x, 0.1f).SetEase(Ease.InCubic);
                
                m_rankImage.transform.DOKill(true);
                m_rankImage.transform.localRotation = Quaternion.identity;
            }

            if (activateCam)
            {
                if (IsLast)
                {
                    m_worstReactionAudioPlayer.Play();
                }
                else
                {
                    m_reactionAudioPlayer.Play();
                }
            }
            m_camera.gameObject.SetActive(activateCam);
        }
    }
}