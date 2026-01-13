using UnityEngine;
using System.IO;

public static class ScreenshotUtility
{
    /// <summary>
    /// Captures the view of a specific camera and saves it as a PNG.
    /// </summary>
    public static void CaptureCamera(Camera targetCamera, int width, int height, string filePath)
    {
        // 1. Create a temporary RenderTexture
        RenderTexture rt = new RenderTexture(width, height, 24);

        // 2. Assign the RT to the camera and render
        targetCamera.targetTexture = rt;
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = rt;

        targetCamera.Render();

        // 3. Create a Texture2D to read the pixels
        Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        image.Apply();

        // 4. Reset camera and active RT
        targetCamera.targetTexture = null;
        RenderTexture.active = currentRT;
        Object.Destroy(rt); // Cleanup RT to avoid memory leaks

        // 5. Save to file
        byte[] bytes = image.EncodeToPNG();
        File.WriteAllBytes(filePath, bytes);

        // Cleanup Texture2D
        Object.Destroy(image);

    }
}