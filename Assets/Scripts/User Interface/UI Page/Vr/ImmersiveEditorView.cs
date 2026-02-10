using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

public class ImmersiveEditorView : MonoBehaviour
{
    [SerializeField] HandMenuManager handMenu;
    [SerializeField] ContinuousMoveProvider moveProvider;

    [SerializeField] List<HM_Base> baseEntries;

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
}
