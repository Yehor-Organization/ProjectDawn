using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public abstract class APIClientBase : MonoBehaviour
{
    [SerializeField] protected AuthService authService;

    // -----------------------
    // DELETE
    // -----------------------
    protected async Task Delete(string path, bool requiresAuth = true)
    {
        await Send<object>("DELETE", path, null, requiresAuth);
    }

    // -----------------------
    // GET
    // -----------------------
    protected async Task<T> Get<T>(string path, bool requiresAuth = true)
    {
        return await Send<T>("GET", path, null, requiresAuth);
    }

    // -----------------------
    // POST
    // -----------------------
    protected async Task<T> Post<T>(string path, object body, bool requiresAuth = true)
    {
        return await Send<T>("POST", path, body, requiresAuth);
    }

    // -----------------------
    // PUT
    // -----------------------
    protected async Task<T> Put<T>(string path, object body, bool requiresAuth = true)
    {
        return await Send<T>("PUT", path, body, requiresAuth);
    }

    // -----------------------
    // CORE REQUEST
    // -----------------------
    private async Task<T> Send<T>(
        string method,
        string path,
        object body,
        bool requiresAuth)
    {
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
            var token = await authService.GetValidAccessToken();
            if (string.IsNullOrEmpty(token))
                throw new UnauthorizedAccessException("No valid auth token");

            req.SetRequestHeader("Authorization", $"Bearer {token}");
        }

        await req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            throw new Exception(
                $"HTTP {method} {path} failed: {req.responseCode} - {req.error}");
        }

        if (typeof(T) == typeof(object) || string.IsNullOrEmpty(req.downloadHandler.text))
            return default;

        return JsonConvert.DeserializeObject<T>(req.downloadHandler.text);
    }
}