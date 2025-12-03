using Dummiesman;
using SimpleFileBrowser;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

public class ImportModel : MonoBehaviour
{

    [SerializeField] private GetModelUi getModelUi;

    // Warning: paths returned by FileBrowser dialogs do not contain a trailing '\' character
    // Warning: FileBrowser can only show 1 dialog at a time

    private void OnEnable()
    {
        //Show a select file dialog using coroutine approach
        StartCoroutine(ShowLoadDialogCoroutine());

        //Shows only files 
        FileBrowser.SetFilters(false,
            new FileBrowser.Filter("All", ".obj", ".stp"),
            new FileBrowser.Filter("Wavefront", ".obj"),
            new FileBrowser.Filter("Step", ".stp"));

        FileBrowser.ClearQuickLinks();
        FileBrowser.AddQuickLink("Users", "C:\\Users", null);
        FileBrowser.AddQuickLink("Documents", Application.persistentDataPath, null);
        FileBrowser.AddQuickLink("Desktop", System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop), null);
        FileBrowser.AddQuickLink("Downloads", System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "\\Downloads", null);
        FileBrowser.AddQuickLink("Models", Path.Combine(Application.persistentDataPath, GetModelUi.ModelsFolder), null);

        FileBrowser.SetDefaultFilter("All");
    }

    IEnumerator ShowLoadDialogCoroutine()
    {
        // Show a load file dialog and wait for a response from user
        // Load file/folder: file, Allow multiple selection: true
        // Initial path: default (Documents), Initial filename: empty
        // Title: "Load File", Submit button selectedName: "Load"
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, "C:\\Users", null, "Select model to import", "Load");

        if (FileBrowser.Success)
        {
            OnFilesSelected(FileBrowser.Result); // FileBrowser.Result is null, if FileBrowser.Success is false
        }
        else
        {
            enabled = false;
        }
    }

    void OnFilesSelected(string[] filePaths)
    {

        if(filePaths.Count() == 0)
        {
            enabled = false;
            return;
        }

        // Get the file path of the first selected file
        string filePath = filePaths[0];

        //extract extension 
        string ext = Path.GetExtension(filePath);
        string name = Path.GetFileNameWithoutExtension(filePath);

        string destDir = Path.Combine(Application.persistentDataPath, GetModelUi.ModelsFolder, name);
        Directory.CreateDirectory(destDir);

        //convert to .obj
        if (ext == ".stp")
        {
            if (!StepToObjWrapper.Convert(filePath, 0.001f))
            {
                enabled = false;
                return;
            }
            filePath = Path.ChangeExtension(filePath, "obj");
        }

        //move file into the models folder
        File.Copy(
            filePath, 
            Path.Combine(
                destDir,
                Path.GetFileName(filePath)),
            true
        );

        //if there is a .mtl file, move it too
        string mtlSourcePath = Path.ChangeExtension(filePath, ".mtl");
        if (File.Exists(mtlSourcePath))
        {
            File.Copy(
                mtlSourcePath,
                Path.Combine(
                    destDir,
                    $"{name}.mtl"),
                true
            );
        }

        getModelUi.AddUiElement(destDir);
        enabled = false;
    }

    private void OnDisable()
    {
        //Show a select file dialog using coroutine approach
        StopCoroutine(ShowLoadDialogCoroutine());
    }
}