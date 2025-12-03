using Dummiesman;
using SimpleFileBrowser;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

public class ImportLayout : MonoBehaviour
{

    [SerializeField] private Canvas builder_Ui;

    // Warning: paths returned by FileBrowser dialogs do not contain a trailing '\' character
    // Warning: FileBrowser can only show 1 dialog at a time

    private void OnEnable()
    {
        //due to a bug .meta files are also shown in the browser, so we delete them here
        CleanMetaFiles();
        //Show a select file dialog using coroutine approach
        StartCoroutine(ShowLoadDialogCoroutine());

        //Shows only .room files 
        FileBrowser.SetFilters(false,
            new FileBrowser.Filter("Room", ".room"));
        FileBrowser.SetDefaultFilter(".room");

        FileBrowser.ClearQuickLinks();
        FileBrowser.AddQuickLink("Rooms", RoomDataExporter.roomsFolderPath, null);
        FileBrowser.SetDefaultFilter("Room");
    }

    private void CleanMetaFiles()
    {
        Directory.EnumerateFiles(RoomDataExporter.roomsFolderPath, "*.meta", SearchOption.AllDirectories)
            .ToList()
            .ForEach(File.Delete);
    }

    IEnumerator ShowLoadDialogCoroutine()
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
            OnFilesSelected(FileBrowser.Result); // FileBrowser.Result is null, if FileBrowser.Success is false
        }
        else
        {
            UiManager.Instance.GoToPreviousScreen();
        }
    }

    void OnFilesSelected(string[] filePaths)
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

    private void OnDisable()
    {
        StopCoroutine(ShowLoadDialogCoroutine());
    }
}