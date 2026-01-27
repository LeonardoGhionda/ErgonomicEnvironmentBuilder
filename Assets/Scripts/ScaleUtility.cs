using UnityEngine;

public static class ScalerUtility
{
    /// <summary>
    /// Scales an object on its local axes, pivoting around a specific world point 
    /// (e.g., its visual center) instead of its Transform pivot.
    /// </summary>
    /// <param name="target">The object to scale.</param>
    /// <param name="pivotPoint">The world position to keep stationary (e.g. renderer.bounds.center).</param>
    /// <param name="newLocalScale">The target local scale.</param>
    public static void SetScaleAround(Transform target, Vector3 pivotPoint, Vector3 newLocalScale)
    {
        // 1. Save the pivot's position relative to the object BEFORE scaling
        // We use InverseTransformPoint to get the pivot in the object's local space
        Vector3 localPivot = target.InverseTransformPoint(pivotPoint);

        // 2. Apply the new scale
        target.localScale = newLocalScale;

        // 3. Calculate where the pivot SHOULD be in world space after scaling
        Vector3 newWorldPivot = target.TransformPoint(localPivot);

        // 4. Move the object to compensate for the drift
        // We calculate the difference between where the pivot is now vs where it was
        Vector3 positionCorrection = pivotPoint - newWorldPivot;

        target.position += positionCorrection;
    }
}