using System;
using Unity.Netcode;
using UnityEngine;

public class XROriginMoCapSync : MonoBehaviour
{
    [SerializeField] private Transform mocapPrefab;
    [SerializeField] private Transform vrCamera;

    public Vector3 EyeOffset = new(0f, 0.15f, 0.1f);
    public float RotationOffset = 0f;

    private float _oldRotationOffset;

    private Transform _mocap;
    private Transform _mocapRoot;
    private Transform _mocapHead;

    private Vector3 expectedPosition;
    private Quaternion expectedRotation;
    private Vector3 expectedLocalHeadsetPosition;

    private bool _initialization = false;

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleNetworkDisconnect;

            if (NetworkManager.Singleton.IsServer)
            {
                InitializeHost();
            }
            else
            {
                NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            }
        }
    }

    private void OnDisable()
    {
        var netObj = _mocap.GetComponent<NetworkObject>();

        if (netObj.IsSpawned) netObj.Despawn(true); // Despown the network object destroy gameobject
        else Destroy(_mocap.gameObject);            // Just destroy gameobject if it was never spawned

        _initialization = false;
        NetworkManager.Singleton.Shutdown();
    }

    private void OnServerStarted()
    {
        InitializeHost();
    }

    private void InitializeHost()
    {
        NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        _mocap = Instantiate(mocapPrefab);
        NetworkObject netObj = _mocap.GetComponent<NetworkObject>();
        netObj.Spawn();

        _mocapHead = _mocap.Find("MvnPuppet/Avatar/Hips/Chest/Chest2/Chest3/Chest4/Neck 1/Head 1");
        _mocapRoot = _mocap.GetChild(0);

        _oldRotationOffset = RotationOffset;

        _initialization = true;

        AlignRoomToAvatar();
    }

    public void AlignRoomToAvatar()
    {
        float correctedY = _mocapRoot.rotation.eulerAngles.y + RotationOffset;
        transform.rotation = Quaternion.Euler(0f, correctedY, 0f);

        expectedPosition = transform.position;
        expectedRotation = transform.rotation;
        expectedLocalHeadsetPosition = vrCamera.localPosition;
    }

    private void LateUpdate()
    {
        if (_initialization == false) return;

        if (!NetworkManager.Singleton.IsServer) return;

        if (_oldRotationOffset != RotationOffset)
        {
            AlignRoomToAvatar();
            _oldRotationOffset = RotationOffset;
        }

        // Calculate teleportation movement and physical walking movement
        Vector3 externalDeltaPos = transform.position - expectedPosition;
        Quaternion externalDeltaRot = transform.rotation * Quaternion.Inverse(expectedRotation);

        Vector3 localHeadsetDelta = vrCamera.localPosition - expectedLocalHeadsetPosition;
        Vector3 worldHeadsetDelta = transform.TransformVector(localHeadsetDelta);

        // Push the mannequin by the combined teleport and walking distance
        if (externalDeltaPos.sqrMagnitude > 0.0001f || worldHeadsetDelta.sqrMagnitude > 0 || Quaternion.Angle(Quaternion.identity, externalDeltaRot) > 0.01f)
        {
            _mocap.position += externalDeltaPos + worldHeadsetDelta;
            _mocap.rotation = externalDeltaRot * _mocap.rotation;
        }

        // Calculate offset axes based on the mannequin head orientation
        Vector3 customRight = _mocapHead.forward;
        Vector3 customUp = -_mocapHead.right;
        Vector3 customForward = -_mocapHead.up;

        Vector3 trueOffset = (customRight * EyeOffset.x) + (customUp * EyeOffset.y) + (customForward * EyeOffset.z);
        Vector3 targetEyePosition = _mocapHead.position + trueOffset;

        // Pull the XR Origin so the physical camera lands exactly on the target eye position
        Vector3 headsetDrift = transform.TransformVector(vrCamera.localPosition);
        transform.position = targetEyePosition - headsetDrift;

        // Save all states to compare against in the next frame
        expectedPosition = transform.position;
        expectedRotation = transform.rotation;
        expectedLocalHeadsetPosition = vrCamera.localPosition;
    }

    private void HandleNetworkDisconnect(ulong clientId)
    {
        if (NetworkManager.Singleton != null && clientId == NetworkManager.Singleton.LocalClientId)
        {
            enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleNetworkDisconnect;
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        }
    }
}