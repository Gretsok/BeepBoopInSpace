using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Gameplay.GridSystem
{
    public class CellRandomModelSelector : MonoBehaviour
    {
        [SerializeField]
        private List<GameObject> m_models;

        private void Start()
        {
            int indexToActivate = Random.Range(0, m_models.Count);

            for (int i = 0; i < m_models.Count; i++)
            {
                m_models[i].SetActive(i == indexToActivate);
            }
        }
    }
}