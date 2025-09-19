using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [Header("Inventory Settings")]
    public int capacity = 20; // max slots
    public List<InventorySlot> slots = new List<InventorySlot>();
    public event Action OnInventoryChanged;

    public bool AddItem(Item item, int amount = 1)
    {
        // Try to stack
        if (item.stackable)
        {
            var slot = slots.Find(s => s.item == item);
            if (slot != null)
            {
                slot.quantity += amount;
                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        // Add new slot if space
        if (slots.Count < capacity)
        {
            slots.Add(new InventorySlot(item, amount));
            OnInventoryChanged?.Invoke();
            return true;
        }

        Debug.LogWarning("Inventory full!");
        return false;
    }

    public bool RemoveItem(Item item, int amount = 1)
    {
        var slot = slots.Find(s => s.item == item);
        if (slot != null)
        {
            slot.quantity -= amount;
            if (slot.quantity <= 0)
                slots.Remove(slot);
            OnInventoryChanged?.Invoke();
            return true;
        }
        return false;
    }

    public bool HasItem(Item item, int amount = 1)
    {
        var slot = slots.Find(s => s.item == item);
        return slot != null && slot.quantity >= amount;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        OnInventoryChanged?.Invoke();
    }
#endif

}
