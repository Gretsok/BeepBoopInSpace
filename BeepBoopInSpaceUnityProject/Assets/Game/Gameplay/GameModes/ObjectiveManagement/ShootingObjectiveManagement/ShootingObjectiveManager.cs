using System.Collections;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.CharactersManagement.SpecialActionsSystem.SniperRifle;

namespace Game.Gameplay.GameModes.Sniper.ShootingObjectiveManagement
{
    public class ShootingObjectiveManager : AObjectiveManager
    {
        private CharactersManager m_charactersManager;

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