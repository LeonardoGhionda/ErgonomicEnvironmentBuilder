using UnityEngine;

public class XROriginMoCapSync : MonoBehaviour
{
    [SerializeField] private Transform moCapContainer;
    [SerializeField] private Transform moCapRoot;
    [SerializeField] private Transform moCapHead;
    [SerializeField] private Transform vrCamera;

    [SerializeField] private Vector3 eyeOffset = new(0f, 0.15f, 0.1f);
    [SerializeField] private float rootRotationOffset = 0f;

    private Vector3 expectedPosition;
    private Quaternion expectedRotation;

    private void Start()
    {
        AlignRoomToAvatar();
    }

    [ContextMenu("Align Room To Avatar")]
    public void AlignRoomToAvatar()
    {
        // Align room rotation to avatar rotation applying the model offset
        float correctedY = moCapRoot.rotation.eulerAngles.y + rootRotationOffset;
        transform.rotation = Quaternion.Euler(0f, correctedY, 0f);

        // Reset expectations to prevent the script from detecting this alignment as a teleport
        expectedPosition = transform.position;
        expectedRotation = transform.rotation;
    }

    private void LateUpdate()
    {
        // Calculate how much the XR Origin was moved by an external script like TeleportationProvider
        Vector3 externalDeltaPos = transform.position - expectedPosition;
        Quaternion externalDeltaRot = transform.rotation * Quaternion.Inverse(expectedRotation);

        // Apply that exact movement to the MoCap Avatar container
        if (externalDeltaPos.sqrMagnitude > 0.0001f || Quaternion.Angle(Quaternion.identity, externalDeltaRot) > 0.01f)
        {
            moCapContainer.position += externalDeltaPos;
            moCapContainer.rotation = externalDeltaRot * moCapContainer.rotation;
        }

        // Map standard directions to custom bone axes
        Vector3 customRight = moCapHead.forward;
        Vector3 customUp = -moCapHead.right;
        Vector3 customForward = -moCapHead.up;

        // Build the offset vector and find target eye position
        Vector3 trueOffset = (customRight * eyeOffset.x) + (customUp * eyeOffset.y) + (customForward * eyeOffset.z);
        Vector3 targetEyePosition = moCapHead.position + trueOffset;

        // Calculate local headset drift and apply position cancellation
        Vector3 headsetDrift = transform.TransformVector(vrCamera.localPosition);
        transform.position = targetEyePosition - headsetDrift;

        // Save the final state to compare against in the next frame
        expectedPosition = transform.position;
        expectedRotation = transform.rotation;
    }
}