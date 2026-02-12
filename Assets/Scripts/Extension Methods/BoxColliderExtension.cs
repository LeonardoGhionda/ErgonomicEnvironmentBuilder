using UnityEngine;

public static class BoxColliderExtension
{
    public static Vector3 ContactPointNormal(this BoxCollider box, Vector3 contactPoint)
    {
        Transform transform = box.transform;

        // Convert contact point to Local Space
        Vector3 localPoint = transform.InverseTransformPoint(contactPoint);

        // Adjust for the collider's center offset
        localPoint -= box.center;

        // Get the half-sizes (extents) of the box
        Vector3 halfSize = box.size * 0.5f;

        // Calculate how close the point is to each face (distance to edge)
        float distToX = Mathf.Abs(Mathf.Abs(localPoint.x) - halfSize.x);
        float distToY = Mathf.Abs(Mathf.Abs(localPoint.y) - halfSize.y);
        float distToZ = Mathf.Abs(Mathf.Abs(localPoint.z) - halfSize.z);

        // Find the smallest distance (that's our face)
        if (distToX < distToY && distToX < distToZ)
        {
            // Valid for Right (+X) or Left (-X)
            return transform.TransformDirection(localPoint.x > 0 ? Vector3.right : Vector3.left);
        }
        else if (distToY < distToX && distToY < distToZ)
        {
            // Valid for Up (+Y) or Down (-Y)
            return transform.TransformDirection(localPoint.y > 0 ? Vector3.up : Vector3.down);
        }
        else
        {
            // Valid for Forward (+Z) or Back (-Z)
            return transform.TransformDirection(localPoint.z > 0 ? Vector3.forward : Vector3.back);
        }
    }
}
