using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera), typeof(CapsuleCollider), typeof(Rigidbody))]
public class FreeCameraController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float orthoMoveSpeed = 5f;
    [SerializeField] float sprintMultiplier = 2f;
    [SerializeField] float lookSpeed = 0.1f;
    [SerializeField] float zoomSpeed = 2f;

    [Header("References")]
    [SerializeField] private Texture2D dot;
    private GameObject roof;

    // Components
    public Camera Camera {  get { return _cam; } }
    private Camera _cam;

    private Rigidbody _rb;

    // Input (Injected)
    private AppActions _input;
    private bool _isInitialized = false;

    // State
    private bool _ortho = true;
    private float _yaw, _pitch;
    private Quaternion _targetRotation;
    private bool _showDot = false;
    private bool _lockMove;
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
    public void Init(AppActions inputInstance)
    {
        _input = inputInstance;
        _isInitialized = true;

        roof = GameObject.FindWithTag("Roof");
        SetOrtho(true);
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
        if (roof != null) roof.SetActive(!_ortho);

        if (_ortho)
        {
            SetFPCursor(false);
            _showDot = false;

            // Top-down pos
            _rb.MovePosition(Vector3.up * 50f);
            _rb.MoveRotation(Quaternion.Euler(90f, 0f, 0f));
            _targetRotation = transform.rotation;
        }
        else
        {
            SetFPCursor(true);

            _rb.MovePosition(Vector3.up * 1.7f);
            _rb.MovePosition(Quaternion.identity.eulerAngles);
            _targetRotation = transform.rotation;
            _yaw = 0; _pitch = 0;
        }

        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
    }

    private void Update()
    {
        if (!_isInitialized)
        {
            Debug.LogError("Missing Camera Initialization!!");
            return;
        }
        if (_lockMove) return;

        if (_ortho)
        {
            // Zoom Logic
            float zIn = _input.RoomEditOrtho.ZoomIn.ReadValue<float>();
            float zOut = _input.RoomEditOrtho.ZoomOut.ReadValue<float>();
            float zoom = zIn - zOut;
            _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize + (zoom * zoomSpeed), MIN_ORTHO, MAX_ORTHO);
        }
        else
        {
            // Look Logic
            Vector2 delta = _input.RoomEditPerspective.View.ReadValue<Vector2>();
            _yaw += delta.x * lookSpeed;
            _pitch -= delta.y * lookSpeed;
            _pitch = Mathf.Clamp(_pitch, -89f, 89f);
            _targetRotation = Quaternion.Euler(_pitch, _yaw, 0);
        }
    }

    private void FixedUpdate()
    {
        if (!_isInitialized || _lockMove) return;

        Vector3 moveDir = Vector3.zero;
        float currentSpeed = _ortho ? orthoMoveSpeed : moveSpeed;

        if (_ortho)
        {
            Vector2 input = _input.RoomEditCommon.Move.ReadValue<Vector2>();
            moveDir = new Vector3(input.x, 0, input.y);
        }
        else
        {
            Vector2 input = _input.RoomEditCommon.Move.ReadValue<Vector2>();
            float y = 0;
            if (_input.RoomEditPerspective.Up.IsPressed()) y = 1;
            if (_input.RoomEditPerspective.Down.IsPressed()) y = -1;

            moveDir = transform.right * input.x + transform.forward * input.y + transform.up * y;
        }

        if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

        //sprint
        if (_input.RoomEditCommon.Sprint.IsPressed()) currentSpeed *= sprintMultiplier;
        
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
                    distance = Mathf.Max(0f, hitInfo.distance - 0.01f);
                }
            }
        }

        _rb.MovePosition(_rb.position + moveDir * dist);

        if (!_ortho) _rb.MoveRotation(_targetRotation);
    }

    private void OnEnable()
    {
        SetFPCursor(!_ortho);
    }

    private void OnDisable()
    {
        SetFPCursor(false);
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

    private void SetFPCursor(bool enable)
    {
        if (enable)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            _showDot = true;
        }
        else
        {
            _showDot = false;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void MenuMode(bool enable)
    {
        _lockMove = enable; 
        SetFPCursor(!_ortho && !enable);
    }
}