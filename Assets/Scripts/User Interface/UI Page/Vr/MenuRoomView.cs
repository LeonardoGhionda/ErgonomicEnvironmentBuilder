using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;


public class MenuRoomView : MonoBehaviour
{
    [SerializeField] GridLayoutGroup roomCardContainer;
    [SerializeField] RectTransform CardTemplate;
    [SerializeField] HandMenuManager handMenu;
    [SerializeField] ContinuousMoveProvider moveProvider;

    [Header("Menu Entry")]
    [SerializeField] List<HM_Base> LockPosition;

    string _roomsPath;

    //-------------
    public event Action<string> RoomCardClicked;

    public void StartHandMenu()
    {
        /*
        List<HandMenuEntry> entries = new List<HandMenuEntry> { LockPosition, LockRotation};
        // Initialize handMenu menu
        handMenu.AddMenuEntries(entries, true);

        LockPosition.GetComponent<Button>().onClick.AddListener(() => OnLockPosition?.Invoke(LockPosition.Toggle()));
        LockRotation.GetComponent<Button>().onClick.AddListener(() => OnLockRotation?.Invoke(LockRotation.Toggle()));
        */
    }

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

    public void HandMenuActions(HandMenuInput input) => handMenu.ProcessInput(input);

    public void ToggleHandMenu()
    {
        // Enable/Disable controller manager based on handMenu menu state -> prevent conflicts
        moveProvider.enabled = handMenu.gameObject.activeInHierarchy;
        // Toggle handMenu menu visibility
        handMenu.gameObject.SetActive(!handMenu.gameObject.activeInHierarchy);
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
