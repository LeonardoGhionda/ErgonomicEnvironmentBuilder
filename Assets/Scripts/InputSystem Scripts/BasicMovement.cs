using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[RequireComponent(typeof(Rigidbody))]
public class BasicMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 5f;

    [Tooltip("frames before a new jump is possible")]
    public int jumpOffset = 10;
    private int jumpOffsetCount = 0;

    [Tooltip("horizontal movement reduction while in air")]
    [Range(0, 10)]
    public int airResistance = 3;
    private float airResistanceCoeff;

    private InputAction moveAction;
    private InputAction jumpAction;

    private Rigidbody rb;
    private CapsuleCollider capsule;

    private Vector2 lastMoveValue = Vector2.zero;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        capsule = GetComponent<CapsuleCollider>();

        airResistanceCoeff = 1.0f - (float)airResistance / 100.0f;
    }

    void Update()
    {
        bool isGrounded = IsGrounded();

        //Move input and handling
        Vector2 moveValue = isGrounded? moveAction.ReadValue<Vector2>() : lastMoveValue;
        lastMoveValue = moveValue * airResistanceCoeff;
        Vector3 move = transform.right * moveValue.x + transform.forward * moveValue.y;

        Vector3 newVelocity = new Vector3(move.x * moveSpeed, rb.linearVelocity.y, move.z * moveSpeed);

        if (jumpOffsetCount > 0)
        {
            jumpOffsetCount--;
        }

        //jump pressed and player is touching ground
        if (jumpAction.IsPressed() && isGrounded)
        {
            jumpOffsetCount = jumpOffset;
            newVelocity.y += jumpForce;
        }

        rb.linearVelocity = newVelocity;
    }

    // Ground check with raycast
    private bool IsGrounded()
    {
        bool isGrounded = Physics.Raycast(transform.position, Vector3.down, capsule.height / 2 + 0.001f);
        return jumpOffsetCount == 0 && isGrounded;
    }
}
