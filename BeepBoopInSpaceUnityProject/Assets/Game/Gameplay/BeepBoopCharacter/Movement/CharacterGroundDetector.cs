using System;
using Game.ArchitectureTools.ActivatableSystem;
using UnityEngine;

namespace Game.Gameplay.BeepBoopCharacter.Movement
{
    public class CharacterGroundDetector : AActivatableSystem
    {
        public Transform Root { get; private set; }

        public void SetDependencies(Transform root)
        {
            Root = root;
        }
        
        
        [SerializeField] 
        private Vector3 m_relativeCenter = Vector3.up * 0.08f;
        [SerializeField]
        private Vector3 m_halfExtents = new Vector3(0.6f, 0.1f, 0.6f);
        [SerializeField] 
        private LayerMask m_layerMask;
        
        public bool IsGrounded { get; private set; }

        private void FixedUpdate()
        {
            if (!IsActivated)
                return;
            
            IsGrounded = Physics.CheckBox(Root.TransformPoint(m_relativeCenter), m_halfExtents, Root.rotation, m_layerMask);
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var root = Root != null ? Root : transform;
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            var oldMatrix = Gizmos.matrix;
            Gizmos.matrix = root.localToWorldMatrix;
            Gizmos.DrawWireCube(m_relativeCenter, m_halfExtents);
            Gizmos.matrix = oldMatrix;
        }
#endif
    }
}