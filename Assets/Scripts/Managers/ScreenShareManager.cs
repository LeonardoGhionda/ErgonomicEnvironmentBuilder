using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ScreenShareManager : MonoBehaviour
{
    [SerializeField] private Transform handScreen;
    private RawImage handRaw;
    [SerializeField] private Transform floatingScreen;
    private RawImage floatingRaw;

    private ScreenReceiver _screenReceiver;
    private Transform _current;
    private InputAction _toggleAction;

    private void Awake()
    {
        // Initial setup
        _toggleAction = DependencyProvider.Input.VRMenu.ToggleScreen;
        handRaw = handScreen.GetComponentInChildren<RawImage>();
        if (handRaw == null) Debug.LogError($"handRaw null");
        floatingRaw = floatingScreen.GetComponentInChildren<RawImage>();
        if (floatingRaw == null) Debug.LogError($"floatingRaw null");
        _screenReceiver = GetComponent<ScreenReceiver>();
        if (_screenReceiver == null) Debug.LogError($"screenReceiver null");

    }

    void OnEnable()
    {
        Transform initalScreen = handScreen;

        _screenReceiver.rawImage = initalScreen == handScreen ? handRaw : floatingRaw;
        _screenReceiver.enabled = true;

        ChangeScreenType(initalScreen);
        _toggleAction.performed += ToggleScreenWrapper;
    }

    private void ChangeScreenType(Transform value)
    {
        // Disable previous screen
        if (_current != null) _current.gameObject.SetActive(false);

        // Update current reference (This was the recursion error)
        _current = value;

        // Enable new screen
        if (_current != null) _current.gameObject.SetActive(true);
        if (_current != null) _screenReceiver.rawImage = _current == handScreen ? handRaw : floatingRaw;
    }

    private void ToggleScreen()
    {
        if (_current == handScreen) ChangeScreenType(floatingScreen);
        else ChangeScreenType(handScreen);
    }

    private void ToggleScreenWrapper(InputAction.CallbackContext _)
    {
        ToggleScreen();
    }

    private void OnDisable()
    {
        if (_toggleAction != null) _toggleAction.performed -= ToggleScreenWrapper;

        // Safe check to avoid null refs on cleanup
        if (handScreen != null) handScreen.gameObject.SetActive(false);
        if (floatingScreen != null) floatingScreen.gameObject.SetActive(false);
    }
}