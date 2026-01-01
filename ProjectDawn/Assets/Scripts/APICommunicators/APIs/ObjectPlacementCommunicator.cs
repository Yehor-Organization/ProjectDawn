using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class ObjectPlacementCommunicator : MonoBehaviour
{
    [SerializeField] private string serverBaseUrl;

    public async Task<bool> SendPlacement(string typeKey, TransformationDC transform)
    {
        var auth = Core.Instance.Services.AuthService;
        string token = await auth.GetValidAccessToken();
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("[ObjectPlacement] No valid access token");
            return false;
        }

        var payload = new
        {
            type = typeKey,
            transformation = transform
        };

        string json = JsonConvert.SerializeObject(payload);

        using var req = new UnityWebRequest(
            $"{serverBaseUrl}/api/Farms/objects",
            UnityWebRequest.kHttpVerbPOST);

        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();

        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Authorization", $"Bearer {token}");

        await req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[ObjectPlacement] Failed: {req.error}");
            return false;
        }

        return true;
    }
}