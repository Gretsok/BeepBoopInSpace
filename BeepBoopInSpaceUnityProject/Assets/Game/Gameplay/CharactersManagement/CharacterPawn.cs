using DG.Tweening;
using Game.Gameplay.GridSystem;
using UnityEngine;

namespace Game.Gameplay.CharactersManagement
{
    public class CharacterPawn : MonoBehaviour
    {
        [field: SerializeField] 
        public GridWalker GridWalker { get; private set; }

        [field: SerializeField]
        public Transform ModelSource { get; private set; }

        public void SetModel(Transform model)
        {
            while (ModelSource.childCount > 0)
            {
                var child = ModelSource.GetChild(0);
                Destroy(child.gameObject);
            }
            
            Instantiate(model, ModelSource);
        }
        
        public void MoveToCell(Cell cell)
        {
            GridWalker.MoveToCell(cell);
            transform.DOJump(GridWalker.transform.position, 0.5f, 1, 0.2f);
        }

        public void TeleportToCell(Cell cell)
        {
            GridWalker.MoveToCell(cell);
            transform.position = GridWalker.transform.position;
        }
        
    }
}