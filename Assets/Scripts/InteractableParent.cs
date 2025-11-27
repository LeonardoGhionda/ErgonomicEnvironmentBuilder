using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

public class InteractableParent : Interactable
{
    private Material selectedMaterial;
    private Dictionary<int, Material> materialsMap;
    private RightPanel rightPanel;

    void Awake()
    {
        rightPanel = FindAnyObjectByType<RightPanel>();
        selectedMaterial = Resources.Load<Material>("Materials/SelectedObject");
        materialsMap = new();
    }

    public override void OnSelect()
    {

        ForEachChildRecursive(transform, child =>
        {
            if (child.TryGetComponent(out MeshRenderer r))
            {
                materialsMap.Add(r.GetInstanceID(), r.material);
                r.material = selectedMaterial;
            }
            else
            {
                Debug.LogWarning("Renderer not found for: " + child.name);
            }
        });


        var rgt = gameObject.AddComponent<RuntimeGizmoTransform>();

        rightPanel.Visible(true, rgt);
    }

    public override void OnDeselect()
    {
        ForEachChildRecursive(transform, child =>
        {
            if (child.TryGetComponent(out MeshRenderer r))
            {
                if (materialsMap.TryGetValue(r.GetInstanceID(), out Material material))
                    r.material = material;
            }
        });

        materialsMap.Clear();

        var collVisual = gameObject.GetNamedChild(RuntimeGizmoTransform.colliderVisualName);
        Destroy(collVisual);
        Destroy(gameObject.GetComponent<RuntimeGizmoTransform>());

        rightPanel.Visible(false);
    }

    void ForEachChildRecursive(Transform root, System.Action<Transform> action)
    {
        foreach (Transform child in root)
        {
            action(child);
            ForEachChildRecursive(child, action); // recurse
        }
    }
}
