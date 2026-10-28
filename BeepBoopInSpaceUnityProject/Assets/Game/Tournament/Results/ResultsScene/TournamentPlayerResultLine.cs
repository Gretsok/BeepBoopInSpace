using System.Linq;
using Game.Gameplay.Flows.Results;
using Game.Global.PlayerManagement;
using Game.UITools.Screens;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Tournament.Results
{
    /// <summary>
    /// A player's line in the tournament results, cloned from the TournamentPlayerResultLine UXML: the robot, the
    /// games won so far, and a cell per game played, highlighted in the character's color where the player won.
    /// </summary>
    public class TournamentPlayerResultLine
    {
        private const string k_GameClass = "tournament-game";
        private const string k_GameWonClass = "tournament-game--won";

        public AbstractPlayer Player { get; }
        public VisualElement Element { get; }
        public int Score { get; }

        public TournamentPlayerResultLine(VisualTreeAsset template, AbstractPlayer player)
        {
            Player = player;
            Element = template.Instantiate().Q("player-line");

            var resultsManager = TournamentContext.Instance.ResultsManager;
            var gameResults = resultsManager.TournamentGameResults;
            var playerResults = resultsManager.GetPlayerResultsOf(player).ToList();
            Score = playerResults.Count(result => result.IsWinner);

            var character = player.CharacterDataAsset;
            Element.Q("icon").style.backgroundImage = character.SelectionIcon
                ? new StyleBackground(character.SelectionIcon)
                : new StyleBackground(character.AvatarSprite);
            Element.Q("badge").style.backgroundColor = character.SelectionColor;
            Element.Q<Label>("badge-label").text = $"J{player.PlayerInput.playerIndex + 1}";
            Element.Q<Label>("score").text = Score.ToString();

            var games = Element.Q("games");
            for (int i = 0; i < gameResults.Count; i++)
                games.Add(CreateGameCell(gameResults[i], playerResults[i], character.SelectionColor));
        }

        /// <summary>Outlines the line in the character's color, for the players in the lead.</summary>
        public void Highlight()
        {
            Element.style.SetBorderColor(Player.CharacterDataAsset.SelectionColor);
        }

        private static VisualElement CreateGameCell(GameResult gameResult, GameResult.SPlayerResult playerResult, Color winColor)
        {
            var cell = new VisualElement { pickingMode = PickingMode.Ignore };
            cell.AddToClassList(k_GameClass);
            var thumbnail = gameResult.LevelDataAsset ? gameResult.LevelDataAsset.Thumbnail : null;
            if (thumbnail)
                cell.style.backgroundImage = new StyleBackground(thumbnail);
            if (playerResult.IsWinner)
            {
                cell.AddToClassList(k_GameWonClass);
                cell.style.SetBorderColor(winColor);
            }
            return cell;
        }
    }
}
