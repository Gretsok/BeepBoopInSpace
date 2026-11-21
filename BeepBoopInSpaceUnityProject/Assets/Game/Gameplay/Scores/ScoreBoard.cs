using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.CharactersManagement.Scoring;
using Game.Gameplay.Flows.Results;
using Game.Global.Localization;
using Game.UITools;
using Game.UITools.Screens;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Gameplay.Scores
{
    /// <summary>
    /// The players' scores, hidden while they play, shown in the free space of an announcement (its score-board
    /// element): what the objective counts, then a card per robot with its score, or as many pips as the goal when the
    /// game is won at a number of points. The board first shows the scores of the previous board, then the ones that
    /// changed since pop to their new value; the leaders' cards take their character's color.
    /// </summary>
    public class ScoreBoard
    {
        public const string ElementName = "score-board";

        private const string k_CaptionClass = "score-board__caption";
        private const string k_CardsClass = "score-board__cards";
        private const string k_CardClass = "score-card";
        private const string k_LeaderClass = "score-card--leader";
        private const string k_IconClass = "score-card__icon";
        private const string k_BadgeClass = "score-card__badge";
        private const string k_ScoreClass = "score-card__score";
        private const string k_PipsClass = "score-card__pips";
        private const string k_PipClass = "score-card__pip";

        // Once the board has unrolled under the banner.
        private const float k_EntranceDelay = 0.15f;
        private const float k_RevealDelay = 0.4f;

        // The scores the last board showed, by robot, so that the next one pops those that changed: one game at a time.
        private static readonly Dictionary<CharacterScoringController, int> s_shownScores = new();
        private static CharactersManager s_shownScoresGame;

        private readonly GameObject m_owner;
        private readonly bool m_lowerIsBest;
        private readonly List<Card> m_cards = new();
        private readonly float m_revealTime;

        private class Card
        {
            public CharacterScoringController Scoring;
            public Color Color;
            public VisualElement Element;
            public Label ScoreLabel;
            public List<VisualElement> Pips;
            public int ShownScore;
        }

        /// <summary>Fills the score-board element of a screen being shown, hides it out of a game; null without one.</summary>
        public static ScoreBoard Show(VisualElement root, GameObject owner)
        {
            var board = root.Q(ElementName);
            if (board == null)
                return null;
            board.Clear();

            var gameplayContext = GameplayContext.Instance;
            var charactersManager = CharactersManager.Instance;
            var objective = gameplayContext ? gameplayContext.ObjectiveManager : null;
            if (!objective || !charactersManager || !charactersManager.CharacterPawns.Any(pawn => pawn))
            {
                board.style.display = DisplayStyle.None;
                return null;
            }
            board.style.display = StyleKeyword.Null;
            return new ScoreBoard(board, owner, objective.ScoreCaption, objective.ScoreGoal, charactersManager);
        }

        private ScoreBoard(VisualElement board, GameObject owner, string caption, int goal, CharactersManager charactersManager)
        {
            m_owner = owner;
            var resultsManager = ResultsManager.Instance;
            m_lowerIsBest = resultsManager && resultsManager.ScoreCalculationMethod == EScoreCalculationMethod.LowerScoreIsBest;
            m_revealTime = Time.unscaledTime + k_RevealDelay;

            if (s_shownScoresGame != charactersManager)
            {
                s_shownScores.Clear();
                s_shownScoresGame = charactersManager;
            }

            var captionLabel = new Label(Loc.T(caption).ToUpperInvariant()) { pickingMode = PickingMode.Ignore };
            captionLabel.AddToClassList("ui-text");
            captionLabel.AddToClassList("ui-heading");
            captionLabel.AddToClassList(k_CaptionClass);
            board.Add(captionLabel);

            var cards = NewElement(k_CardsClass);
            board.Add(cards);
            foreach (var pawn in charactersManager.CharacterPawns)
            {
                if (!pawn)
                    continue;
                var card = CreateCard(pawn, charactersManager, goal);
                m_cards.Add(card);
                cards.Add(card.Element);
            }
            RefreshLeaders();

            board.TweenOpacity(0f, 1f, 0.2f)
                .SetDelay(k_EntranceDelay)
                .SetLink(owner, LinkBehaviour.KillOnDisable);
            board.TweenScale(new Vector2(0.9f, 0.9f), Vector2.one, 0.3f)
                .SetDelay(k_EntranceDelay)
                .SetEase(Ease.OutBack)
                .SetLink(owner, LinkBehaviour.KillOnDisable)
                .OnComplete(board.ResetTransform);
        }

        /// <summary>Once the board is in place, pops the scores that changed since the last board, or while it is shown.</summary>
        public void Refresh()
        {
            if (Time.unscaledTime < m_revealTime)
                return;
            var changed = false;
            foreach (var card in m_cards)
            {
                if (!card.Scoring || card.Scoring.Score == card.ShownScore)
                    continue;
                var previousScore = card.ShownScore;
                SetScore(card, card.Scoring.Score);
                s_shownScores[card.Scoring] = card.ShownScore;
                PopScore(card, previousScore);
                changed = true;
            }
            if (changed)
                RefreshLeaders();
        }

        private Card CreateCard(CharacterPawn pawn, CharactersManager charactersManager, int goal)
        {
            var references = pawn.ReferencesHolder;
            var character = references.CharacterDataAsset;
            var card = new Card
            {
                Scoring = references.ScoringController,
                Color = character.SelectionColor,
                Element = NewElement(k_CardClass),
            };

            var icon = NewElement(k_IconClass);
            icon.style.backgroundImage = character.SelectionIcon
                ? new StyleBackground(character.SelectionIcon)
                : new StyleBackground(character.AvatarSprite);
            card.Element.Add(icon);

            var player = charactersManager.ControllerOf(pawn)?.Player;
            if (player != null)
            {
                var badge = NewElement("ui-badge");
                badge.AddToClassList("ui-badge--done");
                badge.AddToClassList(k_BadgeClass);
                badge.style.backgroundColor = card.Color;
                var badgeLabel = new Label(player.BadgeLabel) { pickingMode = PickingMode.Ignore };
                badgeLabel.AddToClassList("ui-badge__label");
                badge.Add(badgeLabel);
                card.Element.Add(badge);
            }

            if (goal > 0)
            {
                var pips = NewElement(k_PipsClass);
                card.Pips = new List<VisualElement>();
                for (int i = 0; i < goal; i++)
                {
                    var pip = NewElement(k_PipClass);
                    card.Pips.Add(pip);
                    pips.Add(pip);
                }
                card.Element.Add(pips);
            }
            else
            {
                card.ScoreLabel = new Label { pickingMode = PickingMode.Ignore };
                card.ScoreLabel.AddToClassList("ui-text");
                card.ScoreLabel.AddToClassList(k_ScoreClass);
                card.Element.Add(card.ScoreLabel);
            }

            // Scores start at 0: a robot no board showed yet has its points to pop.
            SetScore(card, card.Scoring && s_shownScores.TryGetValue(card.Scoring, out var shownScore) ? shownScore : 0);
            return card;
        }

        private static void SetScore(Card card, int score)
        {
            card.ShownScore = score;
            if (card.ScoreLabel != null)
                card.ScoreLabel.text = score.ToString();
            if (card.Pips == null)
                return;
            for (int i = 0; i < card.Pips.Count; i++)
                card.Pips[i].style.backgroundColor = i < score ? new StyleColor(card.Color) : new StyleColor(StyleKeyword.Null);
        }

        private void PopScore(Card card, int previousScore)
        {
            if (card.ScoreLabel != null)
                Pop(card.ScoreLabel, 0.6f);
            if (card.Pips == null)
                return;
            for (int i = Mathf.Max(previousScore, 0); i < Mathf.Min(card.ShownScore, card.Pips.Count); i++)
                Pop(card.Pips[i], 0.8f);
        }

        private void Pop(VisualElement element, float punch)
        {
            element.KillTweens();
            element.PunchScale(punch, 0.45f, 4)
                .SetLink(m_owner, LinkBehaviour.KillOnDisable)
                .OnComplete(element.ResetTransform);
        }

        // The best score shown, when it sets a robot apart: nobody leads while everyone is equal, or has no point yet.
        private void RefreshLeaders()
        {
            var scores = m_cards.Select(card => card.ShownScore).ToList();
            var best = m_lowerIsBest ? scores.Min() : scores.Max();
            var hasLeaders = scores.Count > 1 && scores.Any(score => score != best) && (m_lowerIsBest || best > 0);
            foreach (var card in m_cards)
            {
                var leads = hasLeaders && card.ShownScore == best;
                card.Element.EnableInClassList(k_LeaderClass, leads);
                card.Element.style.SetBorderColor(leads ? new StyleColor(card.Color) : new StyleColor(StyleKeyword.Null));
            }
        }

        private static VisualElement NewElement(string className)
        {
            var element = new VisualElement { pickingMode = PickingMode.Ignore };
            element.AddToClassList(className);
            return element;
        }
    }
}
