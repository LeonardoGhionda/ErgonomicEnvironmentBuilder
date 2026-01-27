using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

public class ImmersiveEditorView : MonoBehaviour
{
    [SerializeField] HandMenuHandler handMenu;
    [SerializeField] ContinuousMoveProvider moveProvider;
    [SerializeField] HandMenuEntry HMEntryTemplate;

    [Header("Hand Menu Entries")]
    [SerializeField] HandMenuEntry lockAllPosition;
    [SerializeField] HandMenuEntry lockAllRotation;
    [SerializeField] HandMenuEntry mainMenu;
    [SerializeField] HandMenuEntry openModelLib;
    [SerializeField] HandMenuEntry applyGravity;

    // When selected exist
    [SerializeField] HandMenuEntry deleteSelected;
    [SerializeField] HandMenuEntry snap;
    [SerializeField] HandMenuEntry snapNFollow;
    [SerializeField] HandMenuEntry stopFollow;
    [SerializeField] HandMenuEntry follow;
    [SerializeField] HandMenuEntry toggleSelectedGravity;

    // Model Library
    [SerializeField] HandMenuEntry exitLib;

    // Action for each button
    public Action<bool> OnLockAllPositionClick;
    public Action<bool> OnLockAllRotationClick;
    public Action OnMainMenuClick;
    public Action OnDeleteSelectedClick;
    public Action<string> OnModelClicked;
    public Action<bool> OnSnap;
    public Action OnSnapNFollow;
    public Action OnStopFollow;
    public Action OnFollow;
    public Action<bool> OnGravityToggled;
    public Action OnSelectedGravity;

    private List<HandMenuEntry> _startEntries;


    public void StartHandMenu()
    {
        _startEntries = new List<HandMenuEntry> { lockAllPosition, lockAllRotation, mainMenu, openModelLib, applyGravity };
        // Initialize hand menu
        handMenu.AddMenuEntries(_startEntries, true);

        // Button Listeners 
        lockAllPosition.GetComponent<Button>().onClick.AddListener(() => OnLockAllPositionClick?.Invoke(lockAllPosition.Toggle()));
        lockAllRotation.GetComponent<Button>().onClick.AddListener(() => OnLockAllRotationClick?.Invoke(lockAllRotation.Toggle()));
        mainMenu.GetComponent<Button>().onClick.AddListener(() => OnMainMenuClick?.Invoke());
        openModelLib.GetComponent<Button>().onClick.AddListener(OpenModelLibrary);
        exitLib.GetComponent<Button>().onClick.AddListener(CloseModelLibrary);
        applyGravity.GetComponent<Button>().onClick.AddListener(() => OnGravityToggled?.Invoke(applyGravity.Toggle()));
    }

    public void HandMenuActions(HandMenuInput input) => handMenu.ProcessInput(input);

    public void ToggleHandMenu()
    {
        // Enable/Disable controller manager based on hand menu state -> prevent input conflicts
        moveProvider.enabled = handMenu.gameObject.activeInHierarchy;
        // Toggle hand menu visibility
        handMenu.gameObject.SetActive(!handMenu.gameObject.activeInHierarchy);
    }

    public void AddSelectedHandMenuEntries()
    {
        deleteSelected.GetComponent<Button>().onClick.RemoveAllListeners();
        deleteSelected.GetComponent<Button>().onClick.AddListener(() => OnDeleteSelectedClick?.Invoke());

        snap.GetComponent<Button>().onClick.RemoveAllListeners();
        snap.GetComponent<Button>().onClick.AddListener(() => OnSnap?.Invoke(snap.Toggle()));

        snapNFollow.GetComponent<Button>().onClick.RemoveAllListeners();
        snapNFollow.GetComponent<Button>().onClick.AddListener(() => OnSnapNFollow?.Invoke());

        stopFollow.GetComponent<Button>().onClick.RemoveAllListeners();
        stopFollow.GetComponent<Button>().onClick.AddListener(() => OnStopFollow?.Invoke());

        follow.GetComponent<Button>().onClick.RemoveAllListeners();
        follow.GetComponent<Button>().onClick.AddListener(() => OnFollow?.Invoke());

        toggleSelectedGravity.GetComponent<Button>().onClick.RemoveAllListeners();
        toggleSelectedGravity.GetComponent<Button>().onClick.AddListener(() => OnSelectedGravity?.Invoke());

        handMenu.AddMenuEntries(new List<HandMenuEntry> { deleteSelected, snap, snapNFollow, stopFollow, follow, toggleSelectedGravity}, false);
    }
    public void RemoveSelectedHandMenuEntries() 
        => handMenu.RemoveMenuEntries(
            new List<HandMenuEntry> {
                deleteSelected, 
                snap, 
                snapNFollow, 
                stopFollow, 
                follow, 
                toggleSelectedGravity
            });

    public void RemoveAllHandMenuEntries() => handMenu.RemoveAllEntries();

    private void OpenModelLibrary()
    {
        List<ModelButton> buttons = ModelButtonGenerator.VRInit(HMEntryTemplate);
        List<HandMenuEntry> modelEntries = new();

        foreach (ModelButton button in buttons)
        {
            button.GetComponent<Button>().onClick.AddListener(() => OnModelClicked?.Invoke(button.OBJFullpath));

            var entry = button.GetComponent<HandMenuEntry>();
            modelEntries.Add(entry);

        }

        // add close button
        modelEntries.Add(exitLib);

        // Only proceed if we actually found entries
        if (modelEntries.Count > 0)
        {
            handMenu.AddMenuEntries(modelEntries, true);
        }
    }
    private void CloseModelLibrary()
    {
        handMenu.AddMenuEntries(_startEntries, true);
    }
    
}
