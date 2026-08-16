using UnityEngine;

namespace Game.Gameplay.CharactersManagement.Death.Invincibility
{
    public class InvincibilityFeedbackHandler : MonoBehaviour
    {
        [SerializeField]
        private DeathController m_deathController;
        [SerializeField]
        private GameObject m_invincibilityFeedbackGameObject;

        private void Awake()
        {
            m_invincibilityFeedbackGameObject.SetActive(false);
            m_deathController.OnInvincibilityActivated += HandleInvincibilityActivated;
            m_deathController.OnInvincibilityDeactivated += HandleInvincibilityDeactivated;
        }

        private void OnDestroy()
        {
            m_deathController.OnInvincibilityActivated -= HandleInvincibilityActivated;
            m_deathController.OnInvincibilityDeactivated -= HandleInvincibilityDeactivated;
        }

        private void HandleInvincibilityActivated(DeathController controller)
        {
            m_invincibilityFeedbackGameObject.SetActive(true);
        }

        private void HandleInvincibilityDeactivated(DeathController controller)
        {
            m_invincibilityFeedbackGameObject.SetActive(false);
        }
    }
}