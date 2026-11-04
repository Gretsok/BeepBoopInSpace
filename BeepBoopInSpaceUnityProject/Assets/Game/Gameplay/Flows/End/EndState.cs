using Game.ArchitectureTools.FlowMachine;
using Game.LoadingScreen;
using Game.Tournament;
using Game.Training;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Game.Gameplay.Flows.End
{
    public class EndState : AFlowState
    {
        [SerializeField]
        private AssetReference m_mainMenuScene;

        /// <summary>Set by the pause menu: back to the main menu, even in a tournament, which is abandoned.</summary>
        public bool IsQuittingToMainMenu { get; set; }

        private bool m_isEnding;

        protected override void HandleEnter()
        {
            base.HandleEnter();
            // Asked twice (a guest's game the room is lost in as it ends): the game unloads once.
            if (m_isEnding)
                return;
            m_isEnding = true;

            var gameplayContext = GameplayContext.Instance;
            var currentLevelInfoManager = gameplayContext.CurrentLevelInfoManager;

            // Otherwise the tournament goes on once the game is unloaded: its results, then its next game.
            var tournamentContext = TournamentContext.Instance;
            if (tournamentContext && IsQuittingToMainMenu)
            {
                tournamentContext.Abandon();
                tournamentContext = null;
            }

            LoadingScreenManager.Instance?.ShowLoadingScreen();
            
            gameplayContext.LoadingManager.Unload(() =>
            {
                if (currentLevelInfoManager)
                {
                    currentLevelInfoManager.SetCurrentLevelDataAsset(null);
                }
                // Once the game's scenes are released, even those still loading when the game was left.
                if (!tournamentContext)
                {
                    Addressables.LoadSceneAsync(m_mainMenuScene);
                }
            });
        }
    }
}