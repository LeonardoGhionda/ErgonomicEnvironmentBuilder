using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : NetworkBehaviour
{
    public float speed = 3f;
    public float deadZone = 0.1f;
    private InputAction moveAction;

    private void Start()
    {
        moveAction = InputSystem.actions.FindAction("Player/Move");
    }

    void Update()
    {
        if (!IsOwner) return;

        //get input from system-wide input action asset
        Vector2 input = moveAction.ReadValue<Vector2>();

        //thresholding to get discrete directions
        input.Normalize();

        Vector3 move = speed * Time.deltaTime * new Vector3(input.x, 0, input.y);
        transform.Translate(move, Space.World);
    }
}
