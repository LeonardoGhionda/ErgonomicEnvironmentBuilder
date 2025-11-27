using UnityEngine;
using UnityEngine.InputSystem;

public class FreeCameraController : MonoBehaviour
{

    private InputAction moveAction;
    private InputAction viewAction;
    private InputAction upAction;
    private InputAction downAction;
    private InputAction sprintAction;

    //dot
    [SerializeField] private Texture2D dot;
    [SerializeField] private int size = 4;
    private bool showDot = true;

    public float moveSpeed = 5f;
    public float sprintMultiplier = 2f;
    public float lookSpeed = 0.1f;

    float yaw;
    float pitch;

    InputActionMap rbmActionMap;

    CharacterController controller;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
            controller = gameObject.AddComponent<CharacterController>();

        controller.height = 1.7f;
        controller.radius = 0.3f;

        rbmActionMap = InputSystem.actions.FindActionMap("RoomBuilderControl");
        if (rbmActionMap == null)
        {
            Debug.LogError("Input action map 'RoomBuilderControl' not found.");
            return;
        }
        moveAction = rbmActionMap.FindAction("Move");
        viewAction = rbmActionMap.FindAction("View");
        upAction = rbmActionMap.FindAction("Up");
        downAction = rbmActionMap.FindAction("Down");
        sprintAction = rbmActionMap.FindAction("Sprint");
    }

    void OnGUI()
    {
        if (!showDot) return;

        int x = (Screen.width - size) / 2;
        int y = (Screen.height - size) / 2;
        GUI.DrawTexture(new Rect(x, y, size, size), dot);
    }

    void Update()
    {
        // Look
        Vector2 viewValue = viewAction.ReadValue<Vector2>();
        yaw += viewValue.x * lookSpeed;
        pitch -= viewValue.y * lookSpeed;
        pitch = Mathf.Clamp(pitch, -89f, 89f);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0);

        // Movement input
        Vector2 mv = moveAction.ReadValue<Vector2>();
        float x = mv.x;
        float z = mv.y;
        float y = 0f;

        if (upAction.IsPressed()) y += 1f;
        if (downAction.IsPressed()) y -= 1f;

        Vector3 move = (transform.right * x) +
                       (transform.forward * z) +
                       (transform.up * y);

        move = move.normalized;

        float speed = moveSpeed;
        if (sprintAction.IsPressed())
            speed *= sprintMultiplier;

        controller.Move(speed * Time.deltaTime * move);
    }

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        showDot = true;
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