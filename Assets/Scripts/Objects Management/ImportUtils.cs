using SimpleFileBrowser;
using System.IO;
using UnityEngine;

public static class ImportUtils
{
    public static string ModelsPath => Path.Combine(Application.persistentDataPath, "Ready Models");

    public static void ImportObject(System.Action onComplete)
    {
        FileBrowser.SetFilters(false, new FileBrowser.Filter("Models", ".obj", ".stp"));

        _ = FileBrowser.ShowLoadDialog((paths) =>
        {
            ProcessImport(paths[0]);
            onComplete?.Invoke();
        },
        null, FileBrowser.PickMode.Files, false, "C:\\Users", null, "Import Model", "Import");
    }

    private static void ProcessImport(string filePath)
    {
        string ext = Path.GetExtension(filePath).ToLower();
        string name = Path.GetFileNameWithoutExtension(filePath);
        string destFolder = Path.Combine(ModelsPath, name);

        _ = Directory.CreateDirectory(destFolder);

        // STP -> BJ Conversion
        if (ext == ".stp")
        {
            if (!StepToObjWrapper.Convert(filePath, 0.001f)) return;
            filePath = Path.ChangeExtension(filePath, ".obj");
        }

        // .obj copy in Models folder
        string destFile = Path.Combine(destFolder, Path.GetFileName(filePath));
        File.Copy(filePath, destFile, true);

        // If present .mtl and textures copy
        string mtlPath = Path.ChangeExtension(filePath, ".mtl");
        ProcessMtlAndTextures(mtlPath, destFolder);
    }

    /// <summary>
    /// if the .mtl file exists, it copies it and all the textures it references to the destination folder
    /// </summary>
    /// <param name="mtlPath"></param>
    /// <param name="destFolder"></param>
    private static void ProcessMtlAndTextures(string mtlPath, string destFolder)
    {
        if (!File.Exists(mtlPath)) return;

        string sourceDir = Path.GetDirectoryName(mtlPath);
        string[] mtlLines = File.ReadAllLines(mtlPath);

        foreach (string line in mtlLines)
        {
            string trimmedLine = line.Trim();

            // Find texture maps
            if (trimmedLine.StartsWith("map_") || trimmedLine.StartsWith("bump ") || trimmedLine.StartsWith("disp ") || trimmedLine.StartsWith("decal "))
            {
                int firstSpaceIndex = trimmedLine.IndexOf(' ');

                if (firstSpaceIndex > 0)
                {
                    // Extract string after the command to allow spaces
                    string textureFileName = trimmedLine.Substring(firstSpaceIndex + 1).Trim();

                    // Strip quotes if they exist
                    textureFileName = textureFileName.Trim('\"', '\'');

                    string sourceTexturePath = Path.Combine(sourceDir, textureFileName);

                    if (File.Exists(sourceTexturePath))
                    {
                        string destTexturePath = Path.Combine(destFolder, Path.GetFileName(textureFileName));
                        File.Copy(sourceTexturePath, destTexturePath, true);
                    }
                }
            }
        }

        // Copy MTL file
        File.Copy(mtlPath, Path.Combine(destFolder, Path.GetFileName(mtlPath)), true);
    }
}