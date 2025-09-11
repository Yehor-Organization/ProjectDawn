using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the spawning, tracking, and updating of all player characters in the scene.
/// This script acts as the central directory for player GameObjects.
/// </summary>
public class PlayerManager : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("The prefab to spawn for the local player (the one you control).")]
    public GameObject localPlayerPrefab;

    [Tooltip("The prefab to spawn for remote players (other players in the game).")]
    public GameObject remotePlayerPrefab;

    // A dictionary to keep track of remote players. The key is the player's ID.
    // The value is the GameObject representing that player. This allows for very fast lookups.
    private readonly Dictionary<int, GameObject> remotePlayers = new Dictionary<int, GameObject>();

    /// <summary>
    /// Spawns a player character. It handles spawning the local player differently
    /// from remote players.
    /// </summary>
    /// <param name="playerId">The ID of the player to spawn.</param>
    /// <param name="isLocalPlayer">Is this the player this client will control?</param>
    public void SpawnPlayer(int playerId, bool isLocalPlayer)
    {
        if (isLocalPlayer)
        {
            Debug.Log($"Spawning LOCAL player with ID: {playerId}");
            if (localPlayerPrefab != null)
            {
                // You might want to spawn at a specific spawn point. For now, we use Vector3.zero.
                Instantiate(localPlayerPrefab, Vector3.zero, Quaternion.identity);
            }
            else
            {
                Debug.LogError("Local Player Prefab is not assigned in the PlayerManager!");
            }
        }
        else
        {
            // If we already have a GameObject for this player, don't spawn another one.
            if (remotePlayers.ContainsKey(playerId))
            {
                return;
            }

            Debug.Log($"Spawning REMOTE player with ID: {playerId}");
            if (remotePlayerPrefab != null)
            {
                GameObject playerGO = Instantiate(remotePlayerPrefab, Vector3.zero, Quaternion.identity);
                remotePlayers.Add(playerId, playerGO); // Add the new player to our directory.
            }
            else
            {
                Debug.LogError("Remote Player Prefab is not assigned in the PlayerManager!");
            }
        }
    }

    /// <summary>
    /// Finds and destroys the GameObject for a player who has left.
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
    /// Finds a player's GameObject by their ID and updates its position.
    /// </summary>
    public void UpdatePlayerPosition(int playerId, Vector3 newPosition)
    {
        if (remotePlayers.TryGetValue(playerId, out GameObject playerGO))
        {
            // In a real game, you would smoothly move the character (e.g., using Lerp)
            // instead of teleporting them instantly. For this example, we'll just set it.
            playerGO.transform.position = newPosition;
        }
    }
}
