using System.Collections.Generic;
using Game.Gameplay.CharactersManagement;
using UnityEngine;

namespace Game.Gameplay.GridSystem
{
    public class CanBeWalkedOnCellComponent : MonoBehaviour
    {
        [SerializeField]
        private List<MeshRenderer> m_outlineBlocks = new();
        private CharacterPawn m_pawnOnCell;

        private Material m_commonOutlineMaterial;
        private void Start()
        {
            m_commonOutlineMaterial = new Material(m_outlineBlocks[0].material);
            m_outlineBlocks?.ForEach(block => block.material = m_commonOutlineMaterial);
            HandleValueChanged();
        }

        public CharacterPawn PawnOnCell
        {
            get => m_pawnOnCell;
            set
            {
                m_pawnOnCell = value;
                HandleValueChanged();
            }
        }

        private void HandleValueChanged()
        {
            if (m_pawnOnCell == null)
            {
                m_outlineBlocks?.ForEach(block => block.gameObject.SetActive(false));
            }
            else
            {
                m_outlineBlocks?.ForEach(block =>
                {
                    block.sharedMaterial.color = m_pawnOnCell.CharacterData.CharacterColor;
                    block.gameObject.SetActive(true);
                });
            }
            
        }
    }
}