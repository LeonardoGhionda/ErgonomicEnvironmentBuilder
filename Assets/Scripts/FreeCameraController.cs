using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Camera), typeof(CapsuleCollider), typeof(Rigidbody))]
public class FreeCameraController : MonoBehaviour
{

    //---CAMERA ELEMENTS---------------
    Camera cameraComponent;
    Rigidbody rb;
    [SerializeField] private Texture2D dot;
    [SerializeField] private int dotSize = 4;
    private bool showDot = false;

    //---PERSPECTIVE MOVEMENT---------
    public float moveSpeed = 5f;
    public float orthoMoveSpeed = 5f;
    public float sprintMultiplier = 2f;
    public float lookSpeed = 0.1f;
    float yaw;
    float pitch;
    Quaternion targetRotation;

    //----ORTHO MOVEMENT
    public float zoomSpeed = 2f;
    readonly float maxOrthoSize = 55f;
    readonly float minOrthoSize = 4f;
    readonly float defaultOrthoSize = 0f;

    //------ROOF--------
    private GameObject roof;
    public GameObject Roof { set { roof = value; } }

    //-------INPUT----------
    AppActions input;
    AppActions.RoomEditPerspectiveActions pActions;
    AppActions.RoomEditOrthoActions oActions;
    

    //----ORTHOGRAPHIC / PERSPECTIVE---------
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

    void Awake()
    {
        //camera elements setup
        rb = GetComponent<Rigidbody>();
        cameraComponent = GetComponent<Camera>();
        cameraComponent.orthographic = true;
        cameraComponent.orthographicSize = (maxOrthoSize - minOrthoSize) / 2f;

        //physics setup
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        //input setup
        input = new AppActions();
        pActions = input.RoomEditPerspective;
        oActions = input.RoomEditOrtho;
        input.RoomEditOrtho.Enable();

    }

    void OnGUI()
    {
        //draw dot
        if (!showDot) return;
        int x = (Screen.width - dotSize) / 2;
        int y = (Screen.height - dotSize) / 2;
        GUI.DrawTexture(new Rect(x, y, dotSize, dotSize), dot);
    }

    void Update()
    {
        if (ortho)
        {
            //switch to the other camera
            if(oActions.SwitchView.WasPressedThisFrame()) Ortho = false;

            //read zoom input 
            float zIn = oActions.ZoomIn.ReadValue<float>();
            float zOut = oActions.ZoomOut.ReadValue<float>();
            float zoom = zIn - zOut;

            //apply zoom
            cameraComponent.orthographicSize += zoom * zoomSpeed;
            cameraComponent.orthographicSize = Mathf.Clamp(cameraComponent.orthographicSize, minOrthoSize, maxOrthoSize);
        }
        else
        {
            //switch to the other camera
            if (pActions.SwitchView.WasPressedThisFrame()) Ortho = true;

            //read movement input (Shouldn't be read on FixedUpdate)
            Vector2 viewValue = pActions.View.ReadValue<Vector2>();
            yaw += viewValue.x * lookSpeed;
            pitch -= viewValue.y * lookSpeed;
            pitch = Mathf.Clamp(pitch, -89f, 89f);

            //rotation will later be applyed in fixedUpdate
            targetRotation = Quaternion.Euler(pitch, yaw, 0);
        }
            
    }

    void FixedUpdate()
    {
        if (ortho)
        {
            Vector3 moveDir;

            //read horizontal input
            Vector2 moveInput = oActions.Move.ReadValue<Vector2>();
            moveDir = new Vector3(moveInput.x, 0f, moveInput.y);


            //apply movement
            rb.MovePosition(rb.position + moveDir * orthoMoveSpeed);
        }
        else
        {
            float speed;
            Vector3 moveDir;

            //read horizontal movement
            Vector2 moveInput = pActions.Move.ReadValue<Vector2>();

            //read vertical movement
            float y = 0f;
            if (pActions.Up.IsPressed()) y += 1f;
            if (pActions.Down.IsPressed()) y -= 1f;

            //get direction
            moveDir = transform.right * moveInput.x + transform.forward * moveInput.y + transform.up * y;
            if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

            //read sprint action
            speed = moveSpeed;
            if (pActions.Sprint.IsPressed())
                speed *= sprintMultiplier;

            //sweep test 
            float distance = speed * Time.fixedDeltaTime;
            //treshold to avoid useless co,mputation
            if (distance > 0.001f && moveDir.sqrMagnitude > 0.001f)
            {
                //predict collision before moving. 
                if (rb.SweepTest(moveDir, out RaycastHit hitInfo, distance + 0.01f, QueryTriggerInteraction.Ignore))
                {
                    //set position just before the collision point
                    distance = Mathf.Max(0f, hitInfo.distance - 0.01f);
                }
            }

            //translate
            rb.MovePosition(rb.position + moveDir * distance);

            //rotate 
            rb.MoveRotation(targetRotation);
        }
    }

    private void OnEnable()
    {
        //enable current used inut
        if (ortho)
        {
            oActions.Enable();
        }
        else
        {
            pActions.Enable();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            showDot = true;
        }
    }

    private void OnDisable()
    {
        //enable mouse
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        showDot = false;

        //disable input
        oActions.Disable();
        pActions.Disable();
    }
}