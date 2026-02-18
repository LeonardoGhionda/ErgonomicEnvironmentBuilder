using SimpleFileBrowser;
using System.IO;
using UnityEngine;

public static class ImportUtils
{
    public static string ModelsPath => Path.Combine(Application.persistentDataPath, "Ready Models");

    public static void ImportObject(System.Action onComplete)
    {
        FileBrowser.SetFilters(false, new FileBrowser.Filter("Models", ".obj", ".stp"));

        FileBrowser.ShowLoadDialog((paths) =>
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

        Directory.CreateDirectory(destFolder);

        // STP -> BJ Conversion
        if (ext == ".stp")
        {
            if (!StepToObjWrapper.Convert(filePath, 0.001f)) return;
            filePath = Path.ChangeExtension(filePath, ".obj");
        }

        // .obj copy in Models folder
        string destFile = Path.Combine(destFolder, Path.GetFileName(filePath));
        File.Copy(filePath, destFile, true);

        // If present .mtl copy
        string mtlPath = Path.ChangeExtension(filePath, ".mtl");
        if (File.Exists(mtlPath))
        {
            File.Copy(mtlPath, Path.Combine(destFolder, Path.GetFileName(mtlPath)), true);
        }
    }
}