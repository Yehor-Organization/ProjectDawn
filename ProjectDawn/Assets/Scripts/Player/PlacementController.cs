using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlacementController : MonoBehaviour
{
    // 🔗 Dependencies
    private CameraManager cameraManager;

    // Prevent spamming placement requests
    private bool isPlacing;

    private ObjectManager objectManager;

    [Header("Placement Settings")]
    [SerializeField] private string objectTypeToPlace = "Tree";

    private ObjectPlacementCommunicator placementCommunicator;

    [SerializeField] private float placementRadius = 5f;
    [SerializeField] private float playerMaskRadius = 0f;

    private GameObject previewInstance;
    private bool previewValid;
    // =======================
    // Unity lifecycle
    // =======================

    private void Awake()
    {
        var core = Core.Instance;

        cameraManager = core?.Managers?.CameraManager;
        objectManager = core?.Managers?.ObjectManager;
        placementCommunicator = core?.ApiCommunicators?.ObjectPlacement;

        if (cameraManager == null)
            Debug.LogError("[PlacementController] CameraManager missing");

        if (objectManager == null)
            Debug.LogError("[PlacementController] ObjectManager missing");

        if (placementCommunicator == null)
            Debug.LogError("[PlacementController] ObjectPlacementCommunicator missing");
    }

    private void DisablePhysics(GameObject obj)
    {
        foreach (var rb in obj.GetComponentsInChildren<Rigidbody>())
            Destroy(rb);

        foreach (var col in obj.GetComponentsInChildren<Collider>())
            Destroy(col);
    }

    private bool IsNearTree(Terrain terrain, GameObject preview, float minDistance)
    {
        var rend = preview.GetComponentInChildren<Renderer>();
        if (rend == null)
            return false;

        Bounds previewBounds = rend.bounds;
        previewBounds.Expand(minDistance * 2f);

        foreach (var tree in terrain.terrainData.treeInstances)
        {
            Vector3 worldPos =
                Vector3.Scale(tree.position, terrain.terrainData.size)
                + terrain.transform.position;

            if (previewBounds.Contains(new Vector3(worldPos.x, previewBounds.center.y, worldPos.z)))
                return true;
        }

        return false;
    }

    private bool IsPlacementBlocked(GameObject preview, Terrain terrain, float treeMinDistance)
    {
        foreach (var renderer in preview.GetComponentsInChildren<Renderer>())
        {
            var bounds = renderer.bounds;

            var hits = Physics.OverlapBox(
                bounds.center,
                bounds.extents,
                preview.transform.rotation,
                ~0,
                QueryTriggerInteraction.Ignore
            );

            foreach (var hit in hits)
            {
                if (hit.gameObject == preview)
                    continue;
                if (hit is TerrainCollider)
                    continue;

                return true;
            }
        }

        return IsNearTree(terrain, preview, treeMinDistance);
    }

    private void OnDestroy()
    {
        if (previewInstance != null)
            Destroy(previewInstance);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, placementRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, playerMaskRadius);
    }

    // =======================
    // Update loop
    // =======================

    // =======================
    // Helpers
    // =======================
    private void SetPreviewAlpha(GameObject obj, float alpha)
    {
        foreach (var r in obj.GetComponentsInChildren<Renderer>())
        {
            foreach (var m in r.materials)
            {
                var c = m.color;
                c.a = alpha;
                m.color = c;
            }
        }
    }

    private void SetPreviewColor(GameObject obj, Color baseColor, float alpha)
    {
        foreach (var r in obj.GetComponentsInChildren<Renderer>())
        {
            foreach (var m in r.materials)
            {
                var c = baseColor;
                c.a = alpha;
                m.color = c;
            }
        }
    }

    private async Task TryPlaceAtPreviewAsync()
    {
        if (isPlacing)
            return;

        if (previewInstance == null || !previewValid)
        {
            Debug.LogWarning("[PlacementController] Invalid placement attempt");
            return;
        }

        if (placementCommunicator == null || objectManager == null)
        {
            Debug.LogError("[PlacementController] Missing dependencies for placement");
            return;
        }

        isPlacing = true;

        // Snapshot the preview transform now (so user moving mouse doesn't change it mid-request)
        var pos = previewInstance.transform.position;

        // Your data object (keeping your intent)
        var transformData =
            TransformationDC.FromVectors(
                position: pos,
                serverTime: Time.time // client time is fine for placement
            );

        // Local optimistic placement id
        var localId = Guid.NewGuid();

        try
        {
            // Local optimistic placement
            objectManager.PlaceObject(localId, objectTypeToPlace, transformData);

            // Notify server
            await placementCommunicator.SendPlacement(objectTypeToPlace, transformData);

            // Hide preview after success
            previewInstance.SetActive(false);
            previewValid = false;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);

            // Optional rollback so you don't keep ghost objects if server fails
            // If you have a remove method, use it. Otherwise remove this block.
            try
            {
                objectManager.RemoveObject(localId);
            }
            catch
            {
                // ignore rollback errors
            }
        }
        finally
        {
            isPlacing = false;
        }
    }

    private void Update()
    {
        // 🔒 DO NOT process input when unfocused
        if (!Application.isFocused)
            return;

        Vector2? inputPos = null;

        if (Mouse.current != null)
            inputPos = Mouse.current.position.ReadValue();
        else if (Touchscreen.current != null)
            inputPos = Touchscreen.current.primaryTouch.position.ReadValue();

        if (!inputPos.HasValue)
            return;

        UpdatePreview(inputPos.Value);

        if ((Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
            (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame))
        {
            _ = TryPlaceAtPreviewAsync();
        }
    }

    // =======================
    // Preview logic
    // =======================

    private void UpdatePreview(Vector2 screenPosition)
    {
        var cam = cameraManager?.GetCamera();
        if (cam == null)
            return;

        Ray ray = cam.ScreenPointToRay(screenPosition);

        if (!Physics.Raycast(ray, out RaycastHit hit))
        {
            if (previewInstance != null)
                previewInstance.SetActive(false);

            previewValid = false;
            return;
        }

        if (previewInstance == null)
        {
            var prefab = objectManager?.GetPrefab(objectTypeToPlace);
            if (prefab == null)
                return;

            previewInstance = Instantiate(prefab);
            DisablePhysics(previewInstance);
            SetPreviewAlpha(previewInstance, 0.5f);
        }

        previewInstance.transform.position = hit.point;
        previewInstance.SetActive(true);

        if (hit.collider is not TerrainCollider terrainCollider)
        {
            previewValid = false;
            SetPreviewColor(previewInstance, Color.red, 0.5f);
            return;
        }

        var terrain = terrainCollider.GetComponent<Terrain>();
        if (terrain == null)
        {
            previewValid = false;
            SetPreviewColor(previewInstance, Color.red, 0.5f);
            return;
        }

        float distance = Vector3.Distance(transform.position, hit.point);
        bool tooFar = distance > placementRadius;
        bool tooClose = distance < playerMaskRadius;
        bool blocked = IsPlacementBlocked(previewInstance, terrain, 2f);

        previewValid = !(tooFar || tooClose || blocked);

        SetPreviewColor(
            previewInstance,
            previewValid ? Color.green : Color.red,
            0.5f
        );
    }

    // =======================
    // Placement
    // =======================
}