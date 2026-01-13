using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PopulateRoomSelector : MonoBehaviour
{

    string roomsPath = RoomDataExporter.roomsFolderPath;

    void Start()
    {
        
    }

    public static List<string> GetFilesInFolder(string folderPath, string searchPattern = "*.*")
    {
        List<string> results = new List<string>();

        if (!Directory.Exists(folderPath))
        {
            Debug.LogWarning($"[FileBrowser] Folder not found: {folderPath}");
            return results;
        }

        // Get all file paths
        string[] fullPaths = Directory.GetFiles(folderPath, searchPattern);

        foreach (string path in fullPaths)
        {
            //Get name with extension (e.g., "Room1.json")
            string name = Path.GetFileName(path);

            // Skip Unity .meta files
            if (Path.GetExtension(path) == ".meta") continue;

            results.Add(name);
        }

        return results;
    }
}

