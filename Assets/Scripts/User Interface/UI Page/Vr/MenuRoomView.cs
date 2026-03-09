using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuRoomView : MonoBehaviour
{
    [SerializeField] RectTransform CardTemplate;
    [SerializeField] GridLayoutGroup editRoomCardContainer;
    [SerializeField] GridLayoutGroup testRoomCardContainer;

    [Header("Menu Entry")]
    [SerializeField] List<HM_Base> LockPosition;

    string _roomsPath;

    //-------------
    public event Action<string> EditRoomCardClicked;
    public event Action<string> TestRoomCardClicked;

    // These next few function are called directly in the Unity Inspector
    // check in the roomMenu Teleport anchors
    // --- --- ---
    public void ShowEditableRooms() => RefreshRoomList(editRoomCardContainer, EditRoomCardClicked);

    public void ShowTestableRooms() => RefreshRoomList(testRoomCardContainer, TestRoomCardClicked);
    // --- --- --- 

    private void RefreshRoomList(GridLayoutGroup container, Action<string> action)
    {
        _roomsPath = RoomManagementTools.roomsFolderPath;
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
            GenerateRoomCard(room, container, action);
        }
    }

    // Helper functions 
    //-----------------
    private void GenerateRoomCard(string room, GridLayoutGroup container, Action<string> action)
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
        newCard.GetComponent<Button>().onClick.AddListener(() => action?.Invoke(roomNameNoExt));
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
