#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

#if UNITY_EDITOR
[InitializeOnLoad]
#endif
[DisplayStringFormat("{click} at center")]
public class CenterClickComposite : InputBindingComposite<float>
{
    [InputControl(layout = "Button")]
    public int click;

    [InputControl(layout = "Vector2")]
    public int trackpadAxis;

    public float centerTolerance = 0.2f;

    // Registers the composite in the editor
    static CenterClickComposite()
    {
        InputSystem.RegisterBindingComposite<CenterClickComposite>();
    }

    // Registers the composite at runtime
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        InputSystem.RegisterBindingComposite<CenterClickComposite>();
    }

    public override float ReadValue(ref InputBindingCompositeContext context)
    {
        float clickValue = context.ReadValue<float>(click);
        Vector2 axisValue = context.ReadValue<Vector2, Vector2MagnitudeComparer>(trackpadAxis);

        bool isClicking = clickValue > 0.5f;
        bool isCentered = axisValue.magnitude <= centerTolerance;

        if (isClicking && isCentered)
        {
            return clickValue;
        }

        return 0f;
    }

    public override float EvaluateMagnitude(ref InputBindingCompositeContext context)
    {
        return ReadValue(ref context);
    }
}