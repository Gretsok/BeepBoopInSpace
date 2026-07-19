using UnityEngine;

namespace Game.ArchitectureTools.AddressablesAnchoring
{
    [CreateAssetMenu(fileName = "AddressablesAnchorReferencer", menuName = "Game/ArchitectureTools/AddressablesAnchoring/AnchorReferencer")]
    public class AddressablesAnchorReferencerDataAsset : ScriptableObject
    {
        [SerializeField]
        private AddressablesAnchorDataAsset m_addressablesAnchorDataAsset;
    }
}
