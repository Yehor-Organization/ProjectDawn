using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public abstract class APIClientBase : MonoBehaviour
{
    private AuthService authService;

    protected AuthService Auth
    {
        get
        {
            if (authService == null)
            {
                authService = Core.Instance?.Services?.AuthService;

                if (RequiresAuthService && authService == null)
                    throw new InvalidOperationException(
                        $"[{GetType().Name}] AuthService not available.");
            }

            return authService;
        }
    }

    protected virtual bool RequiresAuthService => true;

    protected Task Delete(string path, bool requiresAuth = true)
        => Send<object>("DELETE", path, null, requiresAuth);

    protected Task<T> Get<T>(string path, bool requiresAuth = true)
        => Send<T>("GET", path, null, requiresAuth);

    protected Task<T> Post<T>(string path, object body, bool requiresAuth = true)
        => Send<T>("POST", path, body, requiresAuth);

    protected Task<T> Put<T>(string path, object body, bool requiresAuth = true)
        => Send<T>("PUT", path, body, requiresAuth);

    private static async Task WaitWithTimeout(
            Task task,
            int seconds,
            string error)
    {
        if (await Task.WhenAny(task, Task.Delay(seconds * 1000)) != task)
            throw new TimeoutException(error);

        await task;
    }

    // -----------------------
    // CORE REQUEST (UNITY SAFE)
    // -----------------------
    private async Task<T> Send<T>(
        string method,
        string path,
        object body,
        bool requiresAuth)
    {
        Debug.Log($"[API] {method} {path} START");

        var url = $"{Config.APIBaseUrl}{path}";
        using var req = new UnityWebRequest(url, method);
        req.downloadHandler = new DownloadHandlerBuffer();

        if (body != null)
        {
            var json = JsonConvert.SerializeObject(body);
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.SetRequestHeader("Content-Type", "application/json");
        }

        if (requiresAuth)
        {
            var token = await Auth.GetValidAccessToken();
            if (string.IsNullOrEmpty(token))
                throw new UnauthorizedAccessException("User not logged in");

            req.SetRequestHeader("Authorization", $"Bearer {token}");
        }

        // ✅ Unity-safe await
        Debug.Log("[API] SENDWEBREQUEST START");
        await req.SendWebRequest().ToTask();
        Debug.Log("[API] SENDWEBREQUEST END");

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(
                $"[API ERROR] {method} {path}\n" +
                $"Status: {req.responseCode}\n" +
                $"Error: {req.error}\n" +
                $"Response: {req.downloadHandler.text}");

            throw new Exception($"HTTP {req.responseCode}: {req.error}");
        }

        if (typeof(T) == typeof(object) ||
            string.IsNullOrEmpty(req.downloadHandler.text))
            return default;

        Debug.Log("JSON DESERIALIZE START");
        var result = JsonConvert.DeserializeObject<T>(req.downloadHandler.text);
        Debug.Log("JSON DESERIALIZE END");
        return result;
    }

    // -----------------------
    // HELPERS
    // -----------------------
}