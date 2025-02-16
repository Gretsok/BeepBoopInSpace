using System.Collections.Generic;
using System.Linq;
using Game.Data.Item;
using Game.Data.Recipe;
using Game.Gameplay.HoldingSystem;
using NaughtyAttributes;
using UnityEngine;

namespace Game.Gameplay.ExchangeSystem
{
    public class ExchangeSystem : MonoBehaviour
    {
        [SerializeField] private List<Holder> inputHolders = new ();
        [SerializeField] private List<Item> testItems = new ();
        [SerializeField] private List<SORecipe> recipes = new ();
        private List<Item> inputItems = new ();

        private void Start()
        {
#if UNITY_EDITOR
            if (testItems.Count > 0)
            {
                foreach (var item in testItems)
                {
                    inputItems.Add(item);
                }
            }      
#endif
            foreach (var holder in inputHolders)
            {
                holder.OnCurrentHoldableChanged += OnHoldableChange;
            }
        }

        private void OnHoldableChange(Holder holder, Holdable holdable)
        {
            
        }
        
        [Button("Make Exchange")]
        public void MakeExchange()
        {
            UpdateInputItems();
            if (inputItems.Count == 0) return;
            var matchingRecipe = recipes.FirstOrDefault(r =>
            {
                var usedInstanceID = new List<int>();
                return r.components.All(component =>
                {
                    return inputItems.Exists(i =>
                    {
                        if (i.id == component.id && !usedInstanceID.Contains(i.instanceId))
                        {
                            usedInstanceID.Add(i.instanceId);
                            return true;
                        }
                        return false;
                    });
                });
            });
            if (matchingRecipe == null) 
                FailedExchange();
            else
                SuccessfulExchange(matchingRecipe);
        }

        private void UpdateInputItems()
        {
            inputItems.Clear();
            foreach (var holder in inputHolders)
            {
                if (holder.TryGetComponent(out Item item))
                {
                    inputItems.Add(item);
                }
            }
        }

        private void FailedExchange()
        {
            Debug.Log("Failed");
        }

        private void SuccessfulExchange(SORecipe matchingRecipe)
        {
            Debug.Log("Successful");
        }
    }
}
