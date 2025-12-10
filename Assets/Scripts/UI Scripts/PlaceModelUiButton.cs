using Dummiesman;
using NUnit.Framework;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class PlaceModelUiButton : MonoBehaviour
{

    Button button;
    private string OBJPath;

    private SelectionManager selectionManager;

    GameObject container;

    //its better to set this from outside to avoid searching for it every time
    public void SetSelectionManager(SelectionManager sm)
    {
        selectionManager = sm;
    }

    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(PlaceObjFromUi);
        var folderPath = Path.Combine(GetModelUi.ModelsFolder, gameObject.name);
        OBJPath = Directory.GetFiles(folderPath, "*.obj", SearchOption.TopDirectoryOnly).FirstOrDefault();

        container = GameObject.Find("Objects Container");
        if (container == null)
        {
            Debug.LogError("Can't find objects container");
        }
    }

    void PlaceObjFromUi()
    {
        Assert.NotNull(selectionManager, "Selection manager not set in PlaceModelUiButton");

        if (string.IsNullOrEmpty(OBJPath))
        {
            Debug.LogError("OBJ path is null or empty");
            return;
        }
        OBJLoader loader = new();
        GameObject obj = loader.Load(OBJPath);
        SetUpModel(obj, OBJPath, container);

        RoomEditHUD uiManager = FindFirstObjectByType<RoomEditHUD>();
        if (uiManager != null)
        {
            uiManager.CloseMenu();
        }

        selectionManager.ChangeSelectedObject(obj.GetComponentInChildren<InteractableObject>());
    }

    public static void SetUpModel(GameObject parent, string path, GameObject container)
    {
        parent.name = $"[P] {parent.name}";
        parent.tag = SelectionManager.parentTag;

        parent.AddComponent<InteractableParent>().Path = path;

        parent.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 5f;    

        MeshRenderer[] childrenMRs = parent.GetComponentsInChildren<MeshRenderer>();

        float minY = 0.0f;

        foreach (var mr in childrenMRs)
        {
            var bc = mr.gameObject.AddComponent<BoxCollider>();
            var bottom = (bc.center.y - bc.size.y / 2f);
            if (bottom < minY) minY = bottom;
            mr.gameObject.AddComponent<InteractableObject>();
        }

        if (Camera.main.GetComponent<FreeCameraController>().Ortho)
        {
            parent.transform.position = Vector3.Scale(parent.transform.position, new Vector3(1f, 0f, 1f));
            parent.transform.position = parent.transform.position - Vector3.up * minY;
        }

        parent.transform.SetParent(container.transform, true);
    }
}
