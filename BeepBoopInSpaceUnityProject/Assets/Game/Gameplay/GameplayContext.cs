using System.Collections;
using System.Collections.Generic;
using Game.ArchitectureTools.FlowMachine;
using Game.ArchitectureTools.Manager;
using Game.Gameplay.CharactersManagement.SpecialActionsSystem._0_Core;
using Game.Gameplay.Flows._0_Load;
using Game.Gameplay.GameModes.GameplayModifiers;
using Game.Gameplay.GameModes.ObjectiveManagement;
using Game.Gameplay.Levels._0_Core;
using Game.Gameplay.PauseMenu;
using UnityEngine;

namespace Game.Gameplay
{
    public class GameplayContext : AManager<GameplayContext>
    {
        [field: SerializeField]
        public LoadingManager LoadingManager { get; private set; }
        [field: SerializeField]
        public FlowMachine FlowMachine { get; private set; }
        [field: SerializeField]
        public PauseMenuManager PauseMenuManager { get; private set; }
        [field: SerializeField]
        public UIManager UIManager { get; private set; }
        public AObjectiveManager ObjectiveManager { get; private set; } 
        public SpecialAction SpecialActionPrefab { get; private set; }
        private List<AGameplayModifier> m_gameplayModifiers = new();
        public IReadOnlyList<AGameplayModifier> GameplayModifiers => m_gameplayModifiers;
        
        public CurrentLevelInfoManager CurrentLevelInfoManager { get; private set; }

        public void RegisterObjectiveManager(AObjectiveManager objectiveManager)
        {
            ObjectiveManager = objectiveManager;
        }

        public void RegisterSpecialActionPrefab(SpecialAction specialActionPrefab)
        {
            SpecialActionPrefab = specialActionPrefab;
        }

        public void RegisterGameplayModifier(AGameplayModifier gameplayModifier)
        {
            m_gameplayModifiers.Add(gameplayModifier);
        }

        public T GetGameplayModifier<T>() where T : AGameplayModifier
        {
            return m_gameplayModifiers.Find(gameplayModifier => gameplayModifier.GetType() == typeof(T)) as T;
        }

        public bool HasGameplayModifier<T>() where T : AGameplayModifier
        {
            return m_gameplayModifiers.Exists(gameplayModifier => gameplayModifier.GetType() == typeof(T));
        }
        
        private IEnumerator Start()
        {
            FlowMachine.Run();
            PauseMenuManager.Initialize(FlowMachine);
            
            yield return new WaitUntil(() => LoadingManager.HasLoadedLevel);
            CurrentLevelInfoManager = LoadingManager.FetchCurrentLevelInfoManager();
        }
    }
}
