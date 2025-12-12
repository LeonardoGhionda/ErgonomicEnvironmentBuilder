using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EditorHUDView : MonoBehaviour
{
    [Header("Menus")]
    [SerializeField] private RectTransform exitMenu;
    [SerializeField] private RectTransform modelsMenu;
    [SerializeField] private RectTransform selectedMenu;

    [Header("Controls")]
    [SerializeField] private TextMeshProUGUI selectedName;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button saveButton;

    [Header("Transform Type")]
    [SerializeField] private TransformTypeButton translateButton;
    [SerializeField] private TransformTypeButton rotateButton;
    [SerializeField] private TransformTypeButton scaleButton;

    [Header("Model Menu")]
    public GridLayoutGroup modelButtonContainer;


    // Events for the State to listen to
    public event Action OnQuitClicked;
    public event Action OnSaveClicked;
    public event Action<string> OnModelButtonClicked;
    public event Action<TransformMode> OnTranformButtonClicked;

    private void Start()
    {
        // Link Buttons
        quitButton.onClick.AddListener(() => OnQuitClicked?.Invoke());
        saveButton.onClick.AddListener(() => OnSaveClicked?.Invoke());

        translateButton.GetComponent<Button>().onClick.AddListener(() => OnTranformButtonClicked?.Invoke(translateButton.Mode));
        rotateButton.GetComponent<Button>().onClick.AddListener(() => OnTranformButtonClicked?.Invoke(rotateButton.Mode));
        scaleButton.GetComponent<Button>().onClick.AddListener(() => OnTranformButtonClicked?.Invoke(scaleButton.Mode));

        //generate model buttons
        var modelButtons = ModelButtonGenerator.Init(modelButtonContainer);
        modelButtonPopulate(modelButtons);

        // Default state
        HideAllMenus();
    }

    public void ToggleExitMenu(bool show)
    {
        HideAllMenus(); // Close others first
        exitMenu.gameObject.SetActive(show);
    }

    public void ToggleModelsMenu(bool show)
    {
        HideAllMenus();
        modelsMenu.gameObject.SetActive(show);
    }

    public void ShowSelectionMenu(GameObject target, GizmoManager gizmoTarget)
    {
        // Only show if we actually have a target
        if (target == null)
        {
            selectedMenu.gameObject.SetActive(false);
            return;
        }

        selectedMenu.gameObject.SetActive(true);
        selectedName.text = target.name;

        // Update Gizmo Buttons (Logic kept from your original script)
        UpdateGizmoReferences(gizmoTarget);
    }

    public void HideAllMenus()
    {
        exitMenu.gameObject.SetActive(false);
        modelsMenu.gameObject.SetActive(false);
        selectedMenu.gameObject.SetActive(false);
    }

    private void UpdateGizmoReferences(GizmoManager target)
    {
        // Button Logic
        var gizmoButtons = selectedMenu.gameObject.GetComponentsInChildren<TransformTypeButton>();
        foreach (var button in gizmoButtons)
        {
            button.Target = target;
        }

        Debug.LogWarning("TODO:Implement");
        // Dropdown Logic
        //var gizmoDropdown = gameObject.GetComponentInChildren<ChangeGizmoModeDropdown>(true);
        //if (gizmoDropdown) gizmoDropdown.Target = target;
    }

    // --- model button --
    
    public void modelButtonPopulate(List<ModelButton> modelButtons)
    {
        foreach (var button in modelButtons)
        {
            string objPath = button.OBJFullpath;
            //add event listener
            button.GetComponent<Button>().onClick.AddListener(() => OnModelButtonClicked?.Invoke(objPath));
            //place button
            button.transform.SetParent(modelButtonContainer.transform, false);
        }
    }
}