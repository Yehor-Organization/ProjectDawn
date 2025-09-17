using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Http.Connections;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

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
    /// Called by the GameManager after the farm is chosen.
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
            .WithUrl($"{serverUrl}/farmHub", options =>
            {
                options.Transports = HttpTransportType.WebSockets;
            })
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
                    if (id != this.currentPlayerId)
                        playerManager.SpawnPlayer(id, false);
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

        connection.On<int, TransformationDataModel>("PlayerTransformationUpdated", (updatedPlayerId, newTransformation) =>
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                if (updatedPlayerId == this.currentPlayerId)
                    return;

                Debug.Log($"[DEBUG][SignalR] Received PlayerTransformationUpdated → player={updatedPlayerId}, data={JsonUtility.ToJson(newTransformation)}");
                playerManager.UpdatePlayerTransformation(updatedPlayerId, newTransformation);
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
            playerManager.ClearAllRemotePlayers();

            await connection.InvokeAsync("JoinFarm", currentFarmId, currentPlayerId);
            Debug.Log("[DEBUG] JoinFarm invoked successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DEBUG] Error joining farm: {ex.Message}");
        }
    }

    public async Task SendTransformationUpdate(TransformationDataModel transformation)
    {
        if (connection == null || connection.State != HubConnectionState.Connected)
            return;

        try
        {
            await connection.InvokeAsync("UpdatePlayerTransformation", currentFarmId, currentPlayerId, transformation);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DEBUG] Error sending transformation update: {ex.Message}");
        }
    }

    public async void Disconnect()
    {
        if (connection != null && connection.State == HubConnectionState.Connected)
        {
            try
            {
                await connection.InvokeAsync("LeaveFarm", currentFarmId, currentPlayerId);
                Debug.Log("[DEBUG] LeaveFarm invoked on server.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DEBUG] Failed to notify server on disconnect: {ex.Message}");
            }

            await connection.StopAsync();
            Debug.Log("[DEBUG] Disconnected from farm hub.");
        }

        connection = null;
        currentFarmId = null;
        currentPlayerId = 0;

        if (playerManager != null)
            playerManager.ClearAllPlayers();

    }

    private void OnApplicationQuit()
    {
        Debug.Log("[DEBUG] Application quitting. Disconnecting...");
        Disconnect();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            Debug.Log("[DEBUG] Application paused. Disconnecting...");
            Disconnect();
        }
    }

    private void OnDestroy()
    {
        Debug.Log("[DEBUG] OnDestroy called. Disconnecting...");
        Disconnect();
    }
}
