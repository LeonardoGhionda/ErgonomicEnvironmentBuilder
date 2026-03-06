using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;


public class MenuRoomView : MonoBehaviour
{
    [SerializeField] RectTransform CardTemplate;

    [Header("Menu Entry")]
    [SerializeField] List<HM_Base> LockPosition;

    string _roomsPath;

    //-------------
    public event Action<string> RoomCardClicked;


    public void RefreshRoomList(GridLayoutGroup container)
    {
        _roomsPath = SavingTools.roomsFolderPath;
        // Clear existing cards
        foreach (Transform child in container.transform)
        {
            if (child != CardTemplate.transform)
                Destroy(child.gameObject);
        }

        // Get room files
        var rooms = GetFilesInFolder(_roomsPath, "*.room");

        // Generate cards for each room
        foreach (var room in rooms)
        {
            GenerateRoomCard(room, container);
        }
    }

    // Helper functions 
    //-----------------
    private void GenerateRoomCard(string room, GridLayoutGroup container)
    {
        string roomNameNoExt = Path.GetFileNameWithoutExtension(room);

        // SetUp
        RectTransform newCard = Instantiate(CardTemplate, container.transform);
        newCard.gameObject.SetActive(true);

        // Add room name text
        newCard.GetComponentInChildren<TextMeshProUGUI>().text = roomNameNoExt;

        // Add preview image
        var image = LoadTexture(Path.ChangeExtension(Path.Combine(_roomsPath, room), "png"));
        if (image != null)
            newCard.GetComponentInChildren<RawImage>().texture = image;

        // Add click listener 
        newCard.GetComponent<Button>().onClick.AddListener(() => RoomCardClicked?.Invoke(roomNameNoExt));
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

        if (File.Exists(imagePath))
        {
            byte[] bytes = File.ReadAllBytes(imagePath);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);
            return tex;
        }
        return null;
    }
}
