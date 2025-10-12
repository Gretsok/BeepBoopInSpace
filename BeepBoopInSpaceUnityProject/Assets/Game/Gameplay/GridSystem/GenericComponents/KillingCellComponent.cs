using Game.Gameplay.Cells.Default;
using Game.Gameplay.CharactersManagement.Movement;
using UnityEngine;

namespace Game.Gameplay.GridSystem.GenericComponents
{
    [RequireComponent(typeof(CanBeWalkedOnCellComponent))]
    public class KillingCellComponent : MonoBehaviour
    {
        private CanBeWalkedOnCellComponent m_canBeWalkedOnCellComponent;

        private void Awake()
        {
            m_canBeWalkedOnCellComponent = GetComponent<CanBeWalkedOnCellComponent>();
            m_canBeWalkedOnCellComponent.OnMovementControllerOnCellChanged += HandleMovementControllerOnCellChanged;
        }

        private void HandleMovementControllerOnCellChanged(CanBeWalkedOnCellComponent obj)
        {
            if (obj.MovementControllerOnCell)
                obj.MovementControllerOnCell.OnMoveAnimationDone += HandleTargetMoveAnimationDone;
        }

        private void HandleTargetMoveAnimationDone(CharacterMovementController arg1, Cell arg2)
        {
            if (arg1 != m_canBeWalkedOnCellComponent.MovementControllerOnCell)
                return;
            arg1.OnMoveAnimationDone -= HandleTargetMoveAnimationDone;
            arg1?.ReferencesHolder.DeathController.Kill();
        }
    }
}
