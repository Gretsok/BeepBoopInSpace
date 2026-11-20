using System;
using System.Collections.Generic;
using Game.Gameplay.Levels._0_Core;
using Game.Global.SFXManagement;
using Game.UITools.Screens;
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

        private ConfirmationDialog m_confirmation;
        private LevelDataAsset m_pickedLevel;

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
            m_confirmation = new ConfirmationDialog(Query<VisualElement>("level-confirmation"), HandleLaunchConfirmed, HandleLaunchCancelled);
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

        // The level is only launched once confirmed: a press too many on the hub's Entraînement doesn't start a game.
        private void HandleLevelSelected(LevelDataAsset levelDataAsset)
        {
            m_levelSelectedAudioPlayer.Play();
            m_pickedLevel = levelDataAsset;
            m_confirmation.SetContent(Game.Global.Localization.Loc.T(levelDataAsset.NameKey), levelDataAsset.Thumbnail);
            ShowDialog(m_confirmation);
        }

        private void HandleLaunchConfirmed()
        {
            m_levelSelectedAudioPlayer.Play();
            OnLevelSelected?.Invoke(m_pickedLevel);
        }

        private void HandleLaunchCancelled()
        {
            m_backAudioPlayer.Play();
            HideDialog();
        }
    }
}
