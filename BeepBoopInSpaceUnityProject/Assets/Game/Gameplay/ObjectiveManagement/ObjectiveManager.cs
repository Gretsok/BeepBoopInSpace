using System.Collections;
using Game.ArchitectureTools.Manager;
using Game.Gameplay.CharactersManagement;
using Game.Gameplay.GridSystem;
using UnityEngine;

namespace Game.Gameplay.ObjectiveManagement
{
    public class ObjectiveManager : AManager<ObjectiveManager>
    {
        [SerializeField] private GameObject m_objectiveIndicationPrefab;
        private GameObject m_currentObjectiveIndication;
        public Cell CurrentObjectiveCell { get; private set; }
        
        private GridBuilder m_gridBuilder;
        private CharactersManager m_charactersManager;
        protected override IEnumerator Initialize()
        {
            GridBuilder.RegisterPostInitializationCallback(manager => m_gridBuilder = manager);
            CharactersManager.RegisterPostInitializationCallback(manager =>
            {
                m_charactersManager = manager;
            });
            
            yield return null;
        }

        private void HandlePawnMove(CharacterPawn pawn, Cell cell)
        {
            if (cell == CurrentObjectiveCell)
            {
                pawn.IncreaseScore();
                UpdateObjective();
            }
        }

        public void SetUp()
        {
            foreach (var pawn in m_charactersManager.CharacterPawns)
            {
                pawn.OnMove += HandlePawnMove;
            }
            UpdateObjective();
        }
        
        private void UpdateObjective()
        {
            if (m_currentObjectiveIndication != null)
                Destroy(m_currentObjectiveIndication);

            do
            {
                CurrentObjectiveCell = m_gridBuilder.GetRandomCell();
            } while (DoesAPawnStandOnCell(CurrentObjectiveCell));
            
            m_currentObjectiveIndication = Instantiate(m_objectiveIndicationPrefab, CurrentObjectiveCell.transform);
        }

        private bool DoesAPawnStandOnCell(Cell cell)
        {
            foreach (var pawn in m_charactersManager.CharacterPawns)
            {
                if (pawn.GridWalker.CurrentCell == cell)
                    return true;
            }

            return false;
        }
    }
}