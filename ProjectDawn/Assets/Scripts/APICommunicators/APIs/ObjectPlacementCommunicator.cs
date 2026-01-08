using System.Threading.Tasks;
using UnityEngine;

public class ObjectPlacementCommunicator : APIClientBase
{
    // Optional: if this API always requires auth, you can leave this true
    protected override bool RequiresAuthService => true;

    /// <summary>
    /// Sends object placement data to the server
    /// </summary>
    public async Task<bool> SendPlacement(string typeKey, TransformationDC transform)
    {
        var payload = new
        {
            type = typeKey,
            transformation = transform
        };

        try
        {
            // We don't care about response body → use object
            await Post<object>(
                path: "/Farms/PlaceObject",
                body: payload,
                requiresAuth: true
            );

            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ObjectPlacement] Failed: {ex.Message}");
            return false;
        }
    }
}