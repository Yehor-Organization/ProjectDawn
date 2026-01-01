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

    public async Task<bool> Connect(string farmId)
    {
        this.farmId = farmId;

        await CreateAndStartConnection($"{serverBaseUrl}/farmHub");
        RegisterHandlers();

        await connection.InvokeAsync("JoinFarm", farmId);
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

    private void Awake()
    {
        var managers = Core.Instance.Managers;
        playerManager = managers.PlayerManager;
        gameManager = managers.GameManager;
    }

    private void RegisterHandlers()
    {
        connection.On<int>("PlayerJoined", id =>
            MainThreadDispatcher.Enqueue(() =>
                playerManager.SpawnPlayer(id, false)));

        connection.On<int>("PlayerLeft", id =>
            MainThreadDispatcher.Enqueue(() =>
                playerManager.RemovePlayer(id)));

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
                    playerManager.UpdatePlayerTransformation(id, t)));

        connection.Closed += _ =>
        {
            MainThreadDispatcher.Enqueue(() =>
                gameManager.ForceLeaveFarmImmediate());
            return Task.CompletedTask;
        };
    }
}