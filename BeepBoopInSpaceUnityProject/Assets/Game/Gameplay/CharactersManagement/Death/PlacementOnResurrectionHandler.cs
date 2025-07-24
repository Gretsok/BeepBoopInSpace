using Game.Gameplay.GridSystem;
using Game.Gameplay.GridSystem.GenericComponents;
using UnityEngine;

namespace Game.Gameplay.CharactersManagement.Death
{
    [RequireComponent(typeof(DeathController))]
    public class PlacementOnResurrectionHandler : MonoBehaviour
    {
        private DeathController m_deathController;
        private GridBuilder m_gridBuilder;
        private void Awake()
        {
            m_deathController = GetComponent<DeathController>();

            m_deathController.OnResurrection += HandleResurrection;
            
            GridBuilder.RegisterPostInitializationCallback(builder => m_gridBuilder = builder);
        }

        private void HandleResurrection(DeathController obj)
        {
            if (m_deathController.CharacterReferencesHolder.GridWalker.CurrentCell.TryGetComponent(out KillingCellComponent killingCellComponent))
            {
                m_deathController.CharacterReferencesHolder.MovementController.TeleportToCell(
                    m_gridBuilder.GetRandomWalkableCell(cell => cell.TryGetComponent<KillingCellComponent>(out _))
                    );
            }
        }
    }
}
