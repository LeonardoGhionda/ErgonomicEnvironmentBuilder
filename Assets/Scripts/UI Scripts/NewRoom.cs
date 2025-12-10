using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class NewRoom: MonoBehaviour
{
    [SerializeField] private Scrollbar zoomSlider;
    [SerializeField] private TMP_Text zoomText;
    [Tooltip("Rect where the Room 2d laypout reside")]
    [SerializeField] private RectTransform rect;
    [Tooltip("Button that start the conversion from 2d to 3d room")]
    [SerializeField] private Button confirm;
    [SerializeField] private RoomBuilderManager rbm;
    [SerializeField] private TMP_Text roomNameError;
    [SerializeField] private Canvas nextScreen;

    [SerializeField] private InputAction goBackAction;
    [SerializeField] private Image goBackLoadUi;

    LongPressData? goBackData;

    /// <summary>
    /// keep the last room name used to force file overwriting if 
    /// confirm is clicked again after warning name already in use
    /// </summary>
    private string lastTriedRoomName = "";

    void Start()
    {
        var text = goBackLoadUi.GetComponentInChildren<TextMeshProUGUI>();
        text.text = UiManager.Instance.PreviousScreenName();

        zoomSlider.onValueChanged.AddListener(ChangeZoomText); 
        zoomSlider.onValueChanged.AddListener(ChangeZoom);
        confirm.onClick.AddListener(() =>
        {
            try
            {
                bool overwrite = lastTriedRoomName == rbm.RoomName;
                RoomDataExporter.SaveRoom(rbm.RoomName, rbm, overwrite);
                RoomDataExporter.CreateRoom(rbm.RoomName);
                //hide ui 
                var um = UiManager.Instance;
                um.RoomName = rbm.RoomName;
                um.ChangeScreen(nextScreen);
            }
            catch (Exception e) 
            {
                roomNameError.text = e.Message;
                if (e.Message == ValidationErrors.inUse)
                {
                    lastTriedRoomName = rbm.RoomName;
                }
                return;
            }
        });

        ChangeZoomText(0f);

        goBackAction = InputSystem.actions.FindAction("Ui/Close");
        if (goBackAction == null)
        {
            Debug.LogError("Ui/Close Action not found");
        }
        goBackAction.Enable();
    }

    private void Update()
    {

        //go to main screen (Long press )
        if(goBackAction.WasPressedThisFrame())
            goBackData = LongPressedActions.RegisterAction(goBackAction);
        if (goBackAction.WasReleasedThisFrame())
            goBackData = null;

        //loading ui element for long press 
        int perc = LongPressedActions.ElapsedPercent(goBackData, 1f);
        if(perc == 0)
        {
            goBackLoadUi.gameObject.SetActive(false);
        }
        if (perc > 0)
        {
            goBackLoadUi.gameObject.SetActive(true);
            goBackLoadUi.fillAmount = (float)perc / 100f;
        }
        if (perc == 100)
        {
            UiManager.Instance.GoToPreviousScreen();
            goBackLoadUi.gameObject.SetActive(false);
        }
    }

    private void ChangeZoomText(float value)
    {
        int zoom = Mathf.RoundToInt(value * 100.0f);
        if (zoom == 100) zoom = 99;
        zoomText.text = $"Zoom: {zoom:00}%";
    }

    private void ChangeZoom(float value)
    {
        rect.localScale = Vector3.one * ( 1 + value);
    }

    private void OnDisable()
    {
        goBackData = null;
    }
}
