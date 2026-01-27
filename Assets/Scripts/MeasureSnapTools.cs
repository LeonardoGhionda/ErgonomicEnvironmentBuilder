using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MeasureSnapTools
{
    // Snap requirements
    readonly private float minDistanceToSnap = 0.5f; 
    readonly private float minAngleToSnap = 20.0f;

    // Runtime state
    private List<Collider> _snapIgnore = new List<Collider>();

    private readonly Vector3[] _localDirections = new Vector3[]
    {
        Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back
    };

    /// <summary>
    /// Check and perform snap on the selected Transform if possible.
    /// </summary>
    /// <param name="selected">transform of the target object</param>
    /// <returns>true if snap was performed</returns>
    public bool TrySnap(Transform selected)
    {
        return Snap(selected) != null;
    }

    public void SnapAndFollow(Transform selected)
    {
        BoxCollider snapBox = Snap(selected);
        if(snapBox != null)
            selected.AddComponent<SnapFollow>()?.SetTarget(snapBox.transform);
    }

    private BoxCollider Snap(Transform selected)
    {
        // Snap implemented only for BoxColliders
        if (!selected.TryGetComponent<BoxCollider>(out var selectedBC))
        {
            Debug.LogWarning("SnapTool: L'oggetto selezionato non ha un BoxCollider.");
            return null;
        }

        // Cleanup ignore list if object moved away
        if (_snapIgnore.Count > 0)
        {
            // Get obj bounds and expand a bit
            Bounds selectedBoundsExpanded = selectedBC.bounds;
            selectedBoundsExpanded.Expand(minDistanceToSnap * 1.1f);

            // Remove colliders no longer intersecting
            _snapIgnore.RemoveAll(ignoredCollider =>
            {
                if (ignoredCollider == null) return true; 
                return !selectedBoundsExpanded.Intersects(ignoredCollider.bounds);
            });
        }

        //Raycasting from each face of the BoxCollider
        //--------------------------------------------

        RaycastHit bestHit = new RaycastHit();
        float closestDistance = Mathf.Infinity;
        bool hitFound = false;
        Vector3 bestLocalDirection = Vector3.zero;

        Vector3 worldCenter = selected.TransformPoint(selectedBC.center);

        foreach (Vector3 localDir in _localDirections)
        {
            // Local to world conversion
            Vector3 worldDir = selected.TransformDirection(localDir);

            // Ray setup
            Ray ray = new Ray(worldCenter, worldDir);

            // Raycast
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, ~LayerMask.GetMask("Gizmo")))
            {
                // Itself, obj in ignore list and gizmo are ignored
                if (hit.collider.gameObject == selected.gameObject) continue;
                if (_snapIgnore.Contains(hit.collider)) continue;

                // Distance from the edge of our BoxCollider to the hit point
                float distFromCenterToEdge = GetDistanceToEdge(selectedBC, localDir);
                float gapDistance = hit.distance - distFromCenterToEdge;

                // Check snap conditions
                if (gapDistance < minDistanceToSnap && gapDistance < closestDistance)
                {
                    // Angle check
                    float angle = Vector3.Angle(worldDir, -hit.normal);
                    if (angle < minAngleToSnap)
                    {
                        closestDistance = gapDistance;
                        bestHit = hit;
                        bestLocalDirection = localDir;
                        hitFound = true;
                    }
                }
            }
        }

        // Snap execution
        if (hitFound)
        {
            ExecuteSnap(selected, selectedBC, bestHit, bestLocalDirection);
            return (BoxCollider) bestHit.collider;
        }
        return null;
    }

    private void ExecuteSnap(Transform selected, BoxCollider bc, RaycastHit hit, Vector3 localSnapDirection)
    {
        // Add new collider to ignore list
        _snapIgnore.Add(hit.collider);


        // Compute new rotation and position
        //----------------------------------

        // Rotation
        Vector3 currentWorldDir = selected.TransformDirection(localSnapDirection);
        Quaternion rotationAlignment = Quaternion.FromToRotation(currentWorldDir, -hit.normal);
        Quaternion finalRotation = rotationAlignment * selected.rotation;

        //position
        float distCenterToEdge = GetDistanceToEdge(bc, localSnapDirection);
        Vector3 targetWorldCenter = hit.point + (hit.normal * distCenterToEdge);
        Vector3 pivotOffsetLocal = Vector3.Scale(bc.center, selected.lossyScale);
        Vector3 pivotOffsetRotated = finalRotation * pivotOffsetLocal; 
        Vector3 finalPosition = targetWorldCenter - (finalRotation * pivotOffsetLocal); 
        finalPosition = targetWorldCenter - (finalRotation * Vector3.Scale(bc.center, selected.lossyScale));

        // Apply
        selected.SetPositionAndRotation(finalPosition, finalRotation);

    }

    /// <summary>
    /// Distance from the center of the BoxCollider to its edge in the given local direction.
    /// </summary>
    /// <param name="bc"></param>
    /// <param name="localDir"></param>
    /// <returns></returns>
    private float GetDistanceToEdge(BoxCollider bc, Vector3 localDir)
    {
        Vector3 scaledSize = Vector3.Scale(bc.size, bc.transform.lossyScale);

        float radius = (Mathf.Abs(localDir.x * scaledSize.x) +
                        Mathf.Abs(localDir.y * scaledSize.y) +
                        Mathf.Abs(localDir.z * scaledSize.z)) * 0.5f;

        return radius;
    }

    public void Clear()
    {
        _snapIgnore.Clear();
    }
}