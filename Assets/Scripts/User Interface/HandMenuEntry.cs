using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(CircularMoveUI), typeof(Button))]
public class HandMenuEntry : MonoBehaviour
{
    [SerializeField] private UnityEvent onEntrySelected;
    [SerializeField] private Color unselectedColor;
    [SerializeField] private Color selectedColor;
    [SerializeField] private bool  isToggle;
    private bool _state = false;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Invoke);
        GetComponent<Image>().color = unselectedColor;
    }

    public void Invoke()
    {
        if (isToggle)
        {
            _state = !_state;

            if (_state)
            {
                GetComponentInParent<Image>().color = selectedColor;
            }
            else
            {
                GetComponentInParent<Image>().color = unselectedColor;
            }
        }
        onEntrySelected?.Invoke();
    }

    // OnClick Functions for UI Buttons
    public void LockPosition()
    {
        foreach (var grabbable in FindObjectsByType<XRGrabInteractable>(FindObjectsSortMode.None))
            grabbable.trackPosition = !_state;
    }

    public void LockRotation()
    {
        foreach (var grabbable in FindObjectsByType<XRGrabInteractable>(FindObjectsSortMode.None))
            grabbable.trackRotation = !_state;
    }

    public void MainMenu(StateManager stateManager)
    {
        stateManager.GoToMainMenu();
    }
}
