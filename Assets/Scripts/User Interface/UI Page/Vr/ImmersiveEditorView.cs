using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

public class ImmersiveEditorView : MonoBehaviour
{
    [SerializeField] HandMenuManager handMenu;
    [SerializeField] ContinuousMoveProvider moveProvider;

    [SerializeField] List<HM_Base> baseEntries;
    [SerializeField] List<HM_Base> selectionEntries;

    HM_Base.Dependencies _dependencies;

    public void Init(HM_Base.Dependencies deps)
    {
        _dependencies = deps;
        handMenu.AddMenuEntries(baseEntries, _dependencies);
    }

    public void HandMenuActions(HandMenuInput input)
    {
        handMenu.ProcessInput(input);
    }

    public void OnSelected()
    {
        handMenu.AddMenuEntries(selectionEntries, _dependencies);
    }
    public void OnDeselect()
    {
        handMenu.RemoveMenuEntries(selectionEntries);
    }

/*
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
*/
    
}
