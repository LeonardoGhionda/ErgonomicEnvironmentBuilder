using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Custom struct to pack the Y position and all bone rotations for Netcode transmission
public struct ArmaturePoseData : INetworkSerializable
{
    public float HipY;
    public Quaternion[] Rotations;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref HipY);

        int length = 0;
        if (serializer.IsWriter)
        {
            length = Rotations != null ? Rotations.Length : 0;
        }

        serializer.SerializeValue(ref length);

        if (serializer.IsReader)
        {
            if (Rotations == null || Rotations.Length != length)
            {
                Rotations = new Quaternion[length];
            }
        }

        for (int i = 0; i < length; i++)
        {
            serializer.SerializeValue(ref Rotations[i]);
        }
    }
}

public class BoneDataSync : NetworkBehaviour
{
    [SerializeField] private Transform RootBone;
    [SerializeField] private Transform Hip;

    public float rotationThreshold = 0.1f;
    private List<Transform> bones;
    private Quaternion[] previousRotations;
    private bool isSending = false;

    // NetworkVariable holds our custom struct and handles syncing from Server to Clients
    private readonly NetworkVariable<ArmaturePoseData> poseData = new (
        new ArmaturePoseData { Rotations = new Quaternion[0] },
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    void Awake()
    {
        bones = new List<Transform>();
        GetBoneStructRecursive(RootBone);

        previousRotations = new Quaternion[bones.Count];
        for (int i = 0; i < bones.Count; i++)
        {
            previousRotations[i] = bones[i].localRotation;
        }
    }

    private void GetBoneStructRecursive(Transform rootBone)
    {
        bones.Add(rootBone);
        foreach (Transform child in rootBone)
        {
            GetBoneStructRecursive(child);
        }
    }

    void LateUpdate()
    {
        if (IsServer)
        {
            // Server logic reads the armature and updates the NetworkVariable
            if (!isSending)
            {
                CheckForBoneMovement();
                if (!isSending) return;
            }

            ArmaturePoseData currentPose = new ()
            {
                HipY = Hip.position.y,
                Rotations = new Quaternion[bones.Count]
            };

            for (int i = 0; i < bones.Count; i++)
            {
                currentPose.Rotations[i] = bones[i].localRotation;
            }

            poseData.Value = currentPose;
        }
        else
        {
            // Client logic applies the data received from the Server
            ApplyReceivedData();
        }
    }

    private void ApplyReceivedData()
    {
        if (poseData.Value.Rotations == null || poseData.Value.Rotations.Length != bones.Count) return;

        Vector3 hipPos = Hip.position;
        hipPos.y = poseData.Value.HipY;
        Hip.position = hipPos;

        for (int i = 0; i < bones.Count; i++)
        {
            bones[i].localRotation = poseData.Value.Rotations[i];
        }
    }

    private void CheckForBoneMovement()
    {
        if (previousRotations == null) return;

        for (int i = 0; i < bones.Count; i++)
        {
            if (Quaternion.Angle(bones[i].localRotation, previousRotations[i]) > rotationThreshold)
            {
                isSending = true;
                previousRotations = null;
                return;
            }
        }

        for (int i = 0; i < bones.Count; i++)
        {
            previousRotations[i] = bones[i].localRotation;
        }
    }
}