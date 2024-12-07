using UnityEngine;
using System;
using System.Collections.Generic;

namespace Forever.Core
{
    public class InventorySystem : MonoBehaviour
    {
        public static InventorySystem Instance { get; private set; }

        [Header("Inventory Settings")]
        public int maxSlots = 20;
        public float pickupRadius = 2f;
        
        private Dictionary<string, InventoryItem> items;
        private Dictionary<ItemType, int> itemCounts;
        
        public event Action<InventoryItem> OnItemAdded;
        public event Action<InventoryItem> OnItemRemoved;
        public event Action<InventoryItem> OnItemUsed;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeInventory();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeInventory()
        {
            items = new Dictionary<string, InventoryItem>();
            itemCounts = new Dictionary<ItemType, int>();
            
            // Initialize counts for each item type
            foreach (ItemType type in Enum.GetValues(typeof(ItemType)))
            {
                itemCounts[type] = 0;
            }
        }
        
        public bool AddItem(InventoryItem item)
        {
            if (items.Count >= maxSlots)
                return false;
                
            string itemId = item.itemId;
            if (items.ContainsKey(itemId))
            {
                // Stack if stackable
                if (item.isStackable)
                {
                    items[itemId].quantity += item.quantity;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                items.Add(itemId, item);
            }
            
            itemCounts[item.itemType]++;
            OnItemAdded?.Invoke(item);
            return true;
        }
        
        public bool RemoveItem(string itemId, int quantity = 1)
        {
            if (!items.ContainsKey(itemId))
                return false;
                
            InventoryItem item = items[itemId];
            
            if (item.quantity <= quantity)
            {
                items.Remove(itemId);
                itemCounts[item.itemType]--;
            }
            else
            {
                item.quantity -= quantity;
            }
            
            OnItemRemoved?.Invoke(item);
            return true;
        }
        
        public bool UseItem(string itemId)
        {
            if (!items.ContainsKey(itemId))
                return false;
                
            InventoryItem item = items[itemId];
            
            if (item.isUsable)
            {
                item.Use();
                OnItemUsed?.Invoke(item);
                
                if (item.isConsumable)
                {
                    RemoveItem(itemId);
                }
                
                return true;
            }
            
            return false;
        }
        
        public InventoryItem GetItem(string itemId)
        {
            return items.ContainsKey(itemId) ? items[itemId] : null;
        }
        
        public int GetItemCount(ItemType type)
        {
            return itemCounts.ContainsKey(type) ? itemCounts[type] : 0;
        }
        
        public List<InventoryItem> GetItemsByType(ItemType type)
        {
            List<InventoryItem> result = new List<InventoryItem>();
            
            foreach (var item in items.Values)
            {
                if (item.itemType == type)
                {
                    result.Add(item);
                }
            }
            
            return result;
        }
        
        public void Clear()
        {
            items.Clear();
            foreach (ItemType type in Enum.GetValues(typeof(ItemType)))
            {
                itemCounts[type] = 0;
            }
        }
    }
    
    [System.Serializable]
    public class InventoryItem
    {
        public string itemId;
        public string itemName;
        public ItemType itemType;
        public string description;
        public Sprite icon;
        public int quantity = 1;
        public bool isStackable = true;
        public bool isUsable = false;
        public bool isConsumable = false;
        
        public virtual void Use()
        {
            // Override in derived classes
        }
    }
    
    public enum ItemType
    {
        Quest,
        Consumable,
        Equipment,
        Material,
        Collectible
    }
} 