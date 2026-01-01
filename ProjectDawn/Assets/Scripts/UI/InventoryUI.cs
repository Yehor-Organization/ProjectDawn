using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    public InventoryManager inventory;      // Player InventoryManager

    public Transform slotContainer;
    public GameObject slotPrefab;
    private readonly List<GameObject> spawnedSlots = new List<GameObject>();

    // -----------------------
    // Lazy inventory resolve
    // -----------------------
    private InventoryManager Inventory
    {
        get
        {
            if (inventory == null)
                inventory = Core.Instance?.Managers?.InventoryManager;

            return inventory;
        }
    }

    public void RefreshUI()
    {
        // 🔒 Safety guard (VERY important)
        if (Inventory == null)
            return;

        // Clear old slots
        foreach (var slot in spawnedSlots)
            Destroy(slot);
        spawnedSlots.Clear();

        // Spawn slots up to capacity
        for (int i = 0; i < Inventory.capacity; i++)
        {
            GameObject slotGO = Instantiate(slotPrefab, slotContainer);
            spawnedSlots.Add(slotGO);

            Image icon = slotGO.transform.Find("Icon")?.GetComponent<Image>();
            TextMeshProUGUI quantityText =
                slotGO.transform.Find("QuantityText")?.GetComponent<TextMeshProUGUI>();

            if (icon == null || quantityText == null)
                continue;

            // Does inventory have an item for this slot index?
            if (i < Inventory.slots.Count)
            {
                var invSlot = Inventory.slots[i];
                icon.sprite = invSlot.item.icon;
                icon.enabled = true;
                quantityText.text =
                    invSlot.item.stackable ? invSlot.quantity.ToString() : "";
            }
            else
            {
                // Empty slot
                icon.enabled = false;
                quantityText.text = "";
            }
        }
    }

    private void OnDisable()
    {
        if (Inventory != null)
            Inventory.OnInventoryChanged -= RefreshUI;
    }

    private void OnEnable()
    {
        if (Inventory != null)
            Inventory.OnInventoryChanged += RefreshUI;
    }

    private void Start()
    {
        TryRefreshUI();
    }

    // -----------------------
    // SAFE refresh entry
    // -----------------------
    private void TryRefreshUI()
    {
        if (Inventory == null)
        {
            Debug.Log("[InventoryUI] Inventory not ready yet");
            return;
        }

        RefreshUI();
    }
}