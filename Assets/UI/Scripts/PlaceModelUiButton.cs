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

        LeftPanel uiManager = FindFirstObjectByType<LeftPanel>();
        if (uiManager != null)
        {
            uiManager.ChangeState();
        }

        selectionManager.ChangeSelectedObject(obj.GetComponentInChildren<InteractableObject>());
    }

    private void SetUpModel(GameObject parent)
    {
        //make parent identifiable in debug
        parent.name = $"[PARENT] {parent.name}";
        //each model arrives with an empty parent object
        //setup is done on the children
        MeshRenderer[] childrenMRs = parent.GetComponentsInChildren<MeshRenderer>();
        foreach (var mr in childrenMRs)
        {
            //ad box collider
            var b = mr.gameObject.AddComponent<BoxCollider>();
            //postion
            b.transform.localPosition = Camera.main.transform.position
              + Camera.main.transform.forward * 5f;
            //add interactable object 
            mr.AddComponent<InteractableObject>();
        }
    }
}
