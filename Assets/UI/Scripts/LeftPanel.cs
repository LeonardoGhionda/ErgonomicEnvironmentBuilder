using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class LeftPanel : MonoBehaviour
{
    //INPUT
    private InputActionMap uiActionMap;
    private InputAction menuAction; //room builder Control
    private InputAction closeAction;


    private bool isOpen = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        uiActionMap = InputSystem.actions.FindActionMap("Ui");
        closeAction = uiActionMap.FindAction("Close");

        menuAction = InputSystem.actions.FindAction("RoomBuilderControl/Menu");
        menuAction.Enable();
        
        Show(false);
        Camera.main.GetComponent<FreeCameraController>().enabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (menuAction.WasPressedThisFrame() || closeAction.WasPressedThisFrame())
        {
            ChangeState();
        }
    }

    /// <summary>
    /// Show/Hide left panel 
    /// Un/Lock all input except UI
    /// en/disable free camera movement
    /// </summary>
    public void ChangeState()
    {
        isOpen = !isOpen;
        Show(isOpen);
        if (isOpen) uiActionMap.Enable(); else uiActionMap.Disable();
        Camera.main.GetComponent<FreeCameraController>().enabled = !isOpen;
    }

    //if I just set all the panel inactive, it wont receive the open command
    private void Show(bool value)
    {
        gameObject.GetComponent<Image>().enabled = value;

        int c = gameObject.transform.childCount;
        for (int i = 0; i < c; i++)
        {
            gameObject.transform.GetChild(i).gameObject.SetActive(value);
        }
    }

    private void OnDestroy()
    {
        if(isOpen)
            uiActionMap.Disable();
        menuAction.Disable();
    }
}
