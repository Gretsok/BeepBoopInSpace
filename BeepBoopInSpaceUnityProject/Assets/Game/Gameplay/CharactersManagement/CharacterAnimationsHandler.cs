using System;
using UnityEngine;

namespace Game.Gameplay.CharactersManagement
{
    public class CharacterAnimationsHandler : MonoBehaviour
    {
        private readonly int m_helicePlayAnimationKey = Animator.StringToHash("HelicePlay");
        private readonly int m_moveAnimationKey = Animator.StringToHash("Move");
        private readonly int m_squashAnimationKey = Animator.StringToHash("Squash");
        private readonly int m_victoryAnimationKey = Animator.StringToHash("Victory");
        private readonly int m_loseAnimationKey = Animator.StringToHash("Lose");
        
        private Animator m_animator;

        private void Awake()
        {
            m_animator = GetComponent<Animator>();
        }

        private void Start()
        {
            m_animator.SetBool(m_helicePlayAnimationKey, true);
        }

        public void Move()
        {
            if (!m_animator)
                return;
            m_animator.SetTrigger(m_moveAnimationKey);
        }

        public void Squash()
        {
            if (!m_animator)
                return;
            m_animator.SetTrigger(m_squashAnimationKey);
        }

        public void Lose()
        {
            if (!m_animator)
                return;
            m_animator.SetBool(m_loseAnimationKey, true);
            m_animator.SetBool(m_helicePlayAnimationKey, false);
        }

        public void Win()
        {
            if (!m_animator)
                return;
            m_animator.SetBool(m_victoryAnimationKey, true);
        }
    }
}