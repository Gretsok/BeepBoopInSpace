using UnityEngine;

namespace Game.ArchitectureTools.AddressablesAnchoring
{
    /// <summary>
    /// The bundle of this scene, so all its scenes, depends on the anchor's: loaded before them and kept until they are
    /// gone. Persistent objects keep that bundle's assets, which go away with it (see the unity-build skill).
    /// </summary>
    public class AddressablesAnchorInSceneReferencer : MonoBehaviour
    {
        [SerializeField]
        private AddressablesAnchorDataAsset m_addressablesAnchorDataAsset;
    }
}
