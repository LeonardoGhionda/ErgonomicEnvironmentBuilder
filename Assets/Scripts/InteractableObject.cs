using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    private Material selectedMaterial;
    private Material baseMaterial;

    private RightPanel rightPanel;

    void Awake()
    {
        rightPanel = FindAnyObjectByType<RightPanel>();
        selectedMaterial = Resources.Load<Material>("Materials/SelectedObject");
        baseMaterial = gameObject.GetComponent<Renderer>().material;
    }


    public void OnSelected()
    {

        gameObject.GetComponent<Renderer>().material = selectedMaterial;

        var rgt = gameObject.AddComponent<RuntimeGizmoTransform>();

        rightPanel.Visible(true, rgt);
    }

    public void OnDeselected()
    {
        gameObject.GetComponent<Renderer>().material = baseMaterial;

        Destroy(gameObject.GetComponent<RuntimeGizmoTransform>());

        rightPanel.Visible(false);
    }
}

