using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public struct ArmaturePoseData : INetworkSerializable
{
    public float HipY;
    public float AvatarX;
    public float AvatarZ;
    public Quaternion[] Rotations;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref HipY);
        serializer.SerializeValue(ref AvatarX);
        serializer.SerializeValue(ref AvatarZ);

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
    [SerializeField] private Transform AvatarBone;

    public float rotationThreshold = 0.1f;
    private List<Transform> bones;
    private Quaternion[] previousRotations;

    private readonly NetworkVariable<ArmaturePoseData> poseData = new(
        new ArmaturePoseData { Rotations = new Quaternion[0] },
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
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

    private void LateUpdate()
    {
        if (!IsSpawned) return;

        if (IsServer)
        {
            CheckAndSendBoneMovement();
        }
        else
        {
            ApplyReceivedData();
        }
    }

    private void CheckAndSendBoneMovement()
    {
        bool hasMoved = false;

        if (Mathf.Abs(Hip.localPosition.y - poseData.Value.HipY) > 0.001f ||
            Mathf.Abs(AvatarBone.localPosition.x - poseData.Value.AvatarX) > 0.001f ||
            Mathf.Abs(AvatarBone.localPosition.z - poseData.Value.AvatarZ) > 0.001f)
        {
            hasMoved = true;
        }

        for (int i = 0; i < bones.Count; i++)
        {
            if (Quaternion.Angle(bones[i].localRotation, previousRotations[i]) > rotationThreshold)
            {
                hasMoved = true;
            }

            previousRotations[i] = bones[i].localRotation;
        }

        if (hasMoved)
        {
            ArmaturePoseData currentPose = new ArmaturePoseData
            {
                HipY = Hip.localPosition.y,
                AvatarX = AvatarBone.localPosition.x,
                AvatarZ = AvatarBone.localPosition.z,
                Rotations = new Quaternion[bones.Count]
            };

            for (int i = 0; i < bones.Count; i++)
            {
                currentPose.Rotations[i] = bones[i].localRotation;
            }

            poseData.Value = currentPose;
        }
    }

    private void ApplyReceivedData()
    {
        if (poseData.Value.Rotations == null || poseData.Value.Rotations.Length != bones.Count) return;

        Vector3 hipPos = Hip.localPosition;
        hipPos.y = poseData.Value.HipY;
        Hip.localPosition = hipPos;

        Vector3 avatarPos = AvatarBone.localPosition;
        avatarPos.x = poseData.Value.AvatarX;
        avatarPos.z = poseData.Value.AvatarZ;
        AvatarBone.localPosition = avatarPos;

        for (int i = 0; i < bones.Count; i++)
        {
            bones[i].localRotation = poseData.Value.Rotations[i];
        }
    }
}