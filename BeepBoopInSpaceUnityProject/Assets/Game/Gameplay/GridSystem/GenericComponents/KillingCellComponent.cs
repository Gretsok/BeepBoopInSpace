using Game.Gameplay.Cells.Default;
using Game.Gameplay.CharactersManagement.Movement;
using Game.Online;
using UnityEngine;

namespace Game.Gameplay.GridSystem.GenericComponents
{
    /// <summary>
    /// Water, lava: kills the robot that steps on the cell, as it lands. The robot can't move from the moment it steps
    /// on it: a player mashing the buttons would otherwise leave the cell before landing, and cross it unharmed.
    /// </summary>
    [RequireComponent(typeof(CanBeWalkedOnCellComponent))]
    public class KillingCellComponent : MonoBehaviour, ICharacterMovementControllerBlocker
    {
        private CanBeWalkedOnCellComponent m_canBeWalkedOnCellComponent;
        private Cell m_cell;
        // The robot on the cell, held there until it lands.
        private CharacterMovementController m_victim;

        private void Awake()
        {
            m_canBeWalkedOnCellComponent = GetComponent<CanBeWalkedOnCellComponent>();
            m_cell = GetComponent<Cell>();
            m_canBeWalkedOnCellComponent.OnMovementControllerOnCellChanged += HandleMovementControllerOnCellChanged;
        }

        private void OnDestroy()
        {
            Release();
        }

        private void HandleMovementControllerOnCellChanged(CanBeWalkedOnCellComponent obj)
        {
            // Online guest: the host kills.
            if (!Netplay.HasAuthority)
                return;
            var movementController = obj.MovementControllerOnCell;
            if (movementController == m_victim)
                return;
            // Taken off the cell before landing: it died of something else, or a new round placed it.
            Release();
            if (!movementController)
                return;
            m_victim = movementController;
            m_victim.RegisterBlocker(this);
            m_victim.OnMoveAnimationDone += HandleTargetMoveAnimationDone;
        }

        private void HandleTargetMoveAnimationDone(CharacterMovementController movementController, Cell cell)
        {
            // The end of an earlier jump, onto the cell before this one.
            if (cell != m_cell)
                return;
            Release();
            movementController.ReferencesHolder.DeathController.Kill(a_cannotCounter: true);
        }

        private void Release()
        {
            if (!m_victim)
                return;
            m_victim.OnMoveAnimationDone -= HandleTargetMoveAnimationDone;
            m_victim.UnregisterBlocker(this);
            m_victim = null;
        }
    }
}
