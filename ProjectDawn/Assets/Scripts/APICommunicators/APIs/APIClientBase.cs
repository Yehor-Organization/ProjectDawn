using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public abstract class APIClientBase : MonoBehaviour
{
    private AuthService authService;

    // 🔹 Lazy resolver (NO Awake dependency)
    protected AuthService Auth
    {
        get
        {
            if (authService == null)
            {
                authService = Core.Instance?.Services?.AuthService;

                if (RequiresAuthService && authService == null)
                {
                    throw new InvalidOperationException(
                        $"[{GetType().Name}] AuthService not available. " +
                        $"Core.Services.AuthService is not ready.");
                }
            }

            return authService;
        }
    }

    /// <summary>
    /// Override to disable auth for public endpoints
    /// </summary>
    protected virtual bool RequiresAuthService => true;

    // -----------------------
    // DELETE
    // -----------------------
    protected Task Delete(string path, bool requiresAuth = true)
    {
        return Send<object>("DELETE", path, null, requiresAuth);
    }

    // -----------------------
    // GET
    // -----------------------
    protected Task<T> Get<T>(string path, bool requiresAuth = true)
    {
        return Send<T>("GET", path, null, requiresAuth);
    }

    // -----------------------
    // POST
    // -----------------------
    protected Task<T> Post<T>(string path, object body, bool requiresAuth = true)
    {
        return Send<T>("POST", path, body, requiresAuth);
    }

    // -----------------------
    // PUT
    // -----------------------
    protected Task<T> Put<T>(string path, object body, bool requiresAuth = true)
    {
        return Send<T>("PUT", path, body, requiresAuth);
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
        //Debug.Log($"[API] Requesting: {url}");

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