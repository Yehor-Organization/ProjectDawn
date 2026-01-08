using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class FarmListHubCommunicator : HubClientBase
{
    private bool hasRegisteredHandlers = false;

    private bool isInitialized = false;

    public event Action OnFarmListUpdated;

    protected override void RegisterHandlers(HubConnection connection)
    {
        // Prevent duplicate handler registration
        if (hasRegisteredHandlers)
        {
            Debug.LogWarning("[FarmListHub] Handlers already registered, skipping");
            return;
        }

        hasRegisteredHandlers = true;

        // Register the FarmListUpdated handler
        connection.On("FarmListUpdated", () =>
        {
            Debug.Log("[FarmListHub] FarmListUpdated received");
            MainThreadDispatcher.Enqueue(() =>
            {
                OnFarmListUpdated?.Invoke();
            });
        });

        Debug.Log("[FarmListHub] Handlers registered");
    }

    private void Awake()
    {
        SetHubPath("FarmListHub");
    }

    private async void OnApplicationQuit()
    {
        // Only disconnect when the application is actually quitting
        if (IsConnected)
        {
            Debug.Log("[FarmListHub] Disconnecting on application quit");
            await StopConnectionAsync();
        }
    }

    // ❌ REMOVE OnEnable/OnDisable - they cause disconnects on focus changes!
    // The connection should stay alive for the entire application lifetime
    private void OnDestroy()
    {
        // Clean up when this GameObject is destroyed
        if (IsConnected)
        {
            Debug.Log("[FarmListHub] Disconnecting on destroy");
            _ = StopConnectionAsync(); // Fire and forget since we're destroying
        }
    }

    private async void Start()
    {
        if (isInitialized)
            return;

        isInitialized = true;

        // Wait until Core & Config are ready
        while (string.IsNullOrEmpty(Config.APIBaseUrl))
        {
            await Task.Yield();
        }

        try
        {
            await StartConnectionAsync();
            Debug.Log("[FarmListHub] Successfully connected in Start");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FarmListHub] Failed to connect: {ex.Message}");
        }
    }
}