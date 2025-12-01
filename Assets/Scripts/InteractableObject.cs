using Unity.XR.CoreUtils;
using UnityEngine;

public class InteractableObject : Interactable
{
    private Material selectedMaterial;
    private Material baseMaterial;
    private BuildingUi bui;

    void Awake()
    {
        selectedMaterial = Resources.Load<Material>("Materials/SelectedObject");
        baseMaterial = gameObject.GetComponent<MeshRenderer>().material;
    }

    public override void OnSelect()
    {
        gameObject.GetComponent<MeshRenderer>().material = selectedMaterial;
        var rgt = gameObject.AddComponent<RuntimeGizmoTransform>();
        if (bui == null)
            bui = FindAnyObjectByType<BuildingUi>();
        bui.OpenSelectionPanel(rgt);
    }

    public override void OnDeselect()
    {
        if (gameObject.TryGetComponent<MeshRenderer>(out var mesh))
        {
            mesh.material = baseMaterial;
        }

        var collVisual = gameObject.GetNamedChild(RuntimeGizmoTransform.colliderVisualName);
        Destroy(collVisual);
        Destroy(gameObject.GetComponent<RuntimeGizmoTransform>());
        bui.CloseMenu();
    }
}

