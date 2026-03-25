using Dummiesman;
using Unity.VisualScripting;
using UnityEngine;

public class HM_SpawnModel : HM_Base
{
    public string modelFullPath;
    VRSelectionManager _selectionManager;
    HandMenuManager _handMenu;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _selectionManager = Managers.Get<VRSelectionManager>();
        _handMenu = Managers.Get<HandMenuManager>();
    }

    public override void OnClick()
    {
        base.OnClick();
        if (string.IsNullOrEmpty(modelFullPath))
        {
            Debug.LogError("Model card is missing path to the model");
            return;
        }

        OBJLoader loader = new();
        GameObject obj = loader.FindMTLAndLoad(modelFullPath);
        obj.name = $"[P] {obj.name}";
        obj.transform.SetParent(GameObject.Find("Objects Container").transform);

        obj.AddComponent<InteractableParent>().Path = modelFullPath;

        // Set up children and add colliders to measure the actual size of the object
        foreach (Transform child in obj.transform)
        {
            RoomManagementTools.SetUpVrObject(child, _selectionManager, false, true);
            _ = child.AddComponent<InteractableObject>();
        }

        BoxCollider[] colliders = obj.GetComponentsInChildren<BoxCollider>();
        Bounds bounds = new (obj.transform.position, Vector3.zero);
        bool hasBounds = false;

        if (colliders.Length > 0)
        {
            bounds = colliders[0].bounds;
            foreach (BoxCollider col in colliders)
            {
                bounds.Encapsulate(col.bounds);
            }
            hasBounds = true;
        }

        Camera cam = DependencyProvider.VRPlayer.GetComponentInChildren<Camera>();

        // Use bounds magnitude to guarantee clearance
        float objectRadius = hasBounds ? bounds.extents.magnitude : 0.5f;
        float playerBuffer = 0.5f;
        float minDistance = objectRadius + playerBuffer;
        float targetDistance = 4f;
        float maxDistance = targetDistance;

        int wallLayer = LayerMask.GetMask("Wall Layer");

        // Detect walls and restrict the maximum distance to avoid clipping outside
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit wallHit, targetDistance + objectRadius, wallLayer))
        {
            maxDistance = wallHit.distance - objectRadius;
        }

        float spawnDistance = targetDistance;

        spawnDistance = Mathf.Clamp(spawnDistance, minDistance, maxDistance);

        Vector3 newPos = cam.transform.position + cam.transform.forward * spawnDistance;
        obj.transform.position = newPos;

        // Snap exactly to the floor at Y=0 using the bounds
        if (hasBounds)
        {
            // Recalculate bounds at the new position
            bounds = colliders[0].bounds;
            foreach (BoxCollider col in colliders)
            {
                bounds.Encapsulate(col.bounds);
            }

            // Calculate the offset from the pivot to the lowest point
            float ypos = bounds.size.y / 2f - bounds.center.y;

            // Set Y position so the lowest point rests exactly at 0
            obj.transform.position = new Vector3(obj.transform.position.x, ypos, obj.transform.position.z);
        }

        // Close HM on obj spawn
        _handMenu.Show(false);
    }
}
