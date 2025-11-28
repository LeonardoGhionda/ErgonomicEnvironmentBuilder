using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

public class InteractableParent : Interactable
{
    private Material selectedMaterial;
    private Dictionary<int, Material> materialsMap;
    private BuildingUi bui;

    void Awake()
    {
        bui = FindAnyObjectByType<BuildingUi>();
        selectedMaterial = Resources.Load<Material>("Materials/SelectedObject");
        materialsMap = new();
    }

    public override void OnSelect()
    {
        BoxCollider parentCollider = gameObject.AddComponent<BoxCollider>();
        MeshRenderer[] childrenMRs = gameObject.GetComponentsInChildren<MeshRenderer>();

        Bounds combinedBounds = new Bounds(gameObject.transform.position, Vector3.zero);
        bool hasBounds = false;

        foreach (var mr in childrenMRs)
        {
            //calculate bounds
            if (!hasBounds)
            {
                //bound initialization
                combinedBounds = mr.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(mr.bounds);
            }

            //set material
            materialsMap.Add(mr.GetInstanceID(), mr.material);
            mr.material = selectedMaterial;
        }

        if (hasBounds)
        {
            parentCollider.size = combinedBounds.size;
            // The center must be offset relative to the parent's pivot
            parentCollider.center = gameObject.transform.InverseTransformPoint(combinedBounds.center);
        }

        parentCollider.enabled = false;

        var rgt = gameObject.AddComponent<RuntimeGizmoTransform>();

        bui.OpenSelectionPanel(rgt);
    }

    public override void OnDeselect()
    {

        MeshRenderer[] childrenMRs = gameObject.GetComponentsInChildren<MeshRenderer>();
        foreach (var mr in childrenMRs)
        {
            if (materialsMap.TryGetValue(mr.GetInstanceID(), out Material material))
                mr.material = material;
        }

        materialsMap.Clear();

        var collVisual = gameObject.GetNamedChild(RuntimeGizmoTransform.colliderVisualName);
        Destroy(collVisual);
        Destroy(gameObject.GetComponent<RuntimeGizmoTransform>());
        Destroy(gameObject.GetComponent<BoxCollider>());
        bui.CloseMenu();
    }
}
