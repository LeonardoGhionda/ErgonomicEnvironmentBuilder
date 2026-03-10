using System.Collections.Generic;
using UnityEngine;

public static class PreviewGenerator
{
    private const int PreviewResolution = 200;
    private const string layerName = "Preview";

    /// <summary>
    /// Generates a preview sprite from a prefab.
    /// </summary>
    /// <param name="prefab">The prefab to generate a preview from</param>
    /// <returns>Sprite generated from prefab</returns>
    public static Texture2D GeneratePrefabPreview(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError($"GeneratePrefabPreview: prefab is null");
            return null;
        }

        // Create temporary camera
        GameObject gameObject = new("PreviewCamera");
        GameObject camGO = gameObject;
        Camera cam = camGO.AddComponent<Camera>();
        Color bgColor = Color.blue;
        bgColor.a = 0.2f;
        cam.backgroundColor = bgColor;
        cam.clearFlags = CameraClearFlags.Color;
        cam.orthographic = false;
        cam.cullingMask = LayerMask.GetMask(layerName); // optional layer to isolate

        // Create temporary RenderTexture
        RenderTexture rt = new(PreviewResolution, PreviewResolution, 16);
        cam.targetTexture = rt;

        // Instantiate prefab
        GameObject tempObj = GameObject.Instantiate(prefab);
        tempObj.layer = LayerMask.NameToLayer(layerName);

        //instantiate childrens
        _ = new
        //instantiate childrens
        List<GameObject>();
        foreach (Transform t in prefab.transform)
        {
            GameObject go = t.gameObject;
            go.layer = LayerMask.NameToLayer(layerName);
        }

        // Center camera on prefab
        Bounds bounds = GetBounds(tempObj);
        cam.transform.position = bounds.center + new Vector3(0, bounds.extents.magnitude, bounds.extents.magnitude * 2);
        cam.farClipPlane = bounds.extents.magnitude * 3;
        cam.transform.LookAt(bounds.center);

        // Render
        cam.Render();

        // Read pixels
        RenderTexture.active = rt;
        Texture2D tex = new(rt.width, rt.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;



        // Cleanup
        GameObject.DestroyImmediate(tempObj);
        GameObject.DestroyImmediate(camGO);
        GameObject.DestroyImmediate(prefab);
        rt.Release();

        return tex;
    }

    /// <summary>
    /// Calculates bounds for an object including all children renderers
    /// </summary>
    private static Bounds GetBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
            bounds.Encapsulate(r.bounds);
        return bounds;
    }
}

