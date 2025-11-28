using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

public class InteractableObject : Interactable
{
    private Material selectedMaterial;
    private Material baseMaterial;
    private BuildingUi bui;

    void Awake()
    {
        bui = FindAnyObjectByType<BuildingUi>();
        selectedMaterial = Resources.Load<Material>("Materials/SelectedObject");
        baseMaterial = gameObject.GetComponent<MeshRenderer>().material;
    }

    public override void OnSelect()
    {
        gameObject.GetComponent<MeshRenderer>().material = selectedMaterial;
        var rgt = gameObject.AddComponent<RuntimeGizmoTransform>();
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

