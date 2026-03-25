using System.Reflection;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class HM_MocapCalibration : HM_Base
{
    protected override void OnInitialized()
    {
        base.OnInitialized();
        Debug.Log($"Mocap calib init");
    }

    // Override single choices made previously 
    override public void OnClick()
    {
        base.OnClick();
        Debug.Log($"Mocap calib clicked");
    }
}
