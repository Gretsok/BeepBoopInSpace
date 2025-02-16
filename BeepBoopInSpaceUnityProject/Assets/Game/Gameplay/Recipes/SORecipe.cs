using System.Collections.Generic;
using UnityEngine;

namespace Game.Data.Recipe
{
    [CreateAssetMenu(fileName = "Recipe", menuName = "Recipe")]
    public class SORecipe : ScriptableObject
    {
        public List<Item.Item> components = new List<Item.Item>();
        public Item.Item reward;
    }
}
