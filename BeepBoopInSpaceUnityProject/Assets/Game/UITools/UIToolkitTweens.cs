using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.UITools
{
    /// <summary>
    /// DOTween tweens of UI Toolkit inline styles, for one-shot motions that USS transitions can't express (punches,
    /// shakes, sequences). They drive the inline style, which overrides the USS: tween elements the USS doesn't animate,
    /// or reset the style when done (<see cref="ResetTransform"/>). Tweens target the element (<see cref="KillTweens"/>)
    /// and run on unscaled time.
    /// </summary>
    public static class UIToolkitTweens
    {
        public static Tweener TweenScale(this VisualElement element, Vector2 to, float duration)
        {
            var from = (Vector2)element.resolvedStyle.scale.value;
            return Prepare(DOTween.To(() => from, value =>
            {
                from = value;
                element.style.scale = new Scale(new Vector3(value.x, value.y, 1f));
            }, to, duration), element);
        }

        public static Tweener TweenScale(this VisualElement element, float to, float duration) =>
            element.TweenScale(new Vector2(to, to), duration);

        /// <summary>From an explicit scale, for an element that has just been created (its resolved style isn't computed yet).</summary>
        public static Tweener TweenScale(this VisualElement element, Vector2 from, Vector2 to, float duration)
        {
            element.style.scale = new Scale(new Vector3(from.x, from.y, 1f));
            return Prepare(DOTween.To(() => from, value =>
            {
                from = value;
                element.style.scale = new Scale(new Vector3(value.x, value.y, 1f));
            }, to, duration), element);
        }

        public static Tweener PunchScale(this VisualElement element, float punch, float duration, int vibrato = 6)
        {
            var scale = Vector3.one;
            return Prepare(DOTween.Punch(() => scale, value =>
            {
                scale = value;
                element.style.scale = new Scale(new Vector3(value.x, value.y, 1f));
            }, new Vector3(punch, punch, 0f), duration, vibrato), element);
        }

        public static Tweener TweenTranslate(this VisualElement element, Vector2 to, float duration)
        {
            var translate = element.resolvedStyle.translate;
            var from = new Vector2(translate.x, translate.y);
            return Prepare(DOTween.To(() => from, value =>
            {
                from = value;
                element.style.translate = new Translate(value.x, value.y);
            }, to, duration), element);
        }

        public static Tweener ShakeX(this VisualElement element, float strength, float duration)
        {
            var offset = Vector3.zero;
            return Prepare(DOTween.Shake(() => offset, value =>
            {
                offset = value;
                element.style.translate = new Translate(value.x, 0f);
            }, duration, new Vector3(strength, 0f, 0f), 14, 0f, false), element);
        }

        public static Tweener TweenRotate(this VisualElement element, float toDegrees, float duration)
        {
            var from = element.resolvedStyle.rotate.angle.ToDegrees();
            return Prepare(DOTween.To(() => from, value =>
            {
                from = value;
                element.style.rotate = new Rotate(new Angle(value, AngleUnit.Degree));
            }, toDegrees, duration), element);
        }

        public static Tweener TweenOpacity(this VisualElement element, float to, float duration)
        {
            var from = element.resolvedStyle.opacity;
            return Prepare(DOTween.To(() => from, value =>
            {
                from = value;
                element.style.opacity = value;
            }, to, duration), element);
        }

        /// <summary>From an explicit opacity, for an element that has just been created (its resolved style isn't computed yet).</summary>
        public static Tweener TweenOpacity(this VisualElement element, float from, float to, float duration)
        {
            element.style.opacity = from;
            return Prepare(DOTween.To(() => from, value =>
            {
                from = value;
                element.style.opacity = value;
            }, to, duration), element);
        }

        /// <summary>Gives the element's scale, rotation, translation and opacity back to its USS.</summary>
        public static void ResetTransform(this VisualElement element)
        {
            element.style.scale = StyleKeyword.Null;
            element.style.rotate = StyleKeyword.Null;
            element.style.translate = StyleKeyword.Null;
            element.style.opacity = StyleKeyword.Null;
        }

        /// <summary>Kills the element's tweens, then gives its transform back to its USS.</summary>
        public static void KillTweens(this VisualElement element)
        {
            DOTween.Kill(element);
            element.ResetTransform();
        }

        private static Tweener Prepare(Tweener tweener, VisualElement element) =>
            tweener.SetTarget(element).SetUpdate(true);
    }
}
