using UnityEngine;

namespace Game.Tools
{
    public class OnAwakeDeactivator : MonoBehaviour
    {
        private void Awake()
        {
            gameObject.SetActive(false);
        }
    }
}
