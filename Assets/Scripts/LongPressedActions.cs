using UnityEngine;
using UnityEngine.InputSystem;

public struct LongPressData
{
    private InputAction inputAction;
    private float startTime;

    public LongPressData Init(InputAction inputAction)
    {
        this.inputAction = inputAction;
        this.startTime = Time.time;
        return this;
    }

    public readonly float ElapsedTime()
    {
        return Time.time - startTime;
    }
}

/// <summary>
/// Handle actions that are held down for a certain duration
/// </summary>
static class LongPressedActions
{
    /// <summary>
    /// Register an action returning Data that can be used to verify action status 
    /// </summary>
    /// <param name="action"></param>
    /// <returns></returns>
    static public LongPressData RegisterAction(InputAction action)
    {
        return  new LongPressData().Init(action);
    }

    //
    /// <summary>
    /// Check for how much time the action has been held down
    /// 
    /// This must be called from inside the onUpdate of the action handler (e.g UI screen)
    /// </summary>
    /// <param name="longPressData">struct returned during action registration</param>
    /// <param name="durationSeconds">in seconds</param>
    /// <returns> int between 0 and 100 that represent the percent value of the time it has to be held</returns>
    static public int ElapsedPercent(LongPressData? longPressData, float duration)
    {
        if (longPressData == null)
        {
            return 0;
        }

        return ((int)Mathf.Clamp((longPressData.Value.ElapsedTime()) / duration * 100f, 0f, 100f));  
    }
}
