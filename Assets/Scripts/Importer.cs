/*
using SimpleFileBrowser;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;


public enum ImporterOf
{
    Rooms,
    Objects,
}

public class Importer : MonoBehaviour
{
    public Importer Init(ImporterOf type)
    {
        switch (type)
        {
            case ImporterOf.Objects:
                SetUpRoomImporter(type);
                break;
            case ImporterOf.Rooms:
                SetUpRoomImporter(type);
                break;
        }
        return this;
    }

    // --- IMPORTER SETUP ---
    private void SetUpRoomImporter(ImporterOf type)
    {
        //due to a bug .meta files are also shown in the browser, so we delete them here
        CleanMetaFiles();
        //Show a select file dialog using coroutine approach
        StartCoroutine(RoomImporterCoroutine());

        //Shows only .room files 
        FileBrowser.SetFilters(false,
            new FileBrowser.Filter("Room", ".room"));
        FileBrowser.SetDefaultFilter(".room");

        FileBrowser.ClearQuickLinks();
        FileBrowser.AddQuickLink("Rooms", RoomDataExporter.roomsFolderPath, null);
        FileBrowser.SetDefaultFilter("Room");
    }

    private void SetUpObjectImporter(ImporterOf type)
    {
        //Show a select file dialog using coroutine approach
        StartCoroutine(ObjectImporterCoroutine());

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
    // ------
    // ------

    private void CleanMetaFiles()
    {
        Directory.EnumerateFiles(RoomDataExporter.roomsFolderPath, "*.meta", SearchOption.AllDirectories)
            .ToList()
            .ForEach(File.Delete);
    }

    // --- COROUTINES ---
    IEnumerator RoomImporterCoroutine()
    {
        yield return FileBrowser.WaitForLoadDialog(
            FileBrowser.PickMode.Files, 
            false, 
            RoomDataExporter.roomsFolderPath,
            null, 
            "Select the room you want to edit", 
            "Load"
            );


        if (FileBrowser.Success)
        {
            OnRoomSelected(FileBrowser.Result); // FileBrowser.Result is null, if FileBrowser.Success is false
        }
        else
        {
            UiManager.Instance.GoToPreviousScreen();
        }
    }

    IEnumerator ObjectImporterCoroutine()
    {
        // Show a load file dialog and wait for a response from user
        // Load file/folder: file, Allow multiple selection: true
        // Initial path: default (Documents), Initial filename: empty
        // Title: "Load File", Submit button selectedName: "Load"
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, "C:\\Users", null, "Select model to import", "Load");

        if (FileBrowser.Success)
        {
            OnObjectSelected(FileBrowser.Result); // FileBrowser.Result is null, if FileBrowser.Success is false
        }
        else
        {
            enabled = false;
        }
    }
    //--------
    //--------

    // --- On File Selected ---
    void OnRoomSelected(string[] filePaths)
    {

        if(filePaths.Count() == 0)
        {
            UiManager.Instance.ChangeScreen(GameObject.Find("Main Menu").GetComponent<Canvas>());
        }

        // Get the file path of the first selected file
        string filePath = filePaths[0];

        //extract name 
        string name = Path.GetFileNameWithoutExtension(filePath);

        RoomDataExporter.CreateRoom(name);

        var um = UiManager.Instance;
        um.RoomName = name;
        um.ChangeScreen(builder_Ui);
    }

    void OnObjectSelected(string[] filePaths)
    {

        if (filePaths.Count() == 0)
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

        CloseImporter(OnRoomSelected())
    }

    // -------
    // -------

    private void CloseImporter(Coroutine c)
    {
        StopCoroutine(c);
    }
}
*/