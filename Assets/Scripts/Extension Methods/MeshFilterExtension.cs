using UnityEngine;

public static class MeshFilterExtension
{
    /// <summary>
    /// Saves the current scale as a new mesh so the deformed object can be used with
    /// a uniform scale (avoid common problems caused by un-uniform scaling)
    /// </summary>
    public static void BakeCurrentScale(this MeshFilter filter)
    {
        // Create a copy of the mesh to avoid modifying the original asset
        Mesh originalMesh = filter.sharedMesh;
        Mesh clonedMesh = GameObject.Instantiate(originalMesh);

        Vector3 currentScale = filter.transform.localScale;

        // No need to bake if scale is already uniform/one
        if (currentScale == Vector3.one) return;

        Vector3[] vertices = clonedMesh.vertices;

        // Loop through vertices and apply the transform scale permanently
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = Vector3.Scale(vertices[i], currentScale);
        }

        clonedMesh.vertices = vertices;

        // Recalculate essential mesh data for lighting and physics
        clonedMesh.RecalculateBounds();
        clonedMesh.RecalculateNormals();

        // Apply the new mesh
        filter.mesh = clonedMesh;

        // CRITICAL: Reset the transform scale to (1,1,1)
        // The visual shape remains because vertices are moved
        filter.transform.localScale = Vector3.one;
    }
}