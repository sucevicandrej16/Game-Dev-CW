using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
namespace SG {
    [CreateAssetMenu(fileName = "newCraftingRecipe", menuName = "CraftingRecipe")]
    public class CraftingRecipe : ScriptableObject
    {
        [Header("Crafting Recipe")]
        public SlotClass[] inputItems;
        public SlotClass outputItem;



        public bool CanCraft(InventoryManager inventory)
        {
 
        
            if (inventory.isFull() || (OutputIsPotion()) && inventory.FullForPotions()){ 
                return false;
            }
            for (int i = 0; i < inputItems.Length; i++)
            {
                if (!inventory.Contains(inputItems[i].GetItem(), inputItems[i].GetQuantity()))
                {
                    return false;
                }
            }
 
 
            return true;
        }

        public string getInputAsString() { 

            string toReturn = "";

            for (int i = 0; i < inputItems.Length; i++)
            {
                toReturn += inputItems[i].GetQuantity() + "x " + inputItems[i].GetItem().displayName + "\n";
            }
            return toReturn;
        }
        public void Craft(InventoryManager inventory)
        {
 
            for (int i = 0; i < inputItems.Length; i++)
            {
                inventory.Remove(inputItems[i].GetItem(), inputItems[i].GetQuantity());
 
            }
 
            inventory.Add(outputItem.GetItem(), outputItem.GetQuantity());
            ObjectiveManager.Instance.SetEventComplete( "Craft " + outputItem.GetItem().displayName);
        }

        public bool OutputIsPotion() { 
            return outputItem.GetItem() is ConsumableClass && ((ConsumableClass)outputItem.GetItem()).IsPotion;
        }
    }
}