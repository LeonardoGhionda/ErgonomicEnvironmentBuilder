using UnityEngine;

public static class VectorExtensions
{
    public static Vector2 horizontalPlane(this Vector3 v) => new(v.x, v.z);
}
