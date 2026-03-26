using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

public class ImmersiveEditorView : MonoBehaviour
{
    HandMenuManager _handMenu;
    [SerializeField] ContinuousMoveProvider moveProvider;

    [SerializeField] List<HM_Base> baseEntries;


    public void Init()
    {
        _handMenu = Managers.Get<HandMenuManager>();
        _handMenu.AddMenuEntries(baseEntries);
    }

    public void HandMenuActions(HandMenuInput input)
    {
        _handMenu.ProcessInput(input);
    }
}
