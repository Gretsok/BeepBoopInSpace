using UnityEngine;

namespace Game.Gameplay.CharactersManagement.Death
{
    [RequireComponent(typeof(DeathController))]
    public class ModelDuringDeathHandler : MonoBehaviour
    {
        private DeathController m_deathController;

        private void Awake()
        {
            m_deathController = GetComponent<DeathController>();

            m_deathController.OnDeath += HandleDeath;
            m_deathController.OnResurrection += HandleResurrection;
        }

        private void HandleDeath(DeathController obj)
        {
            m_deathController.CharacterReferencesHolder.ModelSource.gameObject.SetActive(false);
        }

        private void HandleResurrection(DeathController obj)
        {
            m_deathController.CharacterReferencesHolder.ModelSource.gameObject.SetActive(true);
        }
    }
}
