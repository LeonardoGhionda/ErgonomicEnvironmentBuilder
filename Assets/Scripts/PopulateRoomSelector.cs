using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopulateRoomSelector : MonoBehaviour
{

    string _roomsPath;
    [SerializeField] RectTransform CardTemplate;

    void Start()
    {
        _roomsPath = RoomDataExporter.roomsFolderPath;
        var rooms = GetFilesInFolder(_roomsPath, "*.room");

        foreach ( var room in rooms )
            GenerateIcon(room);
    }

    void GenerateIcon(string room)
    {
        RectTransform newCard = Instantiate(CardTemplate, transform);
        newCard.gameObject.SetActive(true);
        newCard.GetComponentInChildren<TextMeshProUGUI>().text = Path.GetFileNameWithoutExtension(room);
        var image = LoadTexture(Path.ChangeExtension(Path.Combine(_roomsPath, room), "png"));
        if(image != null) 
            newCard.GetComponentInChildren<RawImage>().texture = image;
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

    private Texture2D LoadTexture(string imagePath)
    {

        if (System.IO.File.Exists(imagePath))
        {
            byte[] bytes = System.IO.File.ReadAllBytes(imagePath);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);
            return tex;
        }
        return null; 
    }

}

