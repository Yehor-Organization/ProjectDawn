using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the spawning, tracking, and updating of all player characters in the scene.
/// This script acts as the central directory for player GameObjects.
/// </summary>
public class PlayerManager : MonoBehaviour
{
    private readonly Dictionary<int, GameObject> remotePlayers = new();

    private CameraManager cameraManager;

    [Header("Default Settings")]
    [SerializeField] private float defaultMoveSpeed = 15f;

    [SerializeField] private float defaultRotateSpeed = 300f;

    [Header("Spawn Settings")]
    [SerializeField] private Transform defaultSpawnPoint;

    [Header("Prefabs")]
    [SerializeField] private GameObject localPlayerPrefab;

    // 🔗 Dependencies (resolved via Core)
    private ObjectManager objectManager;

    [SerializeField] private float positionUpdateThreshold = 0.01f;
    [SerializeField] private GameObject remotePlayerPrefab;
    [SerializeField] private float rotationUpdateThreshold = 0.01f;
    [SerializeField] private float spawnHeightBuffer = 2f;

    public void ClearAllPlayers()
    {
        ClearAllRemotePlayers();

        var local = FindObjectOfType<LocalPlayerController>();
        if (local != null)
            Destroy(local.gameObject);
    }

    public void ClearAllRemotePlayers()
    {
        foreach (var kvp in remotePlayers)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value);
        }
        remotePlayers.Clear();
    }

    public void RemovePlayer(int playerId)
    {
        if (!remotePlayers.TryGetValue(playerId, out var playerGO))
            return;

        Debug.Log($"[PlayerManager] Removing player {playerId}");
        Destroy(playerGO);
        remotePlayers.Remove(playerId);
    }

    /// <summary>
    /// Spawns either a local or remote player.
    /// </summary>
    public void SpawnPlayer(int playerId, bool isLocalPlayer)
    {
        Vector3 spawnPos = defaultSpawnPoint ? defaultSpawnPoint.position : Vector3.zero;
        Quaternion spawnRot = defaultSpawnPoint ? defaultSpawnPoint.rotation : Quaternion.identity;

        if (Terrain.activeTerrain != null)
        {
            float groundY = Terrain.activeTerrain.SampleHeight(spawnPos);
            spawnPos.y = groundY + spawnHeightBuffer;
        }

        if (isLocalPlayer)
        {
            Debug.Log($"[PlayerManager] Spawning LOCAL player {playerId}");

            if (localPlayerPrefab == null)
            {
                Debug.LogError("[PlayerManager] Local player prefab not assigned");
                return;
            }

            var go = Instantiate(localPlayerPrefab, spawnPos, spawnRot);

            var localCtrl = go.GetComponent<LocalPlayerController>();
            if (localCtrl == null)
            {
                Debug.LogWarning("[PlayerManager] LocalPlayerController missing on prefab");
                return;
            }

            localCtrl.Initialize(
                defaultMoveSpeed,
                defaultRotateSpeed,
                positionUpdateThreshold,
                rotationUpdateThreshold
            );

            cameraManager?.ResetCamera(localCtrl);
        }
        else
        {
            if (remotePlayers.ContainsKey(playerId))
                return;

            Debug.Log($"[PlayerManager] Spawning REMOTE player {playerId}");

            if (remotePlayerPrefab == null)
            {
                Debug.LogError("[PlayerManager] Remote player prefab not assigned");
                return;
            }

            var playerGO = Instantiate(remotePlayerPrefab, spawnPos, spawnRot);
            var remoteCtrl = playerGO.GetComponent<RemotePlayerController>();

            remoteCtrl?.Initialize(defaultMoveSpeed, defaultRotateSpeed);

            remotePlayers.Add(playerId, playerGO);
        }
    }

    public void UpdatePlayerTransformation(int playerId, TransformationDC newTransformation)
    {
        if (!remotePlayers.TryGetValue(playerId, out var playerObj))
        {
            Debug.LogWarning($"[PlayerManager] Player {playerId} not found");
            return;
        }

        var remote = playerObj.GetComponent<RemotePlayerController>();
        if (remote == null)
        {
            Debug.LogWarning($"[PlayerManager] RemotePlayerController missing for {playerId}");
            return;
        }

        remote.SetTargetTransformation(newTransformation);
    }

    private void Awake()
    {
        // Resolve dependencies from Core
        var managers = Core.Instance.Managers;

        objectManager = managers.ObjectManager;
        cameraManager = managers.CameraManager;

        if (objectManager == null)
            Debug.LogError("[PlayerManager] ObjectManager missing in Core.Managers");

        if (cameraManager == null)
            Debug.LogError("[PlayerManager] CameraManager missing in Core.Managers");
    }
}