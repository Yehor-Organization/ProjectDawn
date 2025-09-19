using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    public InventoryManager inventory;   // assign Player’s InventoryController
    public GameObject slotPrefab;           // assign InventorySlot prefab
    public Transform slotContainer;            // where slots will be created (like a GridLayoutGroup)

    private readonly List<GameObject> spawnedSlots = new List<GameObject>();

    void Start()
    {
        RefreshUI();
    }

    void OnEnable()
    {
        if (inventory != null)
            inventory.OnInventoryChanged += RefreshUI;
    }

    void OnDisable()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= RefreshUI;
    }


    public void RefreshUI()
    {
        // Clear old slots
        foreach (var slot in spawnedSlots)
            Destroy(slot);
        spawnedSlots.Clear();

        // Spawn slots up to capacity
        for (int i = 0; i < inventory.capacity; i++)
        {
            GameObject slotGO = Instantiate(slotPrefab, slotContainer);
            spawnedSlots.Add(slotGO);

            Image icon = slotGO.transform.Find("Icon").GetComponent<Image>();
            TextMeshProUGUI quantityText = slotGO.transform.Find("QuantityText").GetComponent<TextMeshProUGUI>();

            // Does inventory have an item for this slot index?
            if (i < inventory.slots.Count)
            {
                var invSlot = inventory.slots[i];
                icon.sprite = invSlot.item.icon;
                icon.enabled = true; // show icon
                quantityText.text = invSlot.item.stackable ? invSlot.quantity.ToString() : "";
            }
            else
            {
                // Empty slot
                icon.enabled = false;
                quantityText.text = "";
            }
        }
    }

}
