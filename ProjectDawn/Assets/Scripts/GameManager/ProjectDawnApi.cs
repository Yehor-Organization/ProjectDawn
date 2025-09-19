using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Handles the SignalR connection to the server.
/// This script is now controlled by the GameManager and delegates player actions to the PlayerManager.
/// </summary>
[RequireComponent(typeof(GameManager))]
[RequireComponent(typeof(PlayerManager))]

public class ProjectDawnApi : MonoBehaviour
{
    public static ProjectDawnApi Instance { get; private set; }

    [Header("API Settings")]
    [SerializeField]
    private string serverBaseUrl = "https://localhost:7123";

    private GameManager gameManager;
    private PlayerManager playerManager;

    private HubConnection connection;
    private string currentFarmId;
    private int currentPlayerId;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        DontDestroyOnLoad(gameObject);

        playerManager = GetComponent<PlayerManager>();
        gameManager = GetComponent<GameManager>();
        if (playerManager == null)
            Debug.LogError("[DEBUG] PlayerManager component not found on GameObject!");
        if (gameManager == null)
            Debug.LogError("[DEBUG] GameManager component not found on GameObject!");
    }

    public async Task SendObjectPlacement(string typeKey, TransformationDC transformData)
{
    if (connection == null || connection.State != HubConnectionState.Connected)
        return;

    try
    {
        // let the server generate the ObjectId (Guid)
        await connection.InvokeAsync(
            "PlaceObject",
            currentFarmId,
            currentPlayerId,
            typeKey,
            transformData
        );

        Debug.Log($"[DEBUG] Sent object placement → type={typeKey}, transform={JsonUtility.ToJson(transformData)}");
    }
    catch (Exception ex)
    {
        Debug.LogError($"[DEBUG] Error sending object placement: {ex.Message}");
    }
}


    private async Task<T> GetFromApi<T>(string endpoint)
    {
        string url = $"{serverBaseUrl}/{endpoint}";
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            await webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = webRequest.downloadHandler.text;
                return JsonConvert.DeserializeObject<T>(jsonResponse);
            }
            else
            {
                Debug.LogError($"[GameManager] API request to {url} failed: {webRequest.error}");
                return default; // null for ref types, 0/false for value types
            }
        }
    }

    public Task<FarmStateDC> GetFarmState(string farmId)
    {
        return GetFromApi<FarmStateDC>($"api/Farms/{farmId}");
    }

    public Task<List<FarmInfoDC>> GetAllFarms()
    {
        return GetFromApi<List<FarmInfoDC>>("api/Farms");
    }





    public async Task<bool> ConnectAndJoin(string farmId, int playerId)
    {
        this.currentFarmId = farmId;
        this.currentPlayerId = playerId;

        if (playerManager == null)
        {
            Debug.LogError("[DEBUG] PlayerManager is not assigned!");
            return false;
        }

        try
        {
            connection = new HubConnectionBuilder()
                .WithUrl($"{serverBaseUrl}/farmHub", options =>
                {
                    options.Transports = HttpTransportType.WebSockets;
                })
                .WithAutomaticReconnect()
                .Build();

            RegisterEventHandlers();

            Debug.Log("[DEBUG] Starting SignalR connection...");
            await connection.StartAsync();
            Debug.Log("[DEBUG] Connection started successfully.");

            bool joined = await JoinFarmGroup();
            if (!joined)
            {
                Debug.LogWarning("[DEBUG] Failed to join farm.");
                return false;
            }

            playerManager.SpawnPlayer(playerId, true);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DEBUG] Error starting connection: {ex}");
            return false;
        }
    }

    public async Task CloseConnectionAsync()
    {
        if (connection != null)
        {
            try
            {
                if (connection.State == HubConnectionState.Connected)
                    await connection.StopAsync();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DEBUG] Error stopping connection: {ex.Message}");
            }

            connection = null;
            currentFarmId = null;
            currentPlayerId = 0;
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

        connection.On<string>("Kicked", (reason) =>
        {
            Debug.LogWarning($"[SignalR] You were kicked: {reason}");
            MainThreadDispatcher.Enqueue(async () =>
            {
                gameManager.ForceLeaveFarmImmediate();
                await CloseConnectionAsync();
            });
        });


        connection.Closed += (error) =>
        {
            Debug.LogWarning($"[DEBUG] Connection closed. Reason={error?.Message}");

            MainThreadDispatcher.Enqueue(() =>
            {
                gameManager.ForceLeaveFarmImmediate();    
            });

            return Task.CompletedTask;
        };

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

        connection.On<string, string, TransformationDC>("ObjectPlaced", (objectId, typeKey, transformData) =>
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                Debug.Log($"[SignalR] ObjectPlaced → id={objectId}, type={typeKey}");
                ObjectManager.Instance.PlaceObject(Guid.Parse(objectId), typeKey, transformData);
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

        connection.On<int, TransformationDC>("PlayerTransformationUpdated", (updatedPlayerId, newTransformation) =>
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
            MainThreadDispatcher.Enqueue(() =>
                Debug.Log($"[DEBUG] SignalR reconnected. New ConnectionId={connectionId}"));
            await JoinFarmGroup();
        };

        connection.Reconnecting += (error) =>
        {
            Debug.LogWarning($"[DEBUG] Reconnecting... reason={error?.Message}");
            return Task.CompletedTask;
        };
    }

    private async Task<bool> JoinFarmGroup()
    {
        if (connection.State != HubConnectionState.Connected)
        {
            Debug.LogWarning("[DEBUG] Tried to join farm but connection is not connected.");
            return false;
        }

        try
        {
            Debug.Log($"[DEBUG] Invoking JoinFarm for farm={currentFarmId}, player={currentPlayerId}");
            playerManager.ClearAllRemotePlayers();

            await connection.InvokeAsync("JoinFarm", currentFarmId, currentPlayerId);
            Debug.Log("[DEBUG] JoinFarm invoked successfully.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DEBUG] Error joining farm: {ex.Message}");
            return false;
        }
    }

    public async Task SendTransformationUpdate(TransformationDC transformation)
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

    public async Task DisconnectAsync()
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

    private async void OnApplicationQuit()
    {
        Debug.Log("[DEBUG] Application quitting. Disconnecting...");
        await DisconnectAsync();
    }

    private async void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            Debug.Log("[DEBUG] Application paused. Disconnecting...");
            await DisconnectAsync();
        }
    }

    private async void OnDestroy()
    {
        Debug.Log("[DEBUG] OnDestroy called. Disconnecting...");
        await DisconnectAsync();
    }
}
