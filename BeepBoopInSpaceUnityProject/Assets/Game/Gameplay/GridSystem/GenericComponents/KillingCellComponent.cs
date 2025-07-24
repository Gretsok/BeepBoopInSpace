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
            obj.MovementControllerOnCell?.ReferencesHolder.DeathController.Kill();
        }
    }
}
