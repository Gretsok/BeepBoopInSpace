using Game.Gameplay.HoldingSystem;
using UnityEngine;

namespace Game.Gameplay.HoldingSystem
{
    public class HolderReferencer : MonoBehaviour
    {
        [field: SerializeField]
        public Holder Holder { get; private set; }
    }
}