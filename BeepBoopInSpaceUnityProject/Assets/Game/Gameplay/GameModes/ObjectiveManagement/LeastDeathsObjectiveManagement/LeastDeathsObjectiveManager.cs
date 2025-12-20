using System.Collections;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.CharactersManagement.Death;
using Game.Gameplay.Flows.Results;
using UnityEngine;

namespace Game.Gameplay.GameModes.ObjectiveManagement.LeastDeathsObjectiveManagement
{
    public class LeastDeathsObjectiveManager : AObjectiveManager
    {
        private CharactersManager m_charactersManager;

        protected override IEnumerator Initialize()
        {
            CharactersManager.RegisterPostInitializationCallback(manager =>
            {
                m_charactersManager = manager;

                m_charactersManager.OnCharactersCreated += SetUp;
            });
            
            ResultsManager.RegisterPostInitializationCallback(manager => manager.SetScoreCalculationMethod(ResultsManager.EScoreCalculationMethod.LowerScoreIsBest));
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
            obj.CharacterReferencesHolder.ScoringController.IncreaseScore();
        }
    }
}
