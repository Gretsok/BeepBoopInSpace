using DG.Tweening;
using Game.UITools;
using Game.UITools.Screens;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.LoadingScreen
{
    /// <summary>Shown by the LoadingScreenManager over menus and games while scenes load: something moves meanwhile.</summary>
    public class LoadingScreenPanel : AUIToolkitScreen
    {
        private const string k_LoadingText = "Chargement";
        private const long k_DotIntervalMs = 300;

        protected override void HandleShown()
        {
            Query<VisualElement>("walkers").TweenTranslate(new Vector2(0f, -14f), 0.25f)
                .SetEase(Ease.OutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            var label = Query<Label>("loading-label");
            var dots = 0;
            label.text = k_LoadingText;
            label.schedule.Execute(() =>
            {
                dots = (dots + 1) % 4;
                label.text = k_LoadingText + new string('.', dots);
            }).Every(k_DotIntervalMs);
        }
    }
}
