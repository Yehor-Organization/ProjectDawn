using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;
using UnityEngine;

public abstract class HubClientBase : MonoBehaviour
{
    protected HubConnection connection;
    private string hubPath;

    protected string HubUrl
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Config.APIBaseUrl))
                throw new InvalidOperationException("Config.APIBaseUrl not initialized");
            if (string.IsNullOrWhiteSpace(hubPath))
                throw new InvalidOperationException("Hub path not set");
            return Config.APIBaseUrl.TrimEnd('/') + "/" + hubPath;
        }
    }

    protected bool IsConnected =>
        connection != null &&
        connection.State == HubConnectionState.Connected;

    protected abstract void RegisterHandlers(HubConnection connection);

    protected void SetHubPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Hub path cannot be null or empty.", nameof(path));
        hubPath = path.TrimStart('/');
    }

    protected async Task StartConnectionAsync()
    {
        if (connection != null)
        {
            Debug.LogWarning("[Hub] Connection already exists, disposing first");
            await StopConnectionAsync();
        }

        Debug.Log($"[Hub] Connecting → {HubUrl}");

        connection = new HubConnectionBuilder()
            .WithUrl(HubUrl, options =>
            {
                options.AccessTokenProvider = async () =>
                    await Core.Instance.Services.AuthService.GetValidAccessToken();
            })
            .WithAutomaticReconnect()
            .Build();

        RegisterHandlers(connection);

        connection.Reconnecting += error =>
        {
            Debug.LogWarning($"[Hub] Reconnecting: {error?.Message}");
            return Task.CompletedTask;
        };

        connection.Reconnected += _ =>
        {
            Debug.Log("[Hub] Reconnected");
            RegisterHandlers(connection);
            return Task.CompletedTask;
        };

        connection.Closed += error =>
        {
            if (error != null)
            {
                Debug.LogError($"[Hub] Closed with error: {error.Message}");
            }
            else
            {
                Debug.Log("[Hub] Closed normally");
            }
            return Task.CompletedTask;
        };

        await connection.StartAsync();
        Debug.Log($"[Hub] CONNECTED → {HubUrl}");
    }

    protected async Task StopConnectionAsync()
    {
        if (connection == null)
            return;

        Debug.Log("[Hub] Stopping connection...");

        var tempConnection = connection;
        connection = null; // Clear reference immediately

        try
        {
            // Stop the connection with timeout
            await tempConnection.StopAsync();

            // Dispose the connection to fully clean up resources
            await tempConnection.DisposeAsync();

            Debug.Log("[Hub] Connection stopped and disposed");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Hub] Error during stop: {ex.Message}");
        }
    }
}