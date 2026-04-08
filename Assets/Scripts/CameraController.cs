using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Camera), typeof(CapsuleCollider), typeof(Rigidbody))]
public class CameraController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float orthoMoveSpeed = 5f;
    [SerializeField] float sprintMultiplier = 2f;
    [SerializeField] float lookSpeed = 0.1f;
    [SerializeField] float zoomSpeed = 2f;

    // Option editable properties
    public float LookSpeed { get { return lookSpeed; } set { lookSpeed = value; } }

    [Header("References")]
    [SerializeField] private Texture2D dot;

    [Header("Lights")]
    [SerializeField] Light OrthoLight;
    [SerializeField] Light PlayerLight;

    // Components
    public Camera Camera { get { return _cam; } }
    private Camera _cam;

    private Rigidbody _rb;

    // Input (Injected)
    private AppActions _input;
    private bool _isInitialized = false;

    // State
    private bool _ortho = true;
    private Vector3 _orthoPos;
    private float _yaw, _pitch;
    private Quaternion _targetRotation;
    private bool _showDot = false;
    private const float MIN_ORTHO = 4f;
    private const float MAX_ORTHO = 55f;



    public bool IsOrtho => _ortho;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _cam = GetComponent<Camera>();

        // Physics setup
        _rb.useGravity = false;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Default State
        SetOrtho(true);
    }

    // --- INITIALIZATION (Called by State) ---
    public void InitOrtho(AppActions inputInstance, Vector3 startPos, float orthoSize)
    {
        _input = inputInstance;
        _isInitialized = true;

        SetOrtho(true);

        _cam.orthographicSize = orthoSize;
        _rb.MovePosition(startPos);
        _orthoPos = startPos;

        OrthoLight.gameObject.SetActive(true);
        PlayerLight.gameObject.SetActive(true);

        OrthoLight.enabled = true;
        PlayerLight.enabled = false;
    }

    // --- LOGIC ---

    public void ToggleView()
    {
        SetOrtho(!_ortho);
    }

    public void SetOrtho(bool value)
    {
        _ortho = value;

        _cam.orthographic = _ortho;


        if (_ortho)
        {
            SetMouseFree(false);
            _showDot = false;

            // Top-down pos
            _rb.MovePosition(_orthoPos);
            _rb.MoveRotation(Quaternion.Euler(90f, 0f, 0f));
            _targetRotation = transform.rotation;
        }
        else
        {
            SetMouseFree(true);

            _rb.MovePosition(Vector3.up * 1.7f + Vector3.left * (10f));
            _rb.MoveRotation(Quaternion.identity);
            _targetRotation = transform.rotation;
            _yaw = 0; _pitch = 0;
        }

        OrthoLight.enabled = _ortho;
        PlayerLight.enabled = !_ortho;


        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
    }

    public void Move(Vector3 position)
    {
        _rb.MovePosition(position);
    }

    private void Update()
    {
        if (!_isInitialized)
        {
            Debug.LogError("Missing Camera Initialization!!");
            return;
        }

        if (_ortho)
        {
            // Zoom Logic
            float zIn = _input.CameraMovement.ZoomIn.ReadValue<float>();
            float zOut = _input.CameraMovement.ZoomOut.ReadValue<float>();
            float zoom = zIn - zOut;
            _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize + (zoom * zoomSpeed), MIN_ORTHO, MAX_ORTHO);
        }
        else
        {
            // Look Logic
            Vector2 delta = _input.CameraMovement.View.ReadValue<Vector2>();
            _yaw += delta.x * lookSpeed;
            _pitch -= delta.y * lookSpeed;
            _pitch = Mathf.Clamp(_pitch, -89f, 89f);
            _targetRotation = Quaternion.Euler(_pitch, _yaw, 0);
        }
    }

    private void FixedUpdate()
    {
        if (!_isInitialized) return;
        _ = Vector3.zero;
        float currentSpeed = _ortho ? orthoMoveSpeed : moveSpeed;
        Vector3 moveDir;
        if (_ortho)
        {
            Vector2 input = _input.CameraMovement.Move.ReadValue<Vector2>();
            moveDir = new Vector3(input.x, 0, input.y);
        }
        else
        {
            Vector2 input = _input.CameraMovement.Move.ReadValue<Vector2>();
            float y = 0;
            if (_input.CameraMovement.Up.IsPressed()) y = 1;
            if (_input.CameraMovement.Down.IsPressed()) y = -1;

            moveDir = transform.right * input.x + transform.forward * input.y + transform.up * y;
        }

        if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

        //sprint
        if (_input.CameraMovement.Sprint.IsPressed()) currentSpeed *= sprintMultiplier;

        // Perform Move
        float dist = currentSpeed * Time.fixedDeltaTime;

        // Avoid tunnelling
        if (!_ortho)
        {
            //sweep test 
            float distance = currentSpeed * Time.fixedDeltaTime;

            //treshold to avoid useless computation
            if (distance > 0.001f && moveDir.sqrMagnitude > 0.001f)
            {
                //predict collision before moving. 
                if (_rb.SweepTest(moveDir, out RaycastHit hitInfo, distance + 0.01f, QueryTriggerInteraction.Ignore))
                {
                    //set position just before the collision point
                    _ = Mathf.Max(0f, hitInfo.distance - 0.01f);
                }
            }
        }

        _rb.MovePosition(_rb.position + moveDir * dist);

        if (!_ortho) _rb.MoveRotation(_targetRotation);
    }

    private void OnEnable()
    {
        SetMouseFree(!_ortho);
    }

    private void OnDisable()
    {
        SetMouseFree(false);
    }

    private void OnGUI()
    {
        if (_showDot && dot != null)
        {
            float x = (Screen.width - 4) / 2;
            float y = (Screen.height - 4) / 2;
            GUI.DrawTexture(new Rect(x, y, 4, 4), dot);
        }
    }

    public void SetMouseFree(bool enable)
    {
        if (_ortho) return; //mouse is always visible in ortho mode

        if (enable)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            _showDot = false;
            _input.CameraMovement.View.Disable();
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            _showDot = true;
            _input.CameraMovement.View.Enable();
        }
    }
}