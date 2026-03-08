using System.Collections;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.CharactersManagement.SpecialActionsSystem.SniperRifle;
using Game.Gameplay.Timer;
using UnityEngine;

namespace Game.Gameplay.GameModes.ObjectiveManagement.MostKillsObjectiveManagement
{
    public class MostKillsObjectiveManager : AObjectiveManager
    {
        private CharactersManager m_charactersManager;

        [SerializeField]
        private TimerManager m_timerManager;

        protected override IEnumerator Initialize()
        {
            CharactersManager.RegisterPostInitializationCallback(manager =>
            {
                m_charactersManager = manager;

                m_charactersManager.OnCharactersCreated += SetUp;
            });
            yield break;
        }

        private void SetUp(CharactersManager obj)
        {
            m_charactersManager.OnCharactersCreated -= SetUp;
            
            foreach (var pawn in m_charactersManager.CharacterPawns)
            {
                pawn.ReferencesHolder.SpecialAction.GetComponent<SniperBulletShooter>().OnKilledOtherPlayer += HandlePlayerKilledOtherPlayer;
            }
        }

        private void HandlePlayerKilledOtherPlayer(SniperBulletShooter shooter)
        {
            shooter.ReferencesHolder.ScoringController.IncreaseScore();
        }
    }
}