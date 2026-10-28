using System;
using System.Collections.Generic;
using Game.Gameplay.Levels._0_Core;
using Game.Global.SFXManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.MainMenu.LevelSelection
{
    public class LevelSelectionScreen : AMainMenuScreen
    {
        [SerializeField]
        private VisualTreeAsset m_levelWidgetTemplate;
        [SerializeField]
        private LevelsListDataAsset m_levelsListDataAsset;

        [Header("Sounds")]
        [SerializeField]
        private AudioPlayer m_levelSelectedAudioPlayer;
        [SerializeField]
        private AudioPlayer m_backAudioPlayer;

        private readonly List<LevelWidget> m_instantiatedWidgets = new();
        public IReadOnlyList<LevelWidget> LevelWidgets => m_instantiatedWidgets;

        public Action<LevelDataAsset> OnLevelSelected;

        protected override void HandleActivation()
        {
            var levelsScrollView = Query<ScrollView>("levels");
            m_instantiatedWidgets.Clear();
            for (int i = 0; i < m_levelsListDataAsset.LevelDataList.Count; i++)
            {
                var newWidget = new LevelWidget(m_levelWidgetTemplate, m_levelsListDataAsset.LevelDataList[i]);
                newWidget.OnLevelSelected += HandleLevelSelected;
                levelsScrollView.Add(newWidget.Element);
                m_instantiatedWidgets.Add(newWidget);
            }
            // Keeps the focused level in view while the gamepad or the keyboard moves through the list.
            levelsScrollView.RegisterCallback<FocusInEvent>(evt =>
            {
                if (evt.target is VisualElement level && level.layout.height > 0)
                    ScrollIntoView(levelsScrollView, level);
            });

            BindButton("back-button", HandleBackButton, m_backAudioPlayer);
            if (m_instantiatedWidgets.Count > 0)
            {
                DefaultFocus = m_instantiatedWidgets[0].Element;
            }
            else
            {
                // No level to move to: the screen itself keeps the focus, for Cancel to reach HandleCancel.
                var screen = Query<VisualElement>("level-selection-screen");
                screen.focusable = true;
                DefaultFocus = screen;
            }
        }

        protected override void HandleCancel()
        {
            m_backAudioPlayer.Play();
            HandleBackButton();
        }

        private void HandleBackButton()
        {
            MainMenuContext.Instance.FlowMachine.RequestBack();
        }

        // Like ScrollView.ScrollTo, margins included: they leave room for the focused level to grow.
        private static void ScrollIntoView(ScrollView scrollView, VisualElement element)
        {
            var bounds = element.ChangeCoordinatesTo(scrollView.contentContainer, new Rect(Vector2.zero, element.layout.size));
            var top = bounds.yMin - element.resolvedStyle.marginTop;
            var bottom = bounds.yMax + element.resolvedStyle.marginBottom;
            var viewportHeight = scrollView.contentViewport.layout.height;
            var offset = scrollView.scrollOffset;
            if (top < offset.y)
                offset.y = top;
            else if (bottom > offset.y + viewportHeight)
                offset.y = bottom - viewportHeight;
            scrollView.scrollOffset = offset;
        }

        private void HandleLevelSelected(LevelDataAsset obj)
        {
            m_levelSelectedAudioPlayer.Play();
            OnLevelSelected?.Invoke(obj);
        }
    }
}
