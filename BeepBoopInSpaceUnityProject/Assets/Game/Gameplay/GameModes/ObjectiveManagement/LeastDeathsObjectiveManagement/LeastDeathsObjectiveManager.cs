using System.Collections;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.CharactersManagement.Death;
using Game.Gameplay.ConfigurationsManagement;
using Game.Gameplay.Flows.Results;
using Game.Gameplay.Timer;
using Game.Online;
using UnityEngine;

namespace Game.Gameplay.GameModes.ObjectiveManagement.LeastDeathsObjectiveManagement
{
    public class LeastDeathsObjectiveManager : AObjectiveManager
    {
        private CharactersManager m_charactersManager;

        [SerializeField]
        private TimerManager m_timerManager;
        [SerializeField]
        private PeriodicConfigurationChangesManager m_periodicConfigurationChangesManager;

        public override string ScoreCaption => "Morts";

        protected override IEnumerator Initialize()
        {
            CharactersManager.RegisterPostInitializationCallback(manager =>
            {
                m_charactersManager = manager;

                m_charactersManager.OnCharactersCreated += SetUp;
            });
            
            ResultsManager.RegisterPostInitializationCallback(manager => manager.SetScoreCalculationMethod(EScoreCalculationMethod.LowerScoreIsBest));
            yield break;
        }

        private void SetUp(CharactersManager obj)
        {
            m_charactersManager.OnCharactersCreated -= SetUp;
            
            foreach (var pawn in m_charactersManager.CharacterPawns)
            {
                pawn.ReferencesHolder.DeathController.OnDeath += HandlePlayedDeath;
            }
        }

        private void HandlePlayedDeath(DeathController obj)
        {
            // Online guest: the host counts, the scores arrive with its messages.
            if (!Netplay.HasAuthority)
                return;
            obj.CharacterReferencesHolder.ScoringController.IncreaseScore();
        }
    }
}
