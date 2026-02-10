using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public static class OBJExporter
{
    /// <summary>
    /// Export the iParent as a new OBJ at InteractableParent.Path
    /// Consolidates all children into a single file, preserving offsets relative to Parent.
    /// </summary>
    /// <param name="parent">The root object containing children to export</param>
    public static void Export(InteractableParent parent)
    {
        string objPath = parent.Path;
        string fileName = Path.GetFileNameWithoutExtension(objPath);

        string baseName = fileName.RemoveModID();

        string mtlFileName = baseName + ".mtl";

        StringBuilder objSb = new StringBuilder();
        CultureInfo culture = CultureInfo.InvariantCulture;

        objSb.AppendLine("# CREATED BY ERGONOMIC ENVIRONMENT BUILDER OBJ EXPORTER");
        objSb.AppendLine($"# PARENT NAME: {parent.name}");

        // Link to the existing original material library
        objSb.AppendLine($"mtllib {mtlFileName}");
        objSb.AppendLine();

        int globalVertexOffset = 0;

        foreach (Transform child in parent.transform)
        {
            if (child.TryGetComponent(out InteractableObject _))
            {
                MeshFilter mf = child.GetComponent<MeshFilter>();
                MeshRenderer mr = child.GetComponent<MeshRenderer>();

                if (mf && mr)
                {
                    objSb.AppendLine($"o {child.name}");

                    Material[] currentMaterials = mr.sharedMaterials;
                    Mesh mesh = mf.mesh;

                    // Use Zero position and Identity rotation to keep mesh local to its pivot
                    Matrix4x4 localToParentMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, child.localScale);

                    WriteMesh(objSb, child.name.ClearUnityString(), mesh, currentMaterials, localToParentMatrix, ref globalVertexOffset);
                }
            }
        }

        objSb.AppendLine("# END OF FILE");

        File.WriteAllText(objPath, objSb.ToString());
    }

    private static void WriteMesh(StringBuilder sb, string meshName, Mesh mesh, Material[] mats, Matrix4x4 matrix, ref int globalOffset)
    {
        CultureInfo culture = CultureInfo.InvariantCulture;

        // Calculate Normal Matrix (Inverse Transpose)
        Matrix4x4 normalMatrix = matrix.inverse.transpose;

        // Vertices
        foreach (Vector3 v in mesh.vertices)
        {
            Vector3 relativePos = matrix.MultiplyPoint3x4(v);
            sb.AppendLine(string.Format(culture, "v {0:F10} {1:F10} {2:F10}", relativePos.x, relativePos.y, relativePos.z));
        }

        // UVs
        foreach (Vector2 uv in mesh.uv)
        {
            sb.AppendLine(string.Format(culture, "vt {0:F10} {1:F10}", uv.x, uv.y));
        }

        // Normals
        foreach (Vector3 n in mesh.normals)
        {
            Vector3 relativeDir = normalMatrix.MultiplyVector(n).normalized;
            sb.AppendLine(string.Format(culture, "vn {0:F10} {1:F10} {2:F10}", relativeDir.x, relativeDir.y, relativeDir.z));
        }

        // Faces
        for (int s = 0; s < mesh.subMeshCount; s++)
        {
            sb.AppendLine($"g {meshName}");
            string rawMatName = (mats != null && s < mats.Length && mats[s] != null) ? mats[s].name : "Default_Material";
            sb.AppendLine($"usemtl {rawMatName.ClearUnityString()}");

            int[] tris = mesh.GetTriangles(s);
            for (int i = 0; i < tris.Length; i += 3)
            {
                int t1 = tris[i] + 1 + globalOffset;
                int t2 = tris[i + 1] + 1 + globalOffset;
                int t3 = tris[i + 2] + 1 + globalOffset;

                sb.AppendLine(string.Format(culture, "f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}", t1, t2, t3));
            }
        }

        globalOffset += mesh.vertexCount;
    }
}