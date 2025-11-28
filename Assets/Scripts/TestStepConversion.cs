using Dummiesman;
using SimpleFileBrowser;
using System.Collections;
using System.IO;
using UnityEngine;

public class FileBrowserTest : MonoBehaviour
{
    // Warning: paths returned by FileBrowser dialogs do not contain a trailing '\' character
    // Warning: FileBrowser can only show 1 dialog at a time

    void Start()
    {
        // Set filters (optional)
        // It is sufficient to set the filters just once (instead of each time before showing the file browser dialog), 
        // if all the dialogs will be using the same filters
        FileBrowser.SetFilters(false,
            new FileBrowser.Filter("STP", ".stp"));
            //new FileBrowser.Filter("Images", ".jpg", ".png"), 
            //new FileBrowser.Filter("Text Files", ".txt", ".pdf"));

        // Set default filter that is selected when the dialog is shown (optional)
        // Returns true if the default filter is set successfully
        // In this case, set Images filter as the default filter
        FileBrowser.SetDefaultFilter("STP");

        // Set excluded file extensions (optional) (by default, .lnk and .tmp extensions are excluded)
        // Note that when you use this function, .lnk and .tmp extensions will no longer be
        // excluded unless you explicitly add them as parameters to the function
        //FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");

        // Add a new quick link to the browser (optional) (returns true if quick link is added successfully)
        // It is sufficient to add a quick link just once
        // Name: Users
        // Path: C:\Users
        // Icon: default (folder icon)
        FileBrowser.AddQuickLink("Users", "C:\\Desktop", null);

        // Example 3: Show a select file dialog using coroutine approach
        StartCoroutine( ShowLoadDialogCoroutine() );
    }

    IEnumerator ShowLoadDialogCoroutine()
    {
        // Show a load file dialog and wait for a response from user
        // Load file/folder: file, Allow multiple selection: true
        // Initial path: default (Documents), Initial filename: empty
        // Title: "Load File", Submit button selectedName: "Load"
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, true, null, null, "Select Files", "Load");

        // Dialog is closed
        // Print whether the user has selected some files or cancelled the operation (FileBrowser.Success)
        Debug.Log(FileBrowser.Success);

        if (FileBrowser.Success)
            OnFilesSelected(FileBrowser.Result); // FileBrowser.Result is null, if FileBrowser.Success is false
    }

    void OnFilesSelected(string[] filePaths)
    {
        // Print paths of the selected files
        for (int i = 0; i < filePaths.Length; i++)
            Debug.Log(filePaths[i]);

        // Get the file path of the first selected file
        string filePath = filePaths[0];

        bool ok = StepToObjWrapper.Convert(filePath);
        Debug.Log("Conversion success: " + ok);

        LoadObj(filePath.Replace(".stp", ".obj"));

        // Read the bytes of the first file via FileBrowserHelpers
        // Contrary to File.ReadAllBytes, this function works on Android 10+, as well
        //byte[] bytes = FileBrowserHelpers.ReadBytesFromFile(filePath);

        // Or, copy the first file to persistentDataPath
        //string destinationPath = Path.Combine(Application.persistentDataPath, FileBrowserHelpers.GetFilename(filePath));
        //FileBrowserHelpers.CopyFile(filePath, destinationPath);
    }

    public void LoadObj( string objFilePath)
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