using System.Threading.Tasks;
using UnityEngine;

public class PlayerMovementCommunicator : MonoBehaviour
{
    public async Task SendTransformation(TransformationDC t)
    {
        var hub = Core.Instance.ApiCommunicators.FarmHub;
        if (hub == null) return;

        await hub.SendTransformation(t);
    }
}