using System;
using Game.Gameplay.Levels._0_Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.MainMenu.LevelSelection
{
    public class LevelWidget : Selectable,
        ISubmitHandler
    {
        [SerializeField]
        private Transform m_thumbnailContainer;
        [SerializeField]
        private Image m_thumbnailImage;
        [SerializeField]
        private TMP_Text m_levelNameText;

        [SerializeField]
        private GameObject m_selectedGO;
        public Action<LevelDataAsset> OnLevelSelected;
        public LevelDataAsset LevelDataAsset { get; private set; }

        public void Initialize(LevelDataAsset levelDataAsset)
        {
            m_selectedGO.SetActive(false);
            
            LevelDataAsset = levelDataAsset;
            m_thumbnailImage.sprite = LevelDataAsset.Thumbnail;
            m_levelNameText.text = LevelDataAsset.NameKey;
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            m_selectedGO.SetActive(true);
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            base.OnDeselect(eventData);
            m_selectedGO.SetActive(false);
        }

        public void OnSubmit(BaseEventData eventData)
        {
            OnLevelSelected?.Invoke(LevelDataAsset);
        }
    }
}
