using Unity.Netcode;
using UnityEngine;

public class XROriginMoCapSync : MonoBehaviour
{
    [SerializeField] private Transform mocapPrefab;
    [SerializeField] private Transform vrCamera;

    [SerializeField] private Vector3 EyeOffset = new(0f, 0.15f, 0.1f);
    [SerializeField] private float RotationOffset = 0f;

    private float _oldRotationOffset;

    private Transform _mocap;
    private Transform _mocapRoot;
    private Transform _mocapHead;

    private Vector3 expectedPosition;
    private Quaternion expectedRotation;

    private void Start()
    {
        // Instantiate locally on the server
        _mocap = GameObject.Instantiate(mocapPrefab);
        if (_mocap == null) Debug.LogError($"mocap is null");

        // Spawn across the network for all clients
        NetworkObject netObj = _mocap.GetComponent<NetworkObject>();
        netObj.Spawn();

        _mocapHead = _mocap.Find("MvnPuppet/Avatar/Hips/Chest/Chest2/Chest3/Chest4/Neck 1/Head 1");
        _mocapRoot = _mocap.GetChild(0);

        _oldRotationOffset = RotationOffset;

        AlignRoomToAvatar();
    }

    public void AlignRoomToAvatar()
    {
        // Align room rotation to avatar rotation applying the model offset
        float correctedY = _mocapRoot.rotation.eulerAngles.y + RotationOffset;
        transform.rotation = Quaternion.Euler(0f, correctedY, 0f);

        // Reset expectations to prevent the script from detecting this alignment as a teleport
        expectedPosition = transform.position;
        expectedRotation = transform.rotation;
    }

    private void LateUpdate()
    {
        if (_mocap == null) return;
        if (_mocapRoot == null) return;
        if (_mocapHead == null) return;

        if (_oldRotationOffset != RotationOffset)
        {
            AlignRoomToAvatar();
            _oldRotationOffset = RotationOffset;
        }

        // Calculate how much the XR Origin was moved by an external script like TeleportationProvider
        Vector3 externalDeltaPos = transform.position - expectedPosition;
        Quaternion externalDeltaRot = transform.rotation * Quaternion.Inverse(expectedRotation);

        // Apply that exact movement to the MoCap Avatar container
        if (externalDeltaPos.sqrMagnitude > 0.0001f || Quaternion.Angle(Quaternion.identity, externalDeltaRot) > 0.01f)
        {
            _mocap.position += externalDeltaPos;
            _mocap.rotation = externalDeltaRot * _mocap.rotation;
        }

        // Map standard directions to custom bone axes
        Vector3 customRight = _mocapHead.forward;
        Vector3 customUp = -_mocapHead.right;
        Vector3 customForward = -_mocapHead.up;

        // Build the offset vector and find target eye position
        Vector3 trueOffset = (customRight * EyeOffset.x) + (customUp * EyeOffset.y) + (customForward * EyeOffset.z);
        Vector3 targetEyePosition = _mocapHead.position + trueOffset;

        // Calculate local headset drift and apply position cancellation
        Vector3 headsetDrift = transform.TransformVector(vrCamera.localPosition);
        transform.position = targetEyePosition - headsetDrift;

        // Save the final state to compare against in the next frame
        expectedPosition = transform.position;
        expectedRotation = transform.rotation;
    }
}