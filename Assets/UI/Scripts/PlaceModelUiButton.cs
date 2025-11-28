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
    //its better to set this from outside to avoid searching for it every time
    public void SetSelectionManager(SelectionManager sm)
    {
        selectionManager = sm;
    }

    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(PlaceObj);
        var folderPath = Path.Combine(GetModelUi.ModelsFolder, gameObject.name);
        OBJPath = Directory.GetFiles(folderPath, "*.obj", SearchOption.TopDirectoryOnly).FirstOrDefault();
    }

    void PlaceObj()
    {
        Assert.NotNull(selectionManager, "Selection manager not set in PlaceModelUiButton");

        if (string.IsNullOrEmpty(OBJPath))
        {
            Debug.LogError("OBJ path is null or empty");
            return;
        }
        OBJLoader loader = new();
        GameObject obj = loader.Load(OBJPath);
        SetUpModel(obj);

        BuildingUi uiManager = FindFirstObjectByType<BuildingUi>();
        if (uiManager != null)
        {
            uiManager.CloseMenu();
        }

        selectionManager.ChangeSelectedObject(obj.GetComponentInChildren<InteractableObject>());
    }

    private void SetUpModel(GameObject parent)
    {
        parent.name = $"[P] {parent.name}";
        parent.tag = SelectionManager.parentTag;

        parent.AddComponent<InteractableParent>();

        parent.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 5f;

        MeshRenderer[] childrenMRs = parent.GetComponentsInChildren<MeshRenderer>();

        foreach (var mr in childrenMRs)
        {
            var childCollider = mr.gameObject.AddComponent<BoxCollider>();
            mr.gameObject.AddComponent<InteractableObject>();
        }
    }
}
