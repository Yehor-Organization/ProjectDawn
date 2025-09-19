using System.Collections.Generic;
using UnityEngine;

public class ObjectManager : MonoBehaviour
{
    public static ObjectManager Instance { get; private set; }

    [Header("Object Prefabs")]
    public List<ObjectPrefabMappingDataModel> objectPrefabs = new List<ObjectPrefabMappingDataModel>();

    private readonly Dictionary<string, GameObject> prefabDictionary = new();
    private readonly Dictionary<int, GameObject> placedObjects = new();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        foreach (var mapping in objectPrefabs)
        {
            if (mapping.prefab != null && !string.IsNullOrEmpty(mapping.typeKey))
                prefabDictionary[mapping.typeKey] = mapping.prefab;
        }
    }

    public void PlaceObject(int objectId, string typeKey, TransformationDC transformData)
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

    public void ClearAll()
    {
        foreach (var go in placedObjects.Values)
            if (go != null) Destroy(go);

        placedObjects.Clear();
    }
}
