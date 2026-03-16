using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpectatorController : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 2f;
    [SerializeField] private float lookSpeed = 0.1f;

    [Header("Components")]
    [SerializeField] private Camera spectatorCamera;
    [SerializeField] private AudioListener audioListener;

    [Header("Input")]
    [SerializeField] private InputActionReference move;
    [SerializeField] private InputActionReference up;
    [SerializeField] private InputActionReference down;
    [SerializeField] private InputActionReference sprint;
    [SerializeField] private InputActionReference viewRotation;

    private float _pitch;
    private float _yaw;

    public override void OnNetworkSpawn()
    {
        // Enable components and inputs only for the local owner
        if (IsOwner)
        {
            spectatorCamera.enabled = true;
            audioListener.enabled = true;

            move.action.Enable();
            up.action.Enable();
            down.action.Enable();
            sprint.action.Enable();
            viewRotation.action.Enable();
        }
        else
        {
            spectatorCamera.enabled = false;
            audioListener.enabled = false;
        }
    }

    public override void OnNetworkDespawn()
    {
        // Disable inputs when destroyed
        if (IsOwner)
        {
            move.action.Disable();
            up.action.Disable();
            down.action.Disable();
            sprint.action.Disable();
            viewRotation.action.Disable();
        }
    }

    private void OnEnable()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnDisable()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void Start()
    {
        Vector3 currentRotation = transform.eulerAngles;
        _pitch = currentRotation.x;
        _yaw = currentRotation.y;
    }

    private void Update()
    {
        // Block movement calculation for the VR host
        if (!IsOwner) return;

        UpdateRotation();
        UpdateMovement();
    }

    private void UpdateRotation()
    {
        Vector2 lookInput = viewRotation.action.ReadValue<Vector2>();

        _yaw += lookInput.x * lookSpeed;
        _pitch -= lookInput.y * lookSpeed;

        _pitch = Mathf.Clamp(_pitch, -90f, 90f);

        transform.eulerAngles = new Vector3(_pitch, _yaw, 0f);
    }

    private void UpdateMovement()
    {
        Vector2 moveInput = move.action.ReadValue<Vector2>();

        float upValue = up.action.ReadValue<float>();
        float downValue = down.action.ReadValue<float>();
        float elevationInput = upValue - downValue;

        Vector3 inputDirection = new Vector3(moveInput.x, elevationInput, moveInput.y).normalized;

        float activeSpeed = moveSpeed;

        if (sprint.action.IsPressed())
        {
            activeSpeed *= sprintMultiplier;
        }

        transform.Translate(inputDirection * (activeSpeed * Time.deltaTime), Space.Self);
    }
}