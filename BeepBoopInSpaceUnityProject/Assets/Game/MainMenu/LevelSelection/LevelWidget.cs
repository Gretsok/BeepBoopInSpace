using System;
using Game.Gameplay.Levels._0_Core;
using UnityEngine.UIElements;

namespace Game.MainMenu.LevelSelection
{
    /// <summary>A level card of the level selection, cloned from the LevelWidget UXML. Clicking or submitting it selects the level.</summary>
    public class LevelWidget
    {
        public Button Element { get; }
        public LevelDataAsset LevelDataAsset { get; }

        public event Action<LevelDataAsset> OnLevelSelected;

        public LevelWidget(VisualTreeAsset template, LevelDataAsset levelDataAsset)
        {
            LevelDataAsset = levelDataAsset;

            Element = template.Instantiate().Q<Button>("level-card");
            // Styled by MainMenu.uss alone, without the default theme's button style.
            Element.RemoveFromClassList(Button.ussClassName);
            Element.Q("thumbnail").style.backgroundImage = new StyleBackground(LevelDataAsset.Thumbnail);
            Element.Q<Label>("level-name").text = LevelDataAsset.NameKey;
            Element.clicked += () => OnLevelSelected?.Invoke(LevelDataAsset);
        }
    }
}
