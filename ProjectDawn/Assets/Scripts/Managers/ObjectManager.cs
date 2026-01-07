using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectManager : MonoBehaviour
{
    [Header("Object Prefabs")]
    public List<ObjectPrefabMappingDM> objectPrefabs = new List<ObjectPrefabMappingDM>();

    private readonly Dictionary<Guid, GameObject> placedObjects = new();
    private readonly Dictionary<string, GameObject> prefabDictionary = new();
    public static ObjectManager Instance { get; private set; }

    public void ClearAll()
    {
        foreach (var go in placedObjects.Values)
            if (go != null) Destroy(go);

        placedObjects.Clear();
    }

    public GameObject GetPrefab(string typeKey)
    {
        if (prefabDictionary.TryGetValue(typeKey, out var prefab))
        {
            return prefab;
        }

        Debug.LogWarning($"[ObjectManager] No prefab found for object type: {typeKey}");
        return null;
    }

    public void PlaceObject(Guid objectId, string typeKey, TransformationDC transformData)
    {
        if (placedObjects.ContainsKey(objectId))
            return;

        if (!prefabDictionary.TryGetValue(typeKey, out var prefab))
        {
            Debug.LogWarning($"No prefab found for object type: {typeKey}");
            return;
        }

        Vector3 pos = new Vector3(transformData.positionX, transformData.positionY, transformData.positionZ);
        Quaternion rot = Quaternion.Euler(transformData.rotationX, transformData.rotationY, transformData.rotationZ);

        var obj = Instantiate(prefab, pos, rot);
        placedObjects[objectId] = obj;
    }

    public void RemoveObject(Guid objectId)
    {
        if (!placedObjects.TryGetValue(objectId, out var obj))
            return;

        if (obj != null)
            Destroy(obj);

        placedObjects.Remove(objectId);
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        foreach (var mapping in objectPrefabs)
        {
            if (mapping.prefab != null && !string.IsNullOrEmpty(mapping.typeKey))
                prefabDictionary[mapping.typeKey] = mapping.prefab;
        }
    }
}