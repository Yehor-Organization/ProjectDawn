using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// A simple class to represent player position.
[System.Serializable]
public class PlayerPosition
{
    public float x;
    public float y;
    public float z;
}

/// <summary>
/// Handles the SignalR connection to the server.
/// This script is now controlled by the GameManager and delegates player actions to the PlayerManager.
/// </summary>
public class ProjectDawnApi : MonoBehaviour
{
    [Header("Component References")]
    [Tooltip("Drag the GameObject that has the PlayerManager script on it here.")]
    public PlayerManager playerManager;

    private HubConnection connection;
    private string currentFarmId;
    private int currentPlayerId;

    /// <summary>
    /// Called by the GameManager after the initial world state has been loaded.
    /// </summary>
    public async void ConnectAndJoin(string serverUrl, string farmId, int playerId)
    {
        this.currentFarmId = farmId;
        this.currentPlayerId = playerId;

        if (playerManager == null)
        {
            Debug.LogError("[DEBUG] PlayerManager is not assigned in the ProjectDawnApi inspector! Aborting connection.");
            return;
        }

        // Ensure MainThreadDispatcher exists
        if (FindObjectOfType<MainThreadDispatcher>() == null)
        {
            GameObject dispatcherObj = new GameObject("MainThreadDispatcher");
            dispatcherObj.AddComponent<MainThreadDispatcher>();
        }

        Debug.Log($"[DEBUG] Initializing SignalR connection for farm={farmId}, player={playerId}");

        if (connection != null && connection.State != HubConnectionState.Disconnected)
        {
            Debug.LogWarning("[DEBUG] Connection already active. Skipping ConnectAndJoin.");
            return;
        }

        connection = new HubConnectionBuilder()
            .WithUrl($"{serverUrl}/farmHub")
            .WithAutomaticReconnect()
            .Build();

        RegisterEventHandlers();

        try
        {
            await connection.StartAsync();
            Debug.Log($"[DEBUG] Connection started. State={connection.State}, ConnectionId={connection.ConnectionId}");
            await JoinFarmGroup();

            // Spawn our own local player
            playerManager.SpawnPlayer(currentPlayerId, true);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DEBUG] Error starting connection: {ex.Message}");
        }
    }

    private void RegisterEventHandlers()
    {
        Debug.Log("[DEBUG] Registering SignalR event handlers...");

        connection.On<int>("PlayerJoined", joinedPlayerId =>
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                Debug.Log($"[DEBUG] Event: PlayerJoined → joinedPlayerId={joinedPlayerId}, myId={currentPlayerId}");
                playerManager.SpawnPlayer(joinedPlayerId, false);
            });
        });

        connection.On<List<int>>("InitialPlayers", playerIds =>
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                Debug.Log($"[DEBUG] Event: InitialPlayers → count={playerIds.Count}, myId={currentPlayerId}");
                foreach (var id in playerIds)
                {
                    Debug.Log($"[DEBUG] InitialPlayers includes id={id}");
                    if (id != this.currentPlayerId)
                    {
                        playerManager.SpawnPlayer(id, false);
                    }
                }
            });
        });

        connection.On<int>("PlayerLeft", leftPlayerId =>
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                Debug.Log($"[DEBUG] Event: PlayerLeft → id={leftPlayerId}");
                playerManager.RemovePlayer(leftPlayerId);
            });
        });

        connection.On<int, PlayerPosition>("PlayerPositionUpdated", (updatedPlayerId, newPosition) =>
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                Debug.Log($"[DEBUG] Event: PlayerPositionUpdated → player={updatedPlayerId}, pos=({newPosition.x},{newPosition.y},{newPosition.z}), myId={currentPlayerId}");

                if (updatedPlayerId == this.currentPlayerId)
                {
                    Debug.Log("[DEBUG] Ignoring own PlayerPositionUpdated event");
                    return;
                }

                Vector3 pos = new Vector3(newPosition.x, newPosition.y, newPosition.z);
                playerManager.UpdatePlayerPosition(updatedPlayerId, pos);
            });
        });

        connection.Reconnected += async (connectionId) =>
        {
            MainThreadDispatcher.Enqueue(() => Debug.Log($"[DEBUG] SignalR reconnected. New ConnectionId={connectionId}"));
            await JoinFarmGroup();
        };

        connection.Closed += async (error) =>
        {
            Debug.LogWarning($"[DEBUG] Connection closed. Reason={error?.Message}");
            await Task.Delay(2000);
        };

        connection.Reconnecting += (error) =>
        {
            Debug.LogWarning($"[DEBUG] Reconnecting... reason={error?.Message}");
            return Task.CompletedTask;
        };
    }

    private async Task JoinFarmGroup()
    {
        if (connection.State != HubConnectionState.Connected)
        {
            Debug.LogWarning("[DEBUG] Tried to join farm but connection is not connected.");
            return;
        }

        try
        {
            Debug.Log($"[DEBUG] Invoking JoinFarm for farm={currentFarmId}, player={currentPlayerId}");
            await connection.InvokeAsync("JoinFarm", currentFarmId, currentPlayerId);
            Debug.Log("[DEBUG] JoinFarm invoked successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DEBUG] Error joining farm: {ex.Message}");
        }
    }

    public async Task SendPositionUpdate(Vector3 position)
    {
        if (connection.State != HubConnectionState.Connected)
        {
            Debug.LogWarning("[DEBUG] Tried to send position update but connection is not connected.");
            return;
        }

        try
        {
            Debug.Log($"[DEBUG] Sending position update → player={currentPlayerId}, pos={position}");
            await connection.InvokeAsync("UpdatePlayerPosition", currentFarmId, currentPlayerId, position.x, position.y, position.z);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DEBUG] Error sending position update: {ex.Message}");
        }
    }

    private async void OnDestroy()
    {
        if (connection != null && connection.State == HubConnectionState.Connected)
        {
            await connection.StopAsync();
            Debug.Log("[DEBUG] Connection stopped on destroy.");
        }
    }
}

/// <summary>
/// Ensures Unity actions run on the main thread.
/// </summary>
public class MainThreadDispatcher : MonoBehaviour
{
    private static MainThreadDispatcher _instance;
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue()?.Invoke();
            }
        }
    }

    public static void Enqueue(Action action)
    {
        if (_instance == null)
        {
            GameObject dispatcherObj = new GameObject("MainThreadDispatcher");
            _instance = dispatcherObj.AddComponent<MainThreadDispatcher>();
            DontDestroyOnLoad(dispatcherObj);
        }
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }
}
