using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

public class VibrateOnCollision : MonoBehaviour
{
    private HapticImpulsePlayer _rightController, _leftController;

    [SerializeField, Range(0f, 1f)]
    private float vibrationIntensity = 0.4f;

    [SerializeField] bool Left = true, Right = true;

    private Coroutine vibrationRoutine;

    [ContextMenu("Start Infinite Vibration")]
    public void StartInfiniteVibration()
    {
        if (vibrationRoutine != null)
        {
            StopCoroutine(vibrationRoutine);
        }

        vibrationRoutine = StartCoroutine(VibrationLoop(vibrationIntensity));
    }

    [ContextMenu("Stop Infinite Vibration")]
    public void StopInfiniteVibration()
    {
        if (vibrationRoutine != null)
        {
            StopCoroutine(vibrationRoutine);
            vibrationRoutine = null;
        }
    }

    private IEnumerator VibrationLoop(float intensity)
    {
        while (true)
        {
            if (Right && _rightController != null) _rightController.SendHapticImpulse(intensity, 0.1f);
            if (Left && _leftController != null) _leftController.SendHapticImpulse(intensity, 0.1f);

            // Wait slightly less than the duration to maintain continuity
            yield return new WaitForSeconds(0.05f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_leftController == null || _rightController == null) {
            // This is done here and not in Start because when controller are added/removed during runtime or
            // if they go outside of the sensor camera area they become inactive and not found
            var controllers = FindObjectsByType<HapticImpulsePlayer>(FindObjectsSortMode.None);
            _leftController = controllers.First(c => c.gameObject.name.Contains("Left", System.StringComparison.OrdinalIgnoreCase));
            _rightController = controllers.First(c => c.gameObject.name.Contains("Right", System.StringComparison.OrdinalIgnoreCase));
        }

        StartInfiniteVibration();
    }

    private void OnTriggerExit(Collider other)
    {
        StopInfiniteVibration();
    }
}