using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlacementController : MonoBehaviour
{
    [Header("Placement Settings")]
    [SerializeField] private string objectTypeToPlace = "Tree";
    [SerializeField] private LayerMask placementMask;

    [Tooltip("Maximum distance from player at which objects can be placed.")]
    [SerializeField] private float placementRadius = 5f;

    [SerializeField] private float playerMaskRadius = 0f;

    private GameObject previewInstance;
    private bool previewValid = false; // ✅ track if preview is valid
    private int nextObjectId = 1000;

    void Update()
    {
        Vector2? inputPos = null;

        if (Mouse.current != null)
            inputPos = Mouse.current.position.ReadValue();
        else if (Touchscreen.current != null)
            inputPos = Touchscreen.current.primaryTouch.position.ReadValue();

        if (inputPos.HasValue)
        {
            UpdatePreview(inputPos.Value);

            if ((Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame))
            {
                TryPlaceAtPreview();
            }
        }
    }

    // ---------------- PREVIEW ----------------
    private void UpdatePreview(Vector2 screenPosition)
    {
        Ray ray = CameraManager.Instance.GetCamera().ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, placementMask))
        {
            // Only allow terrain hits
            if (!(hit.collider is TerrainCollider))
            {
                if (previewInstance != null) previewInstance.SetActive(false);
                previewValid = false;
                return;
            }

            Terrain terrain = hit.collider.GetComponent<Terrain>();
            if (terrain == null)
            {
                if (previewInstance != null) previewInstance.SetActive(false);
                previewValid = false;
                return;
            }

            // Create preview if it doesn’t exist
            if (previewInstance == null)
            {
                GameObject prefab = ObjectManager.Instance.GetPrefab(objectTypeToPlace);
                if (prefab != null)
                {
                    // 👇 parent preview to this controller
                    previewInstance = Instantiate(prefab);
                    MakeTransparent(previewInstance, 0.5f);
                    DisablePhysics(previewInstance);
                }
            }

            if (previewInstance != null)
            {
                previewInstance.transform.position = hit.point;
                previewInstance.SetActive(true);

                float distance = Vector3.Distance(transform.position, hit.point);
                bool tooFar = distance > placementRadius;
                bool tooCloseToPlayer = distance < playerMaskRadius;
                bool blocked = IsPlacementBlocked(previewInstance, terrain, 2f);

                previewValid = !(tooFar || tooCloseToPlayer || blocked);

                // Set color
                SetPreviewColor(previewInstance, previewValid
                    ? new Color(0f, 1f, 0f, 0.5f) // ✅ green
                    : new Color(1f, 0f, 0f, 0.5f) // ❌ red
                );
            }
        }
        else if (previewInstance != null)
        {
            previewInstance.SetActive(false);
            previewValid = false;
        }
    }

    // ---------------- FINAL PLACEMENT ----------------
    private async void TryPlaceAtPreview()
    {
        if (previewInstance != null && previewValid)
        {
            var transformData = TransformationDC.FromPosition(previewInstance.transform.position);
            string typeKey = objectTypeToPlace;

            // 1. Spawn immediately (local feedback)
            ObjectManager.Instance.PlaceObject(Guid.NewGuid(), typeKey, transformData);

            // 2. Notify server (so others see it too)
            await ProjectDawnApi.Instance.SendObjectPlacement(typeKey, transformData);

            // Reset preview
            previewInstance.SetActive(false);
            previewValid = false;
        }
        else
        {
            Debug.LogWarning("[PlacementController] Tried to place, but preview was invalid!");
        }
    }

    // ---------------- HELPERS ----------------
    private bool IsPlacementBlocked(GameObject preview, Terrain terrain, float treeMinDistance = 2f)
    {
        if (preview != null)
        {
            foreach (var renderer in preview.GetComponentsInChildren<Renderer>())
            {
                Bounds bounds = renderer.bounds;
                Collider[] hits = Physics.OverlapBox(
                    bounds.center,
                    bounds.extents,
                    preview.transform.rotation,
                    ~0,
                    QueryTriggerInteraction.Ignore
                );

                foreach (var hit in hits)
                {
                    if (hit.gameObject == preview)
                        continue; // ignore itself

                    if (hit is TerrainCollider)
                        continue; // ✅ ignore terrain

                    return true; // found a blocking object
                }
            }
        }

        // ✅ Bounds-based tree check
        if (terrain != null && IsNearTree(terrain, preview, treeMinDistance))
            return true;

        return false;
    }

    private bool IsNearTree(Terrain terrain, GameObject preview, float minDistance = 2f)
    {
        if (terrain == null || preview == null) return false;

        Renderer rend = preview.GetComponentInChildren<Renderer>();
        if (rend == null) return false;

        Bounds previewBounds = rend.bounds;
        TreeInstance[] trees = terrain.terrainData.treeInstances;

        for (int i = 0; i < trees.Length; i++)
        {
            Vector3 treeWorldPos = Vector3.Scale(trees[i].position, terrain.terrainData.size) + terrain.transform.position;

            Bounds checkBounds = previewBounds;
            checkBounds.Expand(minDistance * 2f);

            if (checkBounds.Contains(new Vector3(treeWorldPos.x, previewBounds.center.y, treeWorldPos.z)))
            {
                return true; // tree inside preview area
            }
        }

        return false;
    }

    private void SetPreviewColor(GameObject obj, Color color)
    {
        if (obj == null) return;

        foreach (var renderer in obj.GetComponentsInChildren<Renderer>())
        {
            foreach (var mat in renderer.materials)
            {
                Color c = mat.color;
                c.r = color.r;
                c.g = color.g;
                c.b = color.b;
                c.a = color.a;
                mat.color = c;
            }
        }
    }

    private void MakeTransparent(GameObject obj, float alpha = 0.5f)
    {
        if (obj == null) return;

        foreach (var renderer in obj.GetComponentsInChildren<Renderer>())
        {
            foreach (var mat in renderer.materials)
            {
                Color color = mat.color;
                color.a = alpha;
                mat.color = color;
            }
        }
    }

    private void DisablePhysics(GameObject obj)
    {
        if (obj == null) return;

        foreach (var rb in obj.GetComponentsInChildren<Rigidbody>())
            Destroy(rb);

        foreach (var col in obj.GetComponentsInChildren<Collider>())
            Destroy(col);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, placementRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, playerMaskRadius);
    }

    // ---------------- CLEANUP ----------------
    private void OnDestroy()
    {
        if (previewInstance != null)
        {
            Destroy(previewInstance);
            previewInstance = null;
        }
    }
}
