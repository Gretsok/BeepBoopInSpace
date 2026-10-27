using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Game.Characters;
using Game.MainMenu.CharacterManagement;
using Game.UITools;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.MainMenu.CharacterSelection
{
    /// <summary>
    /// The grid of characters under the players' circles: where each player's cursor is (a badge per player) and which
    /// characters are taken. Cells past the characters show locked robots. Built in code in the "roster" element.
    /// </summary>
    public class CharacterRoster
    {
        public const int k_Columns = 10;
        // A character's grid colors: its selection color, and a darker shade of it for the outline.
        private const float k_OutlineShade = 0.68f;
        // The icon's top in its cell, as in the USS.
        private const float k_IconTop = -40f;

        private readonly CharacterBankManager m_bank;
        private readonly List<CharacterDataAsset> m_characters;
        private readonly Dictionary<CharacterDataAsset, Cell> m_cells = new();
        private readonly Dictionary<CharacterWidget, Badge> m_badges = new();

        private class Cell
        {
            public VisualElement Element;
            public VisualElement Contour;
            public VisualElement Badges;
        }

        private class Badge
        {
            public VisualElement Element;
            public Label Label;
            public CharacterDataAsset Character;
        }

        public CharacterRoster(VisualElement container, CharacterBankManager bank, int cellCount)
        {
            m_bank = bank;
            m_characters = bank.Characters.ToList();
            container.Clear();
            for (int i = 0; i < Mathf.Max(cellCount, m_characters.Count); i++)
            {
                var character = i < m_characters.Count ? m_characters[i] : null;
                var cell = CreateCell(character);
                container.Add(cell.Element);
                if (character)
                    m_cells[character] = cell;
            }
        }

        private static Cell CreateCell(CharacterDataAsset character)
        {
            var cell = new Cell { Element = Create("cs-cell") };
            cell.Element.Add(Create("cs-cell__background"));
            cell.Contour = Create("cs-cell__contour");
            cell.Element.Add(cell.Contour);
            var icon = Create("cs-cell__icon");
            cell.Element.Add(icon);
            cell.Badges = Create("cs-cell__badges");
            cell.Element.Add(cell.Badges);

            // Without a character, or an icon for it, the cell shows a grey robot.
            if (character && character.SelectionIcon)
            {
                icon.style.backgroundImage = new StyleBackground(character.SelectionIcon);
                icon.style.top = k_IconTop + character.SelectionIconOffset;
            }
            else
                cell.Element.AddToClassList("cs-cell--locked");
            if (character)
                cell.Element.AddToClassList("cs-cell--character");
            return cell;
        }

        private static VisualElement Create(string className)
        {
            var element = new VisualElement { pickingMode = PickingMode.Ignore };
            element.AddToClassList(className);
            return element;
        }

        /// <summary>The character next to <paramref name="from"/>, x going right and y down. Left and right wrap around.</summary>
        public CharacterDataAsset GetNeighbour(CharacterDataAsset from, Vector2Int direction)
        {
            var index = m_characters.IndexOf(from);
            if (index < 0 || m_characters.Count == 0)
                return null;
            if (direction.x != 0)
                return m_characters[(index + direction.x % m_characters.Count + m_characters.Count) % m_characters.Count];
            var target = index + direction.y * k_Columns;
            return target >= 0 && target < m_characters.Count ? m_characters[target] : null;
        }

        /// <summary>A character the widget's player could choose, other than the one it points at.</summary>
        public CharacterDataAsset PickRandom(CharacterWidget widget)
        {
            var candidates = m_characters.Where(character => character != widget.CharacterDataAsset
                                                             && !m_bank.IsCharacterAlreadyTakenByAnotherWidget(character, widget)).ToList();
            return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
        }

        /// <summary>Moves the badges of the players to the characters they point at, and colors the chosen ones.</summary>
        public void Refresh(IEnumerable<CharacterWidget> widgets)
        {
            var activeWidgets = widgets.Where(widget => widget.IsActivated && widget.CharacterDataAsset).ToList();
            foreach (var (widget, badge) in m_badges.ToList())
            {
                if (activeWidgets.Contains(widget))
                    continue;
                badge.Element.RemoveFromHierarchy();
                m_badges.Remove(widget);
            }

            foreach (var widget in activeWidgets)
            {
                if (!m_badges.TryGetValue(widget, out var badge))
                {
                    badge = new Badge { Element = Create("cs-cell__badge"), Label = new Label { pickingMode = PickingMode.Ignore } };
                    badge.Label.AddToClassList("cs-cell__badge-label");
                    badge.Element.Add(badge.Label);
                    m_badges[widget] = badge;
                }
                badge.Label.text = $"J{widget.SlotIndex + 1}";
                badge.Element.EnableInClassList("cs-cell__badge--ready", widget.IsReady);
                badge.Element.style.unityBackgroundImageTintColor = widget.IsReady ? widget.CharacterDataAsset.SelectionColor : StyleKeyword.Null;

                if (badge.Character == widget.CharacterDataAsset || !m_cells.TryGetValue(widget.CharacterDataAsset, out var cell))
                    continue;
                badge.Character = widget.CharacterDataAsset;
                cell.Badges.Add(badge.Element);
                badge.Element.KillTweens();
                badge.Element.style.scale = new Scale(new Vector3(0.3f, 0.3f, 1f));
                badge.Element.TweenScale(1f, 0.25f).SetEase(Ease.OutBack).OnComplete(badge.Element.ResetTransform);
            }

            foreach (var (character, cell) in m_cells)
            {
                var widgetsOnCell = activeWidgets.Where(widget => widget.CharacterDataAsset == character).ToList();
                var isTaken = widgetsOnCell.Any(widget => widget.IsReady);
                cell.Element.EnableInClassList("cs-cell--hovered", widgetsOnCell.Count > 0 && !isTaken);
                cell.Element.EnableInClassList("cs-cell--taken", isTaken);
                cell.Element.Q(className: "cs-cell__background").style.unityBackgroundImageTintColor =
                    isTaken ? character.SelectionColor : StyleKeyword.Null;
                cell.Contour.style.unityBackgroundImageTintColor = isTaken ? Shade(character.SelectionColor) : StyleKeyword.Null;
                // Badges in player order, the first one on the right.
                foreach (var widget in widgetsOnCell.OrderBy(widget => widget.SlotIndex))
                    m_badges[widget].Element.BringToFront();
            }
        }

        public void PlayTaken(CharacterDataAsset character)
        {
            if (!character || !m_cells.TryGetValue(character, out var cell))
                return;
            cell.Element.KillTweens();
            cell.Element.PunchScale(0.25f, 0.4f, 5);
        }

        public void PlayRefused(CharacterDataAsset character)
        {
            if (!character || !m_cells.TryGetValue(character, out var cell))
                return;
            cell.Element.KillTweens();
            cell.Element.ShakeX(10f, 0.4f).OnComplete(cell.Element.ResetTransform);
        }

        private static Color Shade(Color color) => new(color.r * k_OutlineShade, color.g * k_OutlineShade, color.b * k_OutlineShade, color.a);
    }
}
