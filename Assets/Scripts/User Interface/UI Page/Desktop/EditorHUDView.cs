using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EditorHUDView : MonoBehaviour
{
    [Header("Menus")]
    [SerializeField] private RectTransform pauseMenu;
    [SerializeField] private RectTransform modelsMenu;
    [SerializeField] private RectTransform selectedMenu;

    [Header("Controls")]
    [SerializeField] private TextMeshProUGUI selectedName;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button importButton;
    [SerializeField] private DynamicUiElement measureButton;
    [SerializeField] private DynamicUiElement clearMeasureButton;

    [Header("Transform Type")]
    [SerializeField] private TransformTypeButton translateButton;
    [SerializeField] private TransformTypeButton rotateButton;
    [SerializeField] private TransformTypeButton scaleButton;
    [SerializeField] private Button coordButton;
    [SerializeField] private CoordText coordText;


    [Header("Transform Menu")]
    [SerializeField] private TMP_InputField localTranslateX;
    [SerializeField] private TMP_InputField localTranslateY;
    [SerializeField] private TMP_InputField localTranslateZ;
    [SerializeField] private TMP_InputField localRotateX;
    [SerializeField] private TMP_InputField localRotateY;
    [SerializeField] private TMP_InputField localRotateZ;
    [SerializeField] private TMP_InputField localScaleX;
    [SerializeField] private TMP_InputField localScaleY;
    [SerializeField] private TMP_InputField localScaleZ;
    [SerializeField] private TMP_InputField globalTranslateX;
    [SerializeField] private TMP_InputField globalTranslateY;
    [SerializeField] private TMP_InputField globalTranslateZ;
    [SerializeField] private TMP_InputField globalRotateX;
    [SerializeField] private TMP_InputField globalRotateY;
    [SerializeField] private TMP_InputField globalRotateZ;

    [Header("Model Menu")]
    public GridLayoutGroup modelButtonContainer;

    // Enums that identify user input 
    public enum TransSpace { Local, Global }
    public enum TransType { Position, Rotation, Scale }
    public enum Axis { X, Y, Z }
    private bool _isUpdatingUI = false;

    // Events for the State to listen to
    public event Action OnQuitClicked;
    public event Action OnSaveClicked;
    public event Action<string> OnModelButtonClicked;
    public event Action<TransformMode> OnTranformButtonClicked;
    public event Action<CoordText> OnCoordinateModeChanged;
    public event Action<TransSpace, TransType, Axis, float> OnTransformInputChanged;
    public event Action OnMeasureButtonPressed;
    public event Action OnClearMeasureButtonPressed;
    public event Action OnImportButtonPressed;

    private bool _pause = false;
    public bool IsPaused => _pause;

    private void Start()
    {
        // Link Buttons
        quitButton.onClick.AddListener(() => OnQuitClicked?.Invoke());
        saveButton.onClick.AddListener(() => OnSaveClicked?.Invoke());

        translateButton.GetComponent<Button>().onClick.AddListener(() => OnTranformButtonClicked?.Invoke(translateButton.Mode));
        rotateButton.GetComponent<Button>().onClick.AddListener(() => OnTranformButtonClicked?.Invoke(rotateButton.Mode));
        scaleButton.GetComponent<Button>().onClick.AddListener(() => OnTranformButtonClicked?.Invoke(scaleButton.Mode));
        coordButton.onClick.AddListener(() => OnCoordinateModeChanged?.Invoke(coordText));
        measureButton.GetComponent<Button>().onClick.AddListener(() => OnMeasureButtonPressed?.Invoke());
        clearMeasureButton.GetComponent<Button>().onClick.AddListener(() => OnClearMeasureButtonPressed?.Invoke());
        importButton.onClick.AddListener(() => OnImportButtonPressed?.Invoke());

        GenerateModelButton();

        // Default state
        HideAllMenus();

        // Setup Transform Input Fields
        SetupGroup(localTranslateX,  localTranslateY,  localTranslateZ,  TransSpace.Local,  TransType.Position);
        SetupGroup(localRotateX,     localRotateY,     localRotateZ,     TransSpace.Local,  TransType.Rotation);
        SetupGroup(localScaleX,      localScaleY,      localScaleZ,      TransSpace.Local,  TransType.Scale);
        SetupGroup(globalTranslateX, globalTranslateY, globalTranslateZ, TransSpace.Global, TransType.Position);
        SetupGroup(globalRotateX,    globalRotateY,    globalRotateZ,    TransSpace.Global, TransType.Rotation);

    }

    public void TogglePauseMenu()
    {
        HideAllMenus(); // Close others first
        _pause = !_pause;
        pauseMenu.gameObject.SetActive(_pause);
    }

    public void GenerateModelButton()
    {
        //generate model buttons
        var modelButtons = ModelButtonGenerator.DTInit(modelButtonContainer);
        modelButtonPopulate(modelButtons);
    }

    public void ToggleModelsMenu(bool show)
    {
        HideAllMenus();
        modelsMenu.gameObject.SetActive(show);
    }

    public void ShowSelectionMenu(GameObject target)
    {
        // Only show if we actually have a target
        if (target == null)
        {
            selectedMenu.gameObject.SetActive(false);
            return;
        }

        selectedMenu.gameObject.SetActive(true);
        selectedName.text = target.name;
    }

    public void HideAllMenus()
    {
        pauseMenu.gameObject.SetActive(false);
        modelsMenu.gameObject.SetActive(false);
        selectedMenu.gameObject.SetActive(false);
    }

    // --- model button --
    
    public void modelButtonPopulate(List<ModelButton> modelButtons)
    {
        ClearModelButton();
        foreach (var button in modelButtons)
        {
            string objPath = button.OBJFullpath;
            //add event listener
            button.GetComponent<Button>().onClick.AddListener(() => OnModelButtonClicked?.Invoke(objPath));
            //place button
            button.transform.SetParent(modelButtonContainer.transform, false);
        }
    }

    private void ClearModelButton()
    {
        foreach (Transform child in modelButtonContainer.transform)
        {
            Destroy(child.gameObject);
        }
    }

    // --- Transform Input Fields ---
    private void SetupGroup(TMP_InputField x, TMP_InputField y, TMP_InputField z, TransSpace space, TransType type)
    {
        SetupField(x, space, type, Axis.X);
        SetupField(y, space, type, Axis.Y);
        SetupField(z, space, type, Axis.Z);
    }

    private void SetupField(TMP_InputField field, TransSpace space, TransType type, Axis axis)
    {
        field.onValueChanged.AddListener((val) =>
        {
            if (_isUpdatingUI) return;

            // Handle edge cases like "-", ".", empty string
            if (string.IsNullOrEmpty(val) || val == "-" || val == ".") return;

            if (float.TryParse(val, out float result))
            {
                OnTransformInputChanged?.Invoke(space, type, axis, result);
            }
        });
    }

    // Called by State (e.g., during Update loop or selection change)
    public void UpdateTransformUI(Transform t)
    {
        _isUpdatingUI = true; // Lock events

        // Local
        SetSafeText(localTranslateX, t.localPosition.x);
        SetSafeText(localTranslateY, t.localPosition.y);
        SetSafeText(localTranslateZ, t.localPosition.z);

        SetSafeText(localRotateX, t.localRotation.x);
        SetSafeText(localRotateY, t.localRotation.y);
        SetSafeText(localRotateZ, t.localRotation.z);

        SetSafeText(localScaleX, t.localScale.x);
        SetSafeText(localScaleY, t.localScale.y);
        SetSafeText(localScaleZ, t.localScale.z);

        // Global
        SetSafeText(globalTranslateX, t.position.x);
        SetSafeText(globalTranslateY, t.position.y);
        SetSafeText(globalTranslateZ, t.position.z);

        SetSafeText(globalRotateX, t.rotation.x);
        SetSafeText(globalRotateY, t.rotation.y);
        SetSafeText(globalRotateZ, t.rotation.z);

        _isUpdatingUI = false; // Unlock events
    }

    // CRITICAL helper to prevent overwriting the user while they type
    private void SetSafeText(TMP_InputField field, float value)
    {
        // If the user is currently typing in THIS field, do not overwrite their text
        // This prevents "5." turning into "5.00" instantly and deleting the dot
        if (field.isFocused) return;

        field.text = value.ToString("F2");
    }
}