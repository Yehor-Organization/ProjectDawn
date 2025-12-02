using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    [Header("Inventory Settings")]
    public int capacity = 20; // max slots
    public List<InventorySlot> slots = new List<InventorySlot>();
    public event Action OnInventoryChanged;

    private Dictionary<string, Item> itemRegistry = new Dictionary<string, Item>();
    private bool isInitialized = false;

    private void Start()
    {
        InitializeItemRegistry();
        // Load inventory from API if player is connected
        // Need a way to get current PlayerId. Assuming ProjectDawnApi knows it or we can get it from GameManager
        // Ideally this should be called after connection.
    }

    private void InitializeItemRegistry()
    {
        if (isInitialized) return;

        // Load all items from Resources/Items or a specific folder
        // Assuming Items are in Resources root or subfolder.
        // Based on file list, "New Item.asset" is in Assets.
        // It's better if Items are in a Resources folder so we can load them at runtime.
        // But since I cannot move files easily without breaking references, I will assume there is a way to find them.
        // Or I can use Resources.LoadAll if they are in Resources.

        // Strategy: Load all Item ScriptableObjects from Resources
        // The user should ensure all Item assets are in a Resources folder.
        // If not, I can try to use a pre-assigned list in inspector.

        // Fallback: If no items in Resources, rely on manual assignment or single item logic
        var items = Resources.LoadAll<Item>("");
        foreach (var item in items)
        {
            if (!itemRegistry.ContainsKey(item.itemName))
            {
                itemRegistry.Add(item.itemName, item);
            }
        }
        isInitialized = true;
    }

    public async void LoadInventory(int playerId)
    {
        InitializeItemRegistry();
        slots.Clear();

        var inventoryData = await ProjectDawnApi.Instance.GetInventory(playerId);
        if (inventoryData != null)
        {
            foreach (var invItem in inventoryData)
            {
                if (itemRegistry.TryGetValue(invItem.itemName, out Item item))
                {
                    slots.Add(new InventorySlot(item, invItem.quantity));
                }
                else
                {
                    Debug.LogWarning($"[InventoryManager] Unknown item: {invItem.itemName}");
                }
            }
            OnInventoryChanged?.Invoke();
        }
    }

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
                SyncAdd(item, amount);
                return true;
            }
        }

        // Add new slot if space
        if (slots.Count < capacity)
        {
            slots.Add(new InventorySlot(item, amount));
            OnInventoryChanged?.Invoke();
            SyncAdd(item, amount);
            return true;
        }

        Debug.LogWarning("Inventory full!");
        return false;
    }

    private async void SyncAdd(Item item, int amount)
    {
        if (ProjectDawnApi.Instance != null && ProjectDawnApi.Instance.currentPlayerId != 0)
        {
            await ProjectDawnApi.Instance.AddItem(ProjectDawnApi.Instance.currentPlayerId, item.itemName, amount);
        }
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
            SyncRemove(item, amount);
            return true;
        }
        return false;
    }

    private async void SyncRemove(Item item, int amount)
    {
        if (ProjectDawnApi.Instance != null && ProjectDawnApi.Instance.currentPlayerId != 0)
        {
            await ProjectDawnApi.Instance.RemoveItem(ProjectDawnApi.Instance.currentPlayerId, item.itemName, amount);
        }
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
