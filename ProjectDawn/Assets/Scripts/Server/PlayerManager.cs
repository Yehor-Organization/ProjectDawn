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

    // Track remote players by their ID
    private readonly Dictionary<int, GameObject> remotePlayers = new Dictionary<int, GameObject>();

    /// <summary>
    /// Spawns either a local or remote player.
    /// </summary>
    public void SpawnPlayer(int playerId, bool isLocalPlayer)
    {
        if (isLocalPlayer)
        {
            Debug.Log($"Spawning LOCAL player with ID: {playerId}");
            if (localPlayerPrefab != null)
            {
                var go = Instantiate(localPlayerPrefab, Vector3.zero, Quaternion.identity);
                var localCtrl = go.GetComponent<LocalPlayerController>();
                if (localCtrl != null)
                {
                    localCtrl.Initialize(defaultMoveSpeed, defaultRotateSpeed, positionUpdateThreshold, rotationUpdateThreshold);
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
                var playerGO = Instantiate(remotePlayerPrefab, Vector3.zero, Quaternion.identity);
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
