using System.Collections;
using UnityEngine;
using UnityEngine.Events; // For events

public class CustomAnimation : MonoBehaviour
{
    // Grouping data makes the Inspector much cleaner
    [System.Serializable]
    public struct AxisSettings
    {
        public bool enabled;
        public float offset;   // How much to move
        public float duration; // How long it takes
        public float delay;    // When to start
        public AnimationCurve curve; // Easing (Linear, EaseIn, EaseOut)
    }

    [Header("Axis Configuration")]
    [SerializeField] private AxisSettings x;
    [SerializeField] private AxisSettings y;
    [SerializeField] private AxisSettings z;

    [Header("Events")]
    public UnityEvent OnStart; 
    public UnityEvent OnComplete; 

    [SerializeField] private bool playOnce = false;

    private bool _played = false;

    private Vector3 _startPosition;
    private Coroutine _animationRoutine;

    private void Awake()
    {
        _startPosition = transform.localPosition;
    }

    [ContextMenu("Play Animation")] // Allows testing from Inspector right-click
    public void Play()
    {
        if (playOnce && _played) return;

        // Stop existing animation to prevent conflict
        if (_animationRoutine != null) StopCoroutine(_animationRoutine);

        OnStart?.Invoke();

        // Reset position before starting
        transform.localPosition = _startPosition;

        _animationRoutine = StartCoroutine(AnimateRoutine());
    }

    private IEnumerator AnimateRoutine()
    {
        float timer = 0f;

        // Calculate the maximum time needed to finish ALL axes
        float maxDuration = GetMaxDuration();

        while (timer < maxDuration)
        {
            timer += Time.deltaTime; // Use Time.fixedDeltaTime if inside FixedUpdate

            // Calculate new position based on independent axes
            float newX = _startPosition.x + EvaluateAxis(x, timer);
            float newY = _startPosition.y + EvaluateAxis(y, timer);
            float newZ = _startPosition.z + EvaluateAxis(z, timer);

            transform.localPosition = new Vector3(newX, newY, newZ);

            yield return null; // Wait for next frame
        }

        // Ensure perfect final position
        transform.localPosition = new Vector3(
            _startPosition.x + (x.enabled ? x.offset : 0),
            _startPosition.y + (y.enabled ? y.offset : 0),
            _startPosition.z + (z.enabled ? z.offset : 0)
        );

        OnComplete?.Invoke();

        if (playOnce) _played = true;

        _animationRoutine = null;
    }

    private float EvaluateAxis(AxisSettings axis, float time)
    {
        if (!axis.enabled) return 0f;

        // 1. Handle Delay
        if (time < axis.delay) return 0f;

        // 2. Calculate normalized time (0 to 1)
        float t = (time - axis.delay) / axis.duration;
        t = Mathf.Clamp01(t);

        // 3. Apply Curve default to Linear
        float curvedT = (axis.curve != null && axis.curve.length > 0)
            ? axis.curve.Evaluate(t)
            : t;

        return axis.offset * curvedT;
    }

    private float GetMaxDuration()
    {
        float max = 0f;
        if (x.enabled) max = Mathf.Max(max, x.delay + x.duration);
        if (y.enabled) max = Mathf.Max(max, y.delay + y.duration);
        if (z.enabled) max = Mathf.Max(max, z.delay + z.duration);
        return max;
    }
}