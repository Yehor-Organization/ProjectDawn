using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class FarmHubCommunicator : HubClientBase
{
    private string farmId;
    private bool handlersRegistered;
    private PlayerManager playerManager;

    public event Action<FarmInfoDTO> OnFarmJoined;

    public bool IsConnectedPublic => IsConnected;

    private PlayerManager PlayerManager =>
        playerManager ??= Core.Instance.Managers.PlayerManager
        ?? throw new InvalidOperationException("PlayerManager not available");

    // =======================
    // Connection
    // =======================
    public async Task<bool> Connect(string farmId)
    {
        this.farmId = farmId;

        // 1. Clear all players FIRST
        MainThreadDispatcher.Enqueue(PlayerManager.ClearAllPlayers);

        // 2. Connect to hub and join farm
        await StartConnectionAsync();
        await connection.InvokeAsync("JoinFarm", farmId);

        // 3. ALWAYS spawn local player immediately after joining
        // This ensures local player exists regardless of InitialPlayers event
        MainThreadDispatcher.Enqueue(() =>
        {
            PlayerManager.SpawnLocalPlayer();
            Debug.Log("[FarmHub] Local player spawned after joining farm");
        });

        return true;
    }

    public async Task Disconnect()
    {
        if (connection == null || farmId == null)
        {
            Debug.Log("[FarmHub] Disconnect called but already disconnected");
            return;
        }

        Debug.Log($"[FarmHub] Disconnecting from farm {farmId}");

        var leavingFarm = farmId;
        farmId = null;

        // Try to notify server we're leaving
        try
        {
            if (IsConnected)
            {
                await connection.InvokeAsync("LeaveFarm", leavingFarm);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[FarmHub] LeaveFarm failed: {ex.Message}");
        }

        // Stop the connection and wait for it to fully close
        try
        {
            await StopConnectionAsync();
            Debug.Log("[FarmHub] Connection stopped successfully");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[FarmHub] StopConnection failed: {ex.Message}");
        }

        // 🔑 Reset handler flag so they can be re-registered on next connect
        handlersRegistered = false;

        // Clear players on main thread
        MainThreadDispatcher.Enqueue(PlayerManager.ClearAllPlayers);
    }

    // =======================
    // Sending
    // =======================
    public async Task SendTransformation(TransformationDC t)
    {
        if (!IsConnected || farmId == null)
            return;

        await connection.SendAsync("UpdatePlayerTransformation", farmId, t);
    }

    protected void Awake()
    {
        SetHubPath("FarmHub");
    }

    // =======================
    // Handlers
    // =======================
    protected override void RegisterHandlers(HubConnection connection)
    {
        if (handlersRegistered)
            return;

        handlersRegistered = true;

        // -------- FARM JOINED --------
        connection.On<FarmInfoDTO>("FarmJoined", farm =>
        {
            if (farm == null)
                return;

            MainThreadDispatcher.Enqueue(() =>
                OnFarmJoined?.Invoke(farm));
        });

        // -------- INITIAL PLAYERS (Remote only) --------
        connection.On<List<int>>("InitialPlayers", ids =>
        {
            if (farmId == null || ids == null)
                return;

            MainThreadDispatcher.Enqueue(() =>
            {
                int localId = PlayerManager.GetLocalPlayerID();

                Debug.Log($"[FarmHub] Received InitialPlayers: {ids.Count} players");

                // Spawn only REMOTE players
                // Local player was already spawned in Connect()
                foreach (var id in ids)
                {
                    if (id == localId)
                    {
                        Debug.Log($"[FarmHub] Skipping local player {id} in InitialPlayers");
                        continue;
                    }

                    Debug.Log($"[FarmHub] Spawning remote player {id}");
                    PlayerManager.SpawnPlayer(id, false);
                }
            });
        });

        // -------- INITIAL PLAYER STATES (positions) --------
        connection.On<Dictionary<int, TransformationDC>>("InitialPlayerStates", states =>
        {
            if (farmId == null || states == null)
                return;

            MainThreadDispatcher.Enqueue(() =>
            {
                Debug.Log($"[FarmHub] Received InitialPlayerStates: {states.Count} transforms");

                foreach (var kvp in states)
                {
                    int playerId = kvp.Key;
                    TransformationDC transform = kvp.Value;

                    Debug.Log($"[FarmHub] Setting initial position for player {playerId}");
                    PlayerManager.UpdatePlayerTransformation(playerId, transform);
                }
            });
        });

        // -------- PLAYER JOINED --------
        connection.On<int>("PlayerJoined", id =>
        {
            if (farmId == null)
                return;

            int localPlayerId = PlayerManager.GetLocalPlayerID();

            // Never spawn self as remote
            if (id == localPlayerId)
            {
                Debug.Log($"[FarmHub] Ignoring PlayerJoined for self ({id})");
                return;
            }

            Debug.Log($"[FarmHub] Remote player {id} joined");
            MainThreadDispatcher.Enqueue(() =>
                PlayerManager.SpawnPlayer(id, false));
        });

        // -------- PLAYER LEFT --------
        connection.On<int>("PlayerLeft", id =>
        {
            if (farmId == null)
                return;

            Debug.Log($"[FarmHub] Player {id} left");
            MainThreadDispatcher.Enqueue(() =>
                PlayerManager.RemovePlayer(id));
        });

        // -------- TRANSFORM UPDATE --------
        connection.On<int, TransformationDC>("PlayerTransformationUpdated",
            (id, t) =>
            {
                if (farmId == null)
                    return;

                MainThreadDispatcher.Enqueue(() =>
                    PlayerManager.UpdatePlayerTransformation(id, t));
            });
    }
}