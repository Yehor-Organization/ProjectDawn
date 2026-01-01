using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using System.Threading.Tasks;
using UnityEngine;

public abstract class HubClientBase : MonoBehaviour
{
    protected HubConnection connection;

    protected bool IsConnected =>
        connection != null &&
        connection.State == HubConnectionState.Connected;

    protected async Task CreateAndStartConnection(string hubUrl)
    {
        if (connection != null)
            return;

        connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.Transports = HttpTransportType.WebSockets;
                options.AccessTokenProvider = async () =>
                    await Core.Instance.Services.AuthService.GetValidAccessToken();
            })
            .WithAutomaticReconnect()
            .Build();

        await connection.StartAsync();
    }

    protected async Task StopConnection()
    {
        if (connection == null)
            return;

        try
        {
            await connection.StopAsync();
        }
        catch { }

        connection = null;
    }
}