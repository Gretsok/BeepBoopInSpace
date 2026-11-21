using DG.Tweening;
using Game.Characters;
using UnityEngine;

namespace Game.Gameplay.CharactersManagement.Death
{
    [RequireComponent(typeof(ParticleSystem))]
    public class DeathPlacementFX : MonoBehaviour
    {
        [SerializeField]
        private float m_positionHeight = 0.2f;
        private ParticleSystem m_particleSystem;
        private Tween m_moveTween;

        public void SetUp(CharacterDataAsset a_characterDataAsset, Vector3 a_source, Vector3 a_destination, float a_travelDuration)
        {
            if (Vector3.Distance(a_source, a_destination) <= 0.1f)
            {
                Destroy(gameObject);
                return;
            }

            transform.position = a_source + Vector3.up * m_positionHeight;
            
            m_particleSystem = GetComponent<ParticleSystem>();
            
            var mainModule = m_particleSystem.main;
            mainModule.startColor = a_characterDataAsset.CharacterColor;
            
            MoveTo(a_destination, a_travelDuration);
        }

        /// <summary>The robot will come back on another cell: the spark changes course, arriving after the duration.</summary>
        public void Retarget(Vector3 a_destination, float a_travelDuration)
        {
            m_moveTween?.Kill();
            MoveTo(a_destination, a_travelDuration);
        }

        private void MoveTo(Vector3 a_destination, float a_travelDuration)
        {
            m_moveTween = transform.DOMove(a_destination + Vector3.up * m_positionHeight, a_travelDuration)
                .SetLink(gameObject)
                .OnComplete(() => Destroy(gameObject));
        }
    }
}
