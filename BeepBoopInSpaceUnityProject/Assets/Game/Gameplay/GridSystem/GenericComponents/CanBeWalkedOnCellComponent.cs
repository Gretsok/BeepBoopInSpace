using System;
using System.Collections.Generic;
using Game.Gameplay.CharactersManagement.Movement;
using UnityEngine;

namespace Game.Gameplay.GridSystem.GenericComponents
{
    public class CanBeWalkedOnCellComponent : MonoBehaviour
    {
        [SerializeField]
        private List<MeshRenderer> m_outlineBlocks = new();
        private CharacterMovementController m_movementControllerOnCell;

        private Material m_commonOutlineMaterial;
        private void Start()
        {
            m_commonOutlineMaterial = new Material(m_outlineBlocks[0].material);
            m_outlineBlocks?.ForEach(block => block.material = m_commonOutlineMaterial);
            HandleValueChanged();
        }

        public CharacterMovementController MovementControllerOnCell
        {
            get => m_movementControllerOnCell;
            set
            {
                m_movementControllerOnCell = value;
                HandleValueChanged();
            }
        }

        public Action<CanBeWalkedOnCellComponent> OnMovementControllerOnCellChanged;
        private void HandleValueChanged()
        {
            if (m_movementControllerOnCell == null)
            {
                m_outlineBlocks?.ForEach(block => block.gameObject.SetActive(false));
            }
            else
            {
                m_outlineBlocks?.ForEach(block =>
                {
                    block.sharedMaterial.color = m_movementControllerOnCell.ReferencesHolder.CharacterDataAsset.CharacterColor;
                    block.gameObject.SetActive(true);
                });
            }
            OnMovementControllerOnCellChanged?.Invoke(this);
        }
    }
}