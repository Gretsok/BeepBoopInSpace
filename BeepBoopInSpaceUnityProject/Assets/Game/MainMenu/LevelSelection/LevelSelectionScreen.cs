using System;
using System.Collections.Generic;
using Game.Gameplay.Levels._0_Core;
using UnityEngine;

namespace Game.MainMenu.LevelSelection
{
    public class LevelSelectionScreen : AMainMenuScreen
    {
        [SerializeField]
        private LevelWidget m_levelWidgetPrefab;
        [SerializeField]
        private LevelsListDataAsset m_levelsListDataAsset;
        
        [SerializeField]
        private Transform m_levelsContainer;

        private readonly List<LevelWidget> m_instantiatedWidgets = new();
        public IReadOnlyList<LevelWidget> LevelWidgets => m_instantiatedWidgets;
        
        public Action<LevelDataAsset> OnLevelSelected;
        
        protected override void HandleActivation()
        {
            base.HandleActivation();

            for (int i = m_levelsContainer.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                {
                    Destroy(m_levelsContainer.GetChild(i).gameObject);
                }
                else
                {
                    DestroyImmediate(m_levelsContainer.GetChild(i).gameObject);
                }
            }

            m_instantiatedWidgets.Clear();
            for (int i = 0; i < m_levelsListDataAsset.LevelDataList.Count; i++)
            {
                LevelDataAsset levelDataAsset = m_levelsListDataAsset.LevelDataList[i];
                
                var newWidget = Instantiate(m_levelWidgetPrefab, m_levelsContainer);
                newWidget.Initialize(levelDataAsset);
                m_instantiatedWidgets.Add(newWidget);

                newWidget.OnLevelSelected += HandleLevelSelected;
            }
        }

        private void HandleLevelSelected(LevelDataAsset obj)
        {
            OnLevelSelected?.Invoke(obj);
        }
    }
}
