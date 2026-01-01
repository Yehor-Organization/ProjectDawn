using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlacementController : MonoBehaviour
{
    // 🔗 Dependencies
    private CameraManager cameraManager;

    private ObjectManager objectManager;

    [Header("Placement Settings")]
    [SerializeField] private string objectTypeToPlace = "Tree";

    private ObjectPlacementCommunicator placementCommunicator;
    [SerializeField] private float placementRadius = 5f;
    [SerializeField] private float playerMaskRadius = 0f;
    private GameObject previewInstance;
    private bool previewValid;

    private void Awake()
    {
        var core = Core.Instance;

        cameraManager = core.Managers.CameraManager;
        objectManager = core.Managers.ObjectManager;
        placementCommunicator = core.ApiCommunicators.ObjectPlacement;

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
        if (rend == null) return false;

        Bounds previewBounds = rend.bounds;
        previewBounds.Expand(minDistance * 2f);

        foreach (var tree in terrain.terrainData.treeInstances)
        {
            Vector3 worldPos =
                Vector3.Scale(tree.position, terrain.terrainData.size)
                + terrain.transform.position;

            if (previewBounds.Contains(
                new Vector3(worldPos.x, previewBounds.center.y, worldPos.z)))
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
                if (hit.gameObject == preview) continue;
                if (hit is TerrainCollider) continue;
                return true;
            }
        }

        return IsNearTree(terrain, preview, treeMinDistance);
    }

    // ---------------- HELPERS ----------------
    private void MakeTransparent(GameObject obj, float alpha)
    {
        foreach (var r in obj.GetComponentsInChildren<Renderer>())
            foreach (var m in r.materials)
                m.color = m.color.WithAlpha(alpha);
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

    private void SetPreviewColor(GameObject obj, Color color)
    {
        foreach (var r in obj.GetComponentsInChildren<Renderer>())
            foreach (var m in r.materials)
                m.color = color;
    }

    private async void TryPlaceAtPreview()
    {
        if (previewInstance == null || !previewValid)
        {
            Debug.LogWarning("[PlacementController] Invalid placement attempt");
            return;
        }

        var transformData =
            TransformationDC.FromPosition(previewInstance.transform.position);

        // 1️⃣ Local optimistic placement
        objectManager.PlaceObject(Guid.NewGuid(), objectTypeToPlace, transformData);

        // 2️⃣ Notify server
        await placementCommunicator.SendPlacement(objectTypeToPlace, transformData);

        previewInstance.SetActive(false);
        previewValid = false;
    }

    private void Update()
    {
        Vector2? inputPos = null;

        if (Mouse.current != null)
            inputPos = Mouse.current.position.ReadValue();
        else if (Touchscreen.current != null)
            inputPos = Touchscreen.current.primaryTouch.position.ReadValue();

        if (!inputPos.HasValue) return;

        UpdatePreview(inputPos.Value);

        if ((Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
            (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame))
        {
            TryPlaceAtPreview();
        }
    }

    // ---------------- PREVIEW ----------------

    private void UpdatePreview(Vector2 screenPosition)
    {
        var cam = cameraManager.GetCamera();
        if (cam == null) return;

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
            var prefab = objectManager.GetPrefab(objectTypeToPlace);
            if (prefab == null) return;

            previewInstance = Instantiate(prefab);
            MakeTransparent(previewInstance, 0.5f);
            DisablePhysics(previewInstance);
        }

        previewInstance.transform.position = hit.point;
        previewInstance.SetActive(true);

        var terrain = hit.collider.GetComponent<Terrain>();
        if (terrain == null || hit.collider is not TerrainCollider)
        {
            previewValid = false;
            SetPreviewColor(previewInstance, Color.red.WithAlpha(0.5f));
            return;
        }

        float distance = Vector3.Distance(transform.position, hit.point);
        bool tooFar = distance > placementRadius;
        bool tooClose = distance < playerMaskRadius;
        bool blocked = IsPlacementBlocked(previewInstance, terrain, 2f);

        previewValid = !(tooFar || tooClose || blocked);

        SetPreviewColor(
            previewInstance,
            previewValid ? Color.green.WithAlpha(0.5f) : Color.red.WithAlpha(0.5f)
        );
    }

    // ---------------- FINAL PLACEMENT ----------------
}