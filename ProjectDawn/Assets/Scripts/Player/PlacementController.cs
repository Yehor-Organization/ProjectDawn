using UnityEngine;
using UnityEngine.InputSystem;

public class PlacementController : MonoBehaviour
{

    [Header("Placement Settings")]
    [SerializeField]
    private string objectTypeToPlace = "Tree";

    [Tooltip("Maximum distance from player at which objects can be placed.")]
    [SerializeField]
    private float placementRadius = 5f;




    private int nextObjectId = 1000;

    private void Awake()
    {
        
    }

    void Update()
    {
        // Works for both mouse and touchscreen
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryPlaceAtScreenPosition(Mouse.current.position.ReadValue());
        }
        else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            TryPlaceAtScreenPosition(Touchscreen.current.primaryTouch.position.ReadValue());
        }
    }

    private void TryPlaceAtScreenPosition(Vector2 screenPosition)
    {
        Ray ray = CameraManager.Instance.GetCamera().ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // ✅ Check if hit point is within placementRadius of the player
            float distance = Vector3.Distance(transform.position, hit.point);

            if (distance <= placementRadius)
            {
                ObjectManager.Instance.PlaceObject(
                    nextObjectId++,
                    objectTypeToPlace,
                    TransformationDC.FromPosition(hit.point)
                );
            }
            else
            {
                Debug.LogWarning($"[PlacementController] Too far! Hit point at {distance:F1} units, " +
                                 $"but max placement radius is {placementRadius}.");
            }
        }
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, placementRadius);
    }
}
