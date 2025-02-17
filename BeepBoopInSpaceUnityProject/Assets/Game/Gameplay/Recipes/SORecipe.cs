using System.Collections.Generic;
using Game.Gameplay.Items;
using UnityEngine;

namespace Game.Gameplay.Recipe
{
    [CreateAssetMenu(fileName = "Recipe", menuName = "Recipe")]
    public class SORecipe : ScriptableObject
    {
        public List<Item> components = new List<Item>();
        public Item reward;
    }
}
