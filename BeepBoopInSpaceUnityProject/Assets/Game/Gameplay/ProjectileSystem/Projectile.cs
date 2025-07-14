using System;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Gameplay.ProjectileSystem
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField]
        private float m_speed = 8f;

        [SerializeField]
        private float m_maxLifeSpan = 8f;

        private float m_startTime;
        
        [SerializeField]
        private UnityEvent m_onTriggerEntered_UnityEvent;
        
        public event Action<Collider, Projectile> OnTriggerEnter_Event;
        private event Action<Collider, Projectile> m_onTriggerEnter_Event;
        
        public void SetUp(Vector3 startingPosition, Vector3 direction, Action<Collider, Projectile> onTriggerEntered = null)
        {
            direction.Normalize();
            
            transform.position = startingPosition;
            transform.LookAt(startingPosition + direction);
            m_onTriggerEnter_Event = onTriggerEntered;
            
            m_startTime = Time.time; 
        }

        private void FixedUpdate()
        {
            transform.position += transform.forward * (m_speed * Time.deltaTime);
            
            if (Time.time - m_startTime > m_maxLifeSpan)
                Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            m_onTriggerEntered_UnityEvent?.Invoke();
            OnTriggerEnter_Event?.Invoke(other, this);
            m_onTriggerEnter_Event?.Invoke(other, this);
        }
    }
}
