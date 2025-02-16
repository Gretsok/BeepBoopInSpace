using System.Collections.Generic;
using UnityEngine;

namespace Game.Data.Item
{
    public class DebugItemsSpawner : MonoBehaviour
    {
        [SerializeField] private float itemSpacing = 1f;
        [SerializeField] private List<Item> items = new List<Item>();

        private void Start()
        {
            Vector3 pos = Vector3.zero;
            foreach (var item in items)
            {
                Instantiate(item, pos, Quaternion.identity);
                pos += Vector3.right * itemSpacing;
            }
        }
    }
}
