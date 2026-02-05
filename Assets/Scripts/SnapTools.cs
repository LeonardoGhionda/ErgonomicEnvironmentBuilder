using System.Collections.Generic;
using UnityEngine;

public class SnapTools
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

    /// <summary>
    /// Snap selected closest face to target closest face 
    /// </summary>
    /// <returns>false if snap failed</returns>
    public bool SnapToTarget(Transform selected, Transform target, float maxDistance = float.MaxValue)
    {
        if (!selected || !target) return false;

        // 1. Get Colliders
        if (!selected.TryGetComponent<BoxCollider>(out var selBC) ||
            !target.TryGetComponent<BoxCollider>(out var tgtBC))
            return false;

        // 2. Quick Distance Check
        if (Vector3.Distance(selected.position, target.position) > maxDistance) return false;

        // 3. Find Closest Face Pair
        // We compare every face of 'Selected' vs every face of 'Target'
        float minFaceDist = float.MaxValue;
        bool pairFound = false;

        Vector3 bestSelLocalDir = Vector3.zero;     // Which face of Selected?
        Vector3 bestTgtFaceCenter = Vector3.zero;   // Where is the target face?
        Vector3 bestTgtFaceNormal = Vector3.zero;   // What is the target normal?

        // Helper to get world center of a face without allocating new memory
        Vector3 GetFaceCenter(Transform t, BoxCollider b, Vector3 dir)
        {
            Vector3 localFaceCenter = b.center + Vector3.Scale(dir, b.size * 0.5f);
            return t.TransformPoint(localFaceCenter);
        }

        foreach (Vector3 selDir in _localDirections)
        {
            Vector3 selFaceCenter = GetFaceCenter(selected, selBC, selDir);

            foreach (Vector3 tgtDir in _localDirections)
            {
                Vector3 tgtFaceCenter = GetFaceCenter(target, tgtBC, tgtDir);
                float d = Vector3.Distance(selFaceCenter, tgtFaceCenter);

                if (d < minFaceDist)
                {
                    minFaceDist = d;

                    bestSelLocalDir = selDir;
                    bestTgtFaceCenter = tgtFaceCenter;
                    // Target normal in world space
                    bestTgtFaceNormal = target.TransformDirection(tgtDir);

                    pairFound = true;
                }
            }
        }

        if (!pairFound || minFaceDist > maxDistance) return false;

        // 4. Calculate Snap Logic
        // ---------------------

        // A. Rotation: Align selected normal to be opposite of target normal
        // Goal: selWorldNormal == -targetWorldNormal
        Vector3 currentSelWorldNormal = selected.TransformDirection(bestSelLocalDir);
        Quaternion alignRot = Quaternion.FromToRotation(currentSelWorldNormal, -bestTgtFaceNormal);
        Quaternion finalRotation = alignRot * selected.rotation;

        // B. Position: Align face centers
        // We know where the face SHOULD be (bestTgtFaceCenter).
        // We need to back-calculate where the pivot MUST be to satisfy that.

        // 1. Calculate the vector from Pivot to FaceCenter in Local Space
        Vector3 pivotToFaceLocal = selBC.center + Vector3.Scale(bestSelLocalDir, selBC.size * 0.5f);

        // 2. Scale it (LossyScale) and Rotate it (FinalRotation) to get World Offset
        Vector3 pivotToFaceWorld = finalRotation * Vector3.Scale(pivotToFaceLocal, selected.lossyScale);

        // 3. Subtract offset from the target point to find the new pivot position
        Vector3 finalPosition = bestTgtFaceCenter - pivotToFaceWorld;

        // 5. Apply
        selected.SetPositionAndRotation(finalPosition, finalRotation);

        // Add to ignore list to prevent immediate re-snaps/collision fights
        if (!_snapIgnore.Contains(tgtBC)) _snapIgnore.Add(tgtBC);

        return true;
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