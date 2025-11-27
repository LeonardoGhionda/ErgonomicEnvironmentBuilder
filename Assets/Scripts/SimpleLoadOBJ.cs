using Dummiesman;
using System.IO;
using UnityEngine;

public class SimpleLoadOBJ : MonoBehaviour
{
    private void Start()
    {
        LoadObj(Path.Combine(Application.persistentDataPath, "Ready Models\\Textured Cube\\Untitled.obj"));
    }

    public void LoadObj(string objFilePath)
    {
        if (!File.Exists(objFilePath))
        {
            Debug.LogError("OBJ file not found: " + objFilePath);
            return; 
        }

        // Load the OBJ into a GameObject
        GameObject loadedObj = new OBJLoader().Load(objFilePath);

        // Optional: set position/rotation/scale
        loadedObj.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        loadedObj.transform.localScale = Vector3.one;

        Debug.Log("OBJ loaded successfully!");
    }
}