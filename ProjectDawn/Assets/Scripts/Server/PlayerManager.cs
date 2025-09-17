using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the spawning, tracking, and updating of all player characters in the scene.
/// This script acts as the central directory for player GameObjects.
/// </summary>
public class PlayerManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject localPlayerPrefab;
    public GameObject remotePlayerPrefab;

    [Header("Default Settings")]
    [Tooltip("Default movement speed for all players.")]
    public float defaultMoveSpeed = 15f;

    [Tooltip("Default rotation speed for all players.")]
    public float defaultRotateSpeed = 300f;

    [Tooltip("Minimum distance change before sending a position update.")]
    public float positionUpdateThreshold = 0.01f;
    public float rotationUpdateThreshold = 0.01f;

    [Header("Spawn Settings")]
    public Transform defaultSpawnPoint;
    public float spawnHeightBuffer = 2f;

    // Track remote players by their ID
    private readonly Dictionary<int, GameObject> remotePlayers = new Dictionary<int, GameObject>();

    /// <summary>
    /// Spawns either a local or remote player.
    /// </summary>
    public void SpawnPlayer(int playerId, bool isLocalPlayer)
    {
        // Base spawn pos/rot
        Vector3 spawnPos = defaultSpawnPoint != null ? defaultSpawnPoint.position : Vector3.zero;
        Quaternion spawnRot = defaultSpawnPoint != null ? defaultSpawnPoint.rotation : Quaternion.identity;

        // ✅ Adjust Y based on terrain height
        if (Terrain.activeTerrain != null)
        {
            float groundY = Terrain.activeTerrain.SampleHeight(spawnPos);
            spawnPos.y = groundY + spawnHeightBuffer;
        }
        if (isLocalPlayer)
        {
            Debug.Log($"Spawning LOCAL player with ID: {playerId}");
            if (localPlayerPrefab != null)
            {
                var go = Instantiate(localPlayerPrefab, spawnPos, spawnRot);
                var localCtrl = go.GetComponent<LocalPlayerController>();
                if (localCtrl != null)
                {
                    // Initialize movement and network settings
                    localCtrl.Initialize(defaultMoveSpeed, defaultRotateSpeed, positionUpdateThreshold, rotationUpdateThreshold);

                    // Reset the camera using the persistent CameraManager
                    if (CameraManager.Instance != null)
                    {
                        CameraManager.Instance.ResetCamera(localCtrl);
                    }
                }
                else
                {
                    Debug.LogWarning("LocalPlayer prefab missing LocalPlayerController!");
                }
            }
            else
            {
                Debug.LogError("Local Player Prefab is not assigned in the PlayerManager!");
            }
        }

        else
        {
            if (remotePlayers.ContainsKey(playerId)) return;

            Debug.Log($"Spawning REMOTE player with ID: {playerId}");
            if (remotePlayerPrefab != null)
            {
                var playerGO = Instantiate(remotePlayerPrefab, spawnPos, spawnRot);
                var remoteCtrl = playerGO.GetComponent<RemotePlayerController>();
                if (remoteCtrl != null)
                {
                    remoteCtrl.Initialize(defaultMoveSpeed, defaultRotateSpeed);
                }

                remotePlayers.Add(playerId, playerGO);
            }
            else
            {
                Debug.LogError("Remote Player Prefab is not assigned in the PlayerManager!");
            }
        }
    }

    public void ClearAllPlayers()
    {
        // Remove remote players
        ClearAllRemotePlayers();

        // Remove local player (if spawned)
        var local = FindObjectOfType<LocalPlayerController>();
        if (local != null)
            Destroy(local.gameObject);
    }


    /// <summary>
    /// Removes a remote player by ID.
    /// </summary>
    public void RemovePlayer(int playerId)
    {
        if (remotePlayers.TryGetValue(playerId, out GameObject playerGO))
        {
            Debug.Log($"Removing player with ID: {playerId}");
            Destroy(playerGO);
            remotePlayers.Remove(playerId);
        }
    }

    /// <summary>
    /// Clears all remote players from the scene.
    /// </summary>
    public void ClearAllRemotePlayers()
    {
        foreach (var kvp in remotePlayers)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value);
        }
        remotePlayers.Clear();
    }

    /// <summary>
    /// Updates a remote player’s position/rotation from the network.
    /// </summary>
    public void UpdatePlayerTransformation(int playerId, TransformationDataModel newTransformation)
    {
        if (remotePlayers.TryGetValue(playerId, out GameObject playerObj))
        {
            var remote = playerObj.GetComponent<RemotePlayerController>();
            if (remote != null)
            {
                remote.SetTargetTransformation(newTransformation);
            }
            else
            {
                Debug.LogWarning($"[DEBUG][PlayerManager] No RemotePlayerController found for player {playerId} ({playerObj.name})");
            }
        }
        else
        {
            Debug.LogWarning($"[DEBUG][PlayerManager] Player {playerId} not found in dictionary!");
        }
    }
}