using DG.Tweening;
using Game.Gameplay.Cells.Default;
using Game.Gameplay.GridSystem.GenericComponents;
using UnityEngine;

namespace Game.Gameplay.GameModes.Meteorites
{
    public class MeteoriteSystem : MonoBehaviour
    {
        [SerializeField]
        private Transform m_meteoriteTransform;

        [SerializeField]
        private float m_startingHeight = 10f;

        [SerializeField]
        private Transform m_imminentImpactVisualContainer;
        [SerializeField]
        private Transform m_imminentImpactVisualScaler;

        public bool ReadyToBeUsed { get; private set; } = false;

        public Cell Target { get; private set; } = null;
        
        public void ResetMeteorite()
        {
            m_meteoriteTransform.DOKill();
            m_imminentImpactVisualScaler.DOKill();
            
            m_meteoriteTransform.gameObject.SetActive(false);
            m_imminentImpactVisualContainer.gameObject.SetActive(false);
            m_imminentImpactVisualScaler.localScale = Vector3.one;
            m_meteoriteTransform.localPosition = new Vector3(0f, m_startingHeight, 0f);
            Target = null;
            
            ReadyToBeUsed = true;
            
            Debug.Log($"Meteorite System Reset");
        }

        public void Drop(Cell target, float dropTime)
        {
            ReadyToBeUsed = false;

            Target = target;
            
            transform.position = target.transform.position;
            
            m_meteoriteTransform.gameObject.SetActive(true);
            m_imminentImpactVisualContainer.gameObject.SetActive(true);
            m_imminentImpactVisualScaler.localScale = Vector3.zero;
            m_meteoriteTransform.DOLocalMoveY(0f, dropTime).SetEase(Ease.InQuad).onComplete += Explode;
            m_imminentImpactVisualScaler.DOScale(1f, dropTime).SetEase(Ease.InQuart);
        }

        private void Explode()
        {
            Debug.Log($"Boom", gameObject);

            if (Target.TryGetComponent<CanBeWalkedOnCellComponent>(out var component))
            {
                component.MovementControllerOnCell?.ReferencesHolder.DeathController.Kill();
            }
            
            ResetMeteorite();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            m_meteoriteTransform.localPosition = new Vector3(0f, m_startingHeight, 0f);
            UnityEditor.EditorUtility.SetDirty(m_meteoriteTransform);
        }
#endif
    }
}
