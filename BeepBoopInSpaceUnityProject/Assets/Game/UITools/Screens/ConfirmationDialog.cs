using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.UITools.Screens
{
    /// <summary>
    /// A question over a screen, from the ConfirmationDialog UXML: an optional picture, a title, the question, and the
    /// Retour and confirm buttons. The screen shows it with AUIToolkitScreen.ShowDialog, which keeps the d-pad in it and
    /// sends Cancel to its Retour button. The confirm button ignores presses for a moment after it opens: the press that
    /// opened the dialog, repeated too fast, doesn't confirm it.
    /// </summary>
    public class ConfirmationDialog
    {
        private const string k_DialogClass = "ui-dialog";
        private const string k_ShownClass = "ui-dialog--shown";
        private const float k_InputDelay = 0.35f;

        private readonly VisualElement m_panel;
        private readonly VisualElement m_image;
        private readonly Label m_title;
        private readonly Action m_onConfirmed;
        private readonly Action m_onCancelled;
        private float m_shownTime;
        private bool m_isConfirmed;

        /// <summary>The element that darkens the screen, the instance of the template.</summary>
        public VisualElement Element { get; }
        public Button ConfirmButton { get; }
        public Button CancelButton { get; }
        public bool IsShown { get; private set; }

        /// <param name="element">The instance of the ConfirmationDialog template, or an element that holds it.</param>
        /// <param name="onConfirmed">Called once per opening, when the player confirms.</param>
        /// <param name="onCancelled">Called when the player presses Retour or Cancel: hide the dialog from there.</param>
        public ConfirmationDialog(VisualElement element, Action onConfirmed, Action onCancelled)
        {
            Element = element.ClassListContains(k_DialogClass) ? element : element.Q(className: k_DialogClass);
            m_panel = Element.Q("panel");
            m_image = Element.Q("image");
            m_title = Element.Q<Label>("title");
            ConfirmButton = Element.Q<Button>("confirm-button");
            CancelButton = Element.Q<Button>("cancel-button");
            m_onConfirmed = onConfirmed;
            m_onCancelled = onCancelled;

            // Styled by the screens' USS alone, without the default theme's button style.
            ConfirmButton.RemoveFromClassList(Button.ussClassName);
            CancelButton.RemoveFromClassList(Button.ussClassName);
            ConfirmButton.clicked += Confirm;
            CancelButton.clicked += Cancel;
            SetImage(null);
        }

        /// <summary>Replaces the title of the UXML, and shows the picture above it (none if null).</summary>
        public void SetContent(string title, Sprite image)
        {
            m_title.text = title;
            SetImage(image);
        }

        /// <summary>Shows the dialog with a pop. Its tweens die with <paramref name="owner"/>.</summary>
        public void Show(GameObject owner)
        {
            IsShown = true;
            m_isConfirmed = false;
            m_shownTime = Time.unscaledTime;
            Element.AddToClassList(k_ShownClass);

            Element.KillTweens();
            m_panel.KillTweens();
            Element.TweenOpacity(0f, 1f, 0.15f).SetLink(owner, LinkBehaviour.KillOnDisable)
                .OnComplete(Element.ResetTransform);
            m_panel.TweenScale(new Vector2(0.85f, 0.85f), Vector2.one, 0.3f).SetEase(Ease.OutBack)
                .SetLink(owner, LinkBehaviour.KillOnDisable).OnComplete(m_panel.ResetTransform);
        }

        public void Hide()
        {
            IsShown = false;
            Element.KillTweens();
            m_panel.KillTweens();
            Element.RemoveFromClassList(k_ShownClass);
        }

        /// <summary>What the Retour button does: the screen sends the main player's Cancel here while the dialog is shown.</summary>
        public void Cancel()
        {
            if (IsShown && !m_isConfirmed)
                m_onCancelled();
        }

        private void Confirm()
        {
            if (!IsShown || m_isConfirmed || Time.unscaledTime - m_shownTime < k_InputDelay)
                return;
            m_isConfirmed = true;
            m_onConfirmed();
        }

        private void SetImage(Sprite image)
        {
            m_image.style.backgroundImage = image ? new StyleBackground(image) : StyleKeyword.Null;
            m_image.style.display = image ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
