using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.UITools.Screens
{
    /// <summary>
    /// An announcement over the game (new round, new controls, end of the game): its banner (class banner) unrolls
    /// when the flow state shows it, then its text pops in.
    /// </summary>
    public class BannerScreen : AUIToolkitScreen
    {
        private const string k_BannerClass = "banner";
        private const string k_LabelClass = "banner__label";

        protected override void HandleShown()
        {
            var banner = Root.Q(className: k_BannerClass);
            if (banner == null)
                return;
            banner.TweenScale(new Vector2(1f, 0f), Vector2.one, 0.3f)
                .SetEase(Ease.OutBack)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(banner.ResetTransform);

            var label = banner.Q(className: k_LabelClass);
            if (label == null)
                return;
            label.TweenOpacity(0f, 1f, 0.15f)
                .SetDelay(0.15f)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            label.TweenScale(new Vector2(0.6f, 0.6f), Vector2.one, 0.35f)
                .SetDelay(0.15f)
                .SetEase(Ease.OutBack)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(label.ResetTransform);
        }
    }
}
