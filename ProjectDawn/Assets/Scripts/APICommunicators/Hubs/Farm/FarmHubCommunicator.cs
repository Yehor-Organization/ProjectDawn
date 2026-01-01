using Microsoft.AspNetCore.SignalR.Client;
using System.Threading.Tasks;
using UnityEngine;
using System;

public class FarmHubCommunicator : HubClientBase
{
    private string farmId;

    private GameManager gameManager;
    private PlayerManager playerManager;
    [SerializeField] private string serverBaseUrl;

    public bool IsConnectedPublic
    {
        get => IsConnected;
    }

    // -----------------------
    // Lazy manager resolution
    // -----------------------
    private GameManager GameManager
    {
        get
        {
            if (gameManager == null)
                gameManager = Core.Instance?.Managers?.GameManager;

            if (gameManager == null)
                throw new InvalidOperationException(
                    "[FarmHubCommunicator] GameManager not available.");

            return gameManager;
        }
    }

    private PlayerManager PlayerManager
    {
        get
        {
            if (playerManager == null)
                playerManager = Core.Instance?.Managers?.PlayerManager;

            if (playerManager == null)
                throw new InvalidOperationException(
                    "[FarmHubCommunicator] PlayerManager not available.");

            return playerManager;
        }
    }

    // -----------------------
    // Connection lifecycle
    // -----------------------
    public async Task<bool> Connect(string farmId)
    {
        this.farmId = farmId;

        var baseUrl = Config.APIBaseUrl?.TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl))
            throw new InvalidOperationException(
                "[FarmHubCommunicator] Config.APIBaseUrl is not set");

        var hubUrl = $"{baseUrl}/farmHub";

        Debug.Log($"[FarmHubCommunicator] Connecting to hub: {hubUrl}");

        await CreateAndStartConnection(hubUrl);
        RegisterHandlers();

        // 🔹 Tell server we joined
        await connection.InvokeAsync("JoinFarm", farmId);

        // ✅ SPAWN *LOCAL* PLAYER HERE
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

        try
        {
            await connection.InvokeAsync("LeaveFarm", farmId);
        }
        catch { }

        await StopConnection();
    }

    // -----------------------
    // Hub messaging
    // -----------------------
    public async Task SendTransformation(TransformationDC t)
    {
        if (connection?.State != HubConnectionState.Connected)
            return;

        await connection.SendAsync(
            "UpdatePlayerTransformation",
            farmId,
            t
        );
    }

    // -----------------------
    // Hub event handlers
    // -----------------------
    private void RegisterHandlers()
    {
        connection.On<int>("PlayerJoined", id =>
            MainThreadDispatcher.Enqueue(() =>
                PlayerManager.SpawnPlayer(id, false)));

        connection.On<int>("PlayerLeft", id =>
            MainThreadDispatcher.Enqueue(() =>
                PlayerManager.RemovePlayer(id)));

        connection.On<string, string, TransformationDC>(
            "ObjectPlaced",
            (objectId, typeKey, transform) =>
                MainThreadDispatcher.Enqueue(() =>
                    ObjectManager.Instance.PlaceObject(
                        Guid.Parse(objectId),
                        typeKey,
                        transform)));

        connection.On<int, TransformationDC>(
            "PlayerTransformationUpdated",
            (id, t) =>
                MainThreadDispatcher.Enqueue(() =>
                    PlayerManager.UpdatePlayerTransformation(id, t)));

        connection.Closed += _ =>
        {
            MainThreadDispatcher.Enqueue(() =>
                GameManager.ForceLeaveFarmImmediate());
            return Task.CompletedTask;
        };
    }
}