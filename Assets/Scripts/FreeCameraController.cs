using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera), typeof(CapsuleCollider), typeof(Rigidbody))]
public class FreeCameraController : MonoBehaviour
{
    // Input
    InputActionMap rbmActionMap;
    InputAction moveAction;
    InputAction viewAction;
    InputAction upAction;
    InputAction downAction;
    InputAction sprintAction;
    InputAction switchCameraAction;
    InputAction zoomInAction;
    InputAction zoomOutAction;

    // GUI
    [SerializeField] private Texture2D dot;
    [SerializeField] private int dotSize = 4;
    private bool showDot = false;

    // Movement
    public float moveSpeed = 5f;
    public float orthoMoveSpeed = 5f;
    public float sprintMultiplier = 2f;
    public float lookSpeed = 0.1f;

    float yaw;
    float pitch;

    // Ortho
    public float zoomSpeed = 2f;
    readonly float maxSize = 55f;
    readonly float minSize = 4f;
    float defaultOrthoSize = 0f;

    bool ortho = true;
    public bool Ortho
    {
        get => ortho;
        set
        {
            ortho = value;
            cameraComponent.orthographic = ortho;
            if (roof != null) roof.SetActive(!ortho);

            Vector3 targetPos;
            Quaternion targetRot;

            if (ortho)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                showDot = false;

                cameraComponent.orthographicSize = defaultOrthoSize;
                targetPos = Vector3.up * 20f;
                targetRot = Quaternion.Euler(90f, 0f, 0f);
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                showDot = true;

                targetPos = Vector3.up * 1.7f;
                targetRot = Quaternion.identity;

                yaw = 0f;
                pitch = 0f;
                targetRotation = targetRot;
            }

            transform.SetPositionAndRotation(targetPos, targetRot);
            rb.position = targetPos;
            rb.rotation = targetRot;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    Quaternion targetRotation;
    Camera cameraComponent;
    Rigidbody rb;


    GameObject roof;
    public GameObject Roof
    {
        get => roof;
        set
        {
            if (roof != null)
            {
                Debug.LogWarning("Trying to set roof multiple times");
                return;
            }
            roof = value;
            float meshSize = roof.GetComponent<MeshRenderer>().bounds.size.z;
            float size = (meshSize / 2f) * 1.2f;
            size = Mathf.Clamp(size, minSize, maxSize);
            defaultOrthoSize = size;
            cameraComponent.orthographicSize = defaultOrthoSize;
            roof.SetActive(false);
        }
    }

    void Awake()
    {
        rbmActionMap = InputSystem.actions.FindActionMap("RoomBuilderControl");
        moveAction = rbmActionMap.FindAction("Move");
        viewAction = rbmActionMap.FindAction("View");
        upAction = rbmActionMap.FindAction("Up");
        downAction = rbmActionMap.FindAction("Down");
        sprintAction = rbmActionMap.FindAction("Sprint");
        switchCameraAction = rbmActionMap.FindAction("SwitchView");
        zoomInAction = rbmActionMap.FindAction("OrthoZoomIn");
        zoomOutAction = rbmActionMap.FindAction("OrthoZoomOut");

        rb = GetComponent<Rigidbody>();
        cameraComponent = GetComponent<Camera>();

        // Physics Setup
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void OnGUI()
    {
        if (!showDot) return;
        int x = (Screen.width - dotSize) / 2;
        int y = (Screen.height - dotSize) / 2;
        GUI.DrawTexture(new Rect(x, y, dotSize, dotSize), dot);
    }

    void Update()
    {
        if (switchCameraAction.WasPressedThisFrame())
            Ortho = !Ortho;

        if (ortho)
        {
            float zIn = zoomInAction.ReadValue<float>();
            float zOut = zoomOutAction.ReadValue<float>();
            float zoom = zIn - zOut;

            cameraComponent.orthographicSize += zoom * zoomSpeed;
            cameraComponent.orthographicSize = Mathf.Clamp(cameraComponent.orthographicSize, minSize, maxSize);
            return;
        }

        Vector2 viewValue = viewAction.ReadValue<Vector2>();
        yaw += viewValue.x * lookSpeed;
        pitch -= viewValue.y * lookSpeed;
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        targetRotation = Quaternion.Euler(pitch, yaw, 0);
    }

    void FixedUpdate()
    {
        float speed;
        Vector3 moveDir;

        if (ortho)
        {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            moveDir = new Vector3(moveInput.x, 0f, moveInput.y);
            speed = orthoMoveSpeed;
        }
        else
        {
            Vector2 mv = moveAction.ReadValue<Vector2>();
            float y = 0f;

            if (upAction.IsPressed()) y += 1f;
            if (downAction.IsPressed()) y -= 1f;

            moveDir = transform.right * mv.x + transform.forward * mv.y + transform.up * y;
            if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

            speed = moveSpeed;
            rb.MoveRotation(targetRotation);
        }

        if (sprintAction.IsPressed())
            speed *= sprintMultiplier;

        //SweepTest for Anti-Tunneling
        float distance = speed * Time.fixedDeltaTime;

        // Only sweep if we are actually moving
        if (distance > 0.001f && moveDir.sqrMagnitude > 0.001f)
        {
            // Predict collision before moving. 
            if (rb.SweepTest(moveDir, out RaycastHit hitInfo, distance + 0.01f, QueryTriggerInteraction.Ignore))
            {
                //set position just before the collision point
                distance = Mathf.Max(0f, hitInfo.distance - 0.01f);
            }
        }

        rb.MovePosition(rb.position + moveDir * distance);
    }

    private void OnEnable()
    {
        if (!cameraComponent.orthographic)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            showDot = true;
        }
        rbmActionMap.Enable();
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        showDot = false;
        rbmActionMap.Disable();
    }
}