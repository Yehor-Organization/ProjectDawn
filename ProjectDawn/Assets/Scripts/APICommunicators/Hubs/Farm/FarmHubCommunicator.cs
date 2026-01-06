using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class FarmHubCommunicator : HubClientBase
{
    // =======================
    // Public events (UI hooks)
    // =======================

    private string farmId;

    private GameManager gameManager;

    // =======================
    // State
    // =======================
    private bool handlersRegistered;

    private PlayerManager playerManager;

    public event Action<FarmInfoDTO> OnFarmJoined;

    public event Action OnFarmListUpdated;

    public bool IsConnectedPublic => IsConnected;

    // =======================
    // Lazy managers
    // =======================

    private PlayerManager PlayerManager =>
        playerManager ??= Core.Instance?.Managers?.PlayerManager
        ?? throw new InvalidOperationException(
            "[FarmHubCommunicator] PlayerManager not available.");

    // =======================
    // Connection lifecycle
    // =======================

    public async Task<bool> Connect(string farmId)
    {
        this.farmId = farmId;

        var baseUrl = Config.APIBaseUrl?.TrimEnd('/');
        var hubUrl = $"{baseUrl}/farmHub";

        await CreateAndStartConnection(hubUrl);
        RegisterHandlers();

        // ✅ CLEAR FIRST
        MainThreadDispatcher.Enqueue(() =>
        {
            PlayerManager.ClearAllPlayers();
        });

        // THEN join
        await connection.InvokeAsync("JoinFarm", farmId);

        // Spawn local AFTER join
        MainThreadDispatcher.Enqueue(() =>
        {
            PlayerManager.SpawnLocalPlayer();
        });

        return true;
    }

    public async Task Disconnect()
    {
        if (!IsConnected)
            return;

        var leavingFarm = farmId;
        farmId = null;

        try
        {
            await connection.InvokeAsync("LeaveFarm", leavingFarm);
        }
        catch { }

        await StopConnection();

        MainThreadDispatcher.Enqueue(() =>
        {
            PlayerManager.ClearAllPlayers();
        });
    }

    // =======================
    // Hub messaging (client → server)
    // =======================

    public async Task SendTransformation(TransformationDC t)
    {
        if (connection?.State != HubConnectionState.Connected || farmId == null)
            return;

        await connection.SendAsync(
            "UpdatePlayerTransformation",
            farmId,
            t
        );
    }

    // =======================
    // SignalR handlers
    // =======================

    private void RegisterHandlers()
    {
        if (handlersRegistered)
            return;

        handlersRegistered = true;

        // ---------- FARM LIST EVENTS ----------

        connection.On("FarmListUpdated", () =>
        {
            if (farmId == null)
                return;

            Debug.Log("Farm list updated event received.");
            MainThreadDispatcher.Enqueue(() =>
            {
                OnFarmListUpdated?.Invoke();
            });
        });

        connection.On<FarmInfoDTO>("FarmJoined", farm =>
        {
            if (farmId == null || farm == null)
                return;

            MainThreadDispatcher.Enqueue(() =>
            {
                OnFarmJoined?.Invoke(farm);
            });
        });

        // ---------- PLAYER EVENTS ----------

        connection.On<int>("PlayerJoined", id =>
        {
            Debug.Log("PlayerJoined");
            if (farmId == null)
                return;

            MainThreadDispatcher.Enqueue(() =>
                PlayerManager.SpawnPlayer(id, false));
        });

        connection.On<int>("PlayerLeft", id =>
        {
            if (farmId == null)
                return;

            MainThreadDispatcher.Enqueue(() =>
                PlayerManager.RemovePlayer(id));
        });

        connection.On<int, TransformationDC>(
            "PlayerTransformationUpdated",
            (id, t) =>
            {
                if (farmId == null)
                    return;

                MainThreadDispatcher.Enqueue(() =>
                    PlayerManager.UpdatePlayerTransformation(id, t));
            });

        // ---------- INITIAL SNAPSHOT ----------
        connection.On<List<int>>("InitialPlayers", ids =>
        {
            if (farmId == null)
                return;

            Debug.Log($"InitialPlayers received: {ids.Count}");

            MainThreadDispatcher.Enqueue(() =>
            {
                foreach (var id in ids)
                    PlayerManager.SpawnPlayer(id, false);
            });
        });

        // ---------- CONNECTION CLOSED ----------

        connection.Closed += async _ =>
        {
            farmId = null;

            MainThreadDispatcher.Enqueue(() =>
            {
                PlayerManager.ClearAllPlayers();
                Core.Instance.Managers.UIManager.ShowMenu();
            });

            await Task.CompletedTask;
        };
    }
}