using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuRoomView : MonoBehaviour
{
    [SerializeField] GridLayoutGroup roomCardContainer;
    [SerializeField] RectTransform CardTemplate;
    
    string _roomsPath;

    //-------------
    public event Action<string> RoomCardClicked;

    public void RefreshRoomList()
    {
        _roomsPath = RoomsUtility.roomsFolderPath;
        // Clear existing cards
        foreach (Transform child in roomCardContainer.transform)
        {
            if (child != CardTemplate.transform)
                Destroy(child.gameObject);
        }
        // Get room files
        var rooms = GetFilesInFolder(_roomsPath, "*.room");

        // Generate cards for each room
        foreach (var room in rooms)
        {
            GenerateRoomCard(room);
        }
    }

    // Helper functions 
    //-----------------
    private void GenerateRoomCard(string room)
    {
        string roomNameNoExt = Path.GetFileNameWithoutExtension(room);

        // SetUp
        RectTransform newCard = Instantiate(CardTemplate, roomCardContainer.transform);
        newCard.gameObject.SetActive(true);

        // Add room name text
        newCard.GetComponentInChildren<TextMeshProUGUI>().text = roomNameNoExt;

        // Add preview image
        var image = LoadTexture(Path.ChangeExtension(Path.Combine(_roomsPath, room), "png"));
        if (image != null)
            newCard.GetComponentInChildren<RawImage>().texture = image;

        // Add click listener 
        newCard.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => RoomCardClicked?.Invoke(roomNameNoExt));
    }

    private List<string> GetFilesInFolder(string folderPath, string searchPattern = "*.*")
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
