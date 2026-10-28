using UnityEngine.UIElements;

namespace Game.UITools.Screens
{
    public static class StyleExtensions
    {
        /// <summary>Sets the four border colors; StyleKeyword.Null gives them back to the USS.</summary>
        public static void SetBorderColor(this IStyle style, StyleColor color)
        {
            style.borderTopColor = color;
            style.borderRightColor = color;
            style.borderBottomColor = color;
            style.borderLeftColor = color;
        }
    }
}
