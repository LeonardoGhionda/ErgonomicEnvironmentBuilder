using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera), typeof(CapsuleCollider), typeof(Rigidbody))]
public class FreeCameraController : MonoBehaviour
{
    //input
    InputActionMap rbmActionMap;
    InputAction moveAction;
    InputAction viewAction;
    InputAction upAction;
    InputAction downAction;
    InputAction sprintAction;
    InputAction switchCameraAction;
    InputAction zoomInAction;
    InputAction zoomOutAction;

    //dot
    [SerializeField] private Texture2D dot;
    [SerializeField] private int dotSize = 4;
    private bool showDot = false;

    public float moveSpeed = 5f;
    public float sprintMultiplier = 2f;
    public float lookSpeed = 0.1f;

    float yaw;
    float pitch;


    //ortho
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
            roof.SetActive(!ortho);

            if (ortho)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                showDot = false;

                cameraComponent.orthographicSize = defaultOrthoSize;
                transform.SetPositionAndRotation(Vector3.up * 20f, Quaternion.Euler(90f, 0f, 0f));
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                showDot = true;

                transform.SetPositionAndRotation(Vector3.up * 1.7f, Quaternion.identity);
            }
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
            float size = (meshSize / 2f) * 1.2f /*padding*/;
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

        // Look
        Vector2 viewValue = viewAction.ReadValue<Vector2>();
        yaw += viewValue.x * lookSpeed;
        pitch -= viewValue.y * lookSpeed;
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        targetRotation = Quaternion.Euler(pitch, yaw, 0);
    }

    void FixedUpdate()
    {
        if (ortho) return;

        //Move
        Vector2 mv = moveAction.ReadValue<Vector2>();
        float x = mv.x;
        float z = mv.y;
        float y = 0f;

        if (upAction.IsPressed()) y += 1f;
        if (downAction.IsPressed()) y -= 1f;

        Vector3 move =
            transform.right * x +
            transform.forward * z +
            transform.up * y;

        move.Normalize();

        float speed = moveSpeed;
        if (sprintAction.IsPressed())
            speed *= sprintMultiplier;

        rb.MovePosition(rb.position + speed * Time.fixedDeltaTime * move);


        // Look
        rb.MoveRotation(targetRotation);
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
