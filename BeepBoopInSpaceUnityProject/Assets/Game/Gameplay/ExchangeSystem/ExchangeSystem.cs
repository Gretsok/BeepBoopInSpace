using System.Collections.Generic;
using System.Linq;
using Game.Gameplay.HoldingSystem;
using Game.Gameplay.Items;
using Game.Gameplay.Recipe;
using NaughtyAttributes;
using UnityEngine;

namespace Game.Gameplay.ExchangeSystem
{
    public class ExchangeSystem : MonoBehaviour
    {
        [SerializeField] private List<Holder> inputHolders = new ();
        [SerializeField] private Holder outputHolder;
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
            DestroyComponents(matchingRecipe);
            if (matchingRecipe == null) 
                FailedExchange();
            else
                SuccessfulExchange(matchingRecipe);
        }

        private void DestroyComponents(SORecipe recipe)
        {
            List<Item> itemsToDestroy = new();
            foreach (var component in recipe.components)
            {
                Item item = inputItems.FirstOrDefault(i => i.id == component.id && itemsToDestroy.All(itd => itd.instanceId != i.instanceId));
                if(item != null)
                    itemsToDestroy.Add(item);
                else
                    Debug.LogError("Cannot find item to destroy");
            }

            foreach (var item in itemsToDestroy)
            {
                item.Destroy();
            }
        }

        private void UpdateInputItems()
        {
            inputItems.Clear();
            foreach (var holder in inputHolders)
            {
                if (holder.CurrentHoldable != null && holder.CurrentHoldable.TryGetComponent(out Item item))
                {
                    inputItems.Add(item);
                }
            }
        }

        private void FailedExchange()
        {
            Debug.Log("Exchange Failed");
        }

        private void SuccessfulExchange(SORecipe matchingRecipe)
        {
            Debug.Log("Exchange Successful");
            var reward = Instantiate(matchingRecipe.reward, outputHolder.transform);
            //TODO: replace below with a transform instead of the output holder and just make the object "pop" out with a little random
            outputHolder.TryToHoldHoldable(reward.GetComponent<Holdable>());
        }
    }
}
