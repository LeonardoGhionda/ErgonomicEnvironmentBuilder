using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BuildingUi : MonoBehaviour
{
    private InputAction menuAction;
    private InputActionMap uiActionMap;
    private InputAction modelsMenuAction; //room builder Control
    private InputAction closeAction;

    FreeCameraController fCam;

    [SerializeField] RectTransform exitMenu;
    [SerializeField] RectTransform modelsMenu;
    [SerializeField] RectTransform selectedMenu;

    [SerializeField] private TextMeshProUGUI selectedName;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button saveButton;

    GameObject selected;

    private void Awake()
    {
        //open exit menu
        menuAction = InputSystem.actions.FindAction("RoomBuilderControl/Menu");
        menuAction.Enable();

        //use ui in menu
        uiActionMap = InputSystem.actions.FindActionMap("Ui");

        //close ui 
        closeAction = uiActionMap.FindAction("Close");

        //open left panel
        modelsMenuAction = InputSystem.actions.FindAction("RoomBuilderControl/ModelsMenu");
        modelsMenuAction.Enable();

        //stop FreeCameraController when a menu is open
        fCam = Camera.main.GetComponent<FreeCameraController>();

        string roomName = UiManager.Instance.RoomName;

        if (string.IsNullOrEmpty(roomName))
            Debug.LogError("[BuildingUi: Start()] roomName is null or empty");

        quitButton.onClick.AddListener(() =>
        {
            //save and quit
            RoomDataExporter.Save(roomName);
            QuitRoom();
        });
        saveButton.onClick.AddListener(() =>
        {
            //save
            RoomDataExporter.Save(roomName);
        });
    }

    private void OnEnable()
    {
        fCam.enabled = true;

        menuAction.Enable();
        modelsMenuAction.Enable();

        exitMenu.gameObject.SetActive(false);
        modelsMenu.gameObject.SetActive(false);
        selectedMenu.gameObject.SetActive(false);
    }

    private void Update()
    {
        //open exit menu
        if (menuAction.WasPressedThisFrame())
        {
            exitMenu.gameObject.SetActive(true);
            fCam.enabled = false;
            uiActionMap.Enable();
            modelsMenuAction.Disable();
            menuAction.Disable();
        }

        //open models menu
        if (modelsMenuAction.WasPressedThisFrame())
        {
            modelsMenu.gameObject.SetActive(true);
            fCam.enabled = false;
            uiActionMap.Enable();
            modelsMenuAction.Disable();
            menuAction.Disable();
        }

        //closes menus
        if (closeAction.WasPressedThisFrame())
        {
            CloseMenu();
        }
    }

    public void CloseMenu()
    {
        exitMenu.gameObject.SetActive(false);
        modelsMenu.gameObject.SetActive(false);
        selectedMenu.gameObject.SetActive(false);
        fCam.enabled = true;
        uiActionMap.Disable();
        modelsMenuAction.Enable();
        menuAction.Enable();
    }

    public void OpenSelectionPanel(RuntimeGizmoTransform target = null)
    {
        selectedMenu.gameObject.SetActive(true);
        fCam.enabled = false;
        uiActionMap.Enable();
        modelsMenuAction.Disable();
        menuAction.Disable();
        selectedName.text = target != null ? target.gameObject.name : null;
        ChangeGizmoTarget(target);

        selected = target == null? null : target.gameObject;
    }

    /// <summary>
    /// Makes the gizmoActionMap mode changer buttons affect the given target
    /// </summary>
    /// <param name="target">Gizmo transformable object that will be affected by the ui Gizmo mode changer buttons.
    /// null disable the buttons</param>
    public void ChangeGizmoTarget(RuntimeGizmoTransform target)
    {
        //button
        var gizmoButtons = selectedMenu.gameObject.GetComponentsInChildren<ChangeGizmoModeButton>();
        foreach (var button in gizmoButtons)
        {
            button.Target = target;
        }
        //dropdown
        var gizmoDropdown = gameObject.GetComponentInChildren<ChangeGizmoModeDropdown>(true);
        gizmoDropdown.Target = target;
    }

    private void OnDisable()
    {
        menuAction.Disable();
        modelsMenuAction.Disable();
        uiActionMap.Disable();
    }


    public static void QuitRoom()
    {
        GameObject rContainer = GameObject.Find("Room Container");
        if (rContainer == null)
        {
            Debug.LogError("Room Container not found");
        }

        foreach (Transform o in rContainer.transform)
        {
            Destroy(o.gameObject);
        }

        GameObject oContainer = GameObject.Find("Objects Container");
        if (rContainer == null)
        {
            Debug.LogError("Objects Container not found");
        }

        foreach (Transform o in oContainer.transform)
        {
            Destroy(o.gameObject);
        }
    }
}
