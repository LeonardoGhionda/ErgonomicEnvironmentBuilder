using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class BoneDataReceiver : MonoBehaviour
{
    [SerializeField] private Transform RootBone;
    [SerializeField] private Transform Avatar, Hips;
    [SerializeField] private Transform vrCamera;
    [Header("Calibration")]
    [SerializeField] private bool Position = true;
    [SerializeField] private bool AddHeadOffset;
    [SerializeField] private bool RemoveHeadOffset;
    [SerializeField] private float HeadOffset = 0.01f;

    private float initPosX, initPosZ;

    private List<Transform> bones;
    public int port = 5000;

    private UdpClient udpClient;
    private IPEndPoint endPoint;
    private byte[] receiveBuffer;
    private bool hasNewData = false;

    private void OnValidate()
    {
        if (Position)
        {
            Position = false;

            // 1. Move Avatar so the Hips align horizontally with the Camera
            Vector3 posOffset = new Vector3(vrCamera.position.x, Avatar.position.y, vrCamera.position.z) - Hips.position;
            Avatar.position += posOffset;

            // 2. Calculate the rotation needed
            float cameraYaw = vrCamera.eulerAngles.y;
            float hipsYaw = Hips.eulerAngles.y;
            float deltaYaw = (cameraYaw - hipsYaw) + 180f;

            // 3. Apply rotation around the Hips pivot
            Avatar.RotateAround(Hips.position, Vector3.up, deltaYaw);
        }
        if( AddHeadOffset || RemoveHeadOffset)
        {

            float offsetDirection = AddHeadOffset? 1f : -1f;
            AddHeadOffset = false;
            RemoveHeadOffset = false;

            // 4. Move Avatar in the direction of the Camera by HeadOffset
            // We use only the X/Z direction of the camera to keep the skeleton on the ground
            Vector3 cameraForwardFlat = vrCamera.forward;
            cameraForwardFlat.y = 0;
            cameraForwardFlat.Normalize();

            Avatar.position += cameraForwardFlat * HeadOffset * offsetDirection;
        }

    }

    void Start()
    {
        bones = new List<Transform>();
        GetBoneStructRecursive(RootBone);

        //initPosX = PosXTarget.position.x;
        //initPosZ = PosZTarget.position.z;

        udpClient = new UdpClient(port);
        endPoint = new IPEndPoint(IPAddress.Any, port);

        udpClient.BeginReceive(ReceiveCallback, null);
    }

    private void GetBoneStructRecursive(Transform current)
    {
        bones.Add(current);
        foreach (Transform child in current)
        {
            GetBoneStructRecursive(child);
        }
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            receiveBuffer = udpClient.EndReceive(ar, ref endPoint);
            hasNewData = true;

            udpClient?.BeginReceive(ReceiveCallback, null);
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
        }
    }

    void Update()
    {
        if (hasNewData && receiveBuffer != null)
        {
            hasNewData = false;
            int offset = 0;

            int expectedSize = (sizeof(float) * 3) + (sizeof(float) * 4 * bones.Count);

            if (receiveBuffer.Length >= expectedSize)
            {                
                float rootY = BitConverter.ToSingle(receiveBuffer, offset);
                offset += sizeof(float);

                //Avatar.position = new Vector3(/*rootX + */initPosX, Avatar.position.y, Avatar.position.z);
                Hips.position = new Vector3(Hips.position.x, rootY, Hips.position.z);
                //Avatar.position = new Vector3(Avatar.position.x, Avatar.position.y, /*rootZ + */initPosZ);

                // Set all bones rotations
                for (int i = 0; i < bones.Count; i++)
                {
                    float x = BitConverter.ToSingle(receiveBuffer, offset);
                    offset += sizeof(float);

                    float y = BitConverter.ToSingle(receiveBuffer, offset);
                    offset += sizeof(float);

                    float z = BitConverter.ToSingle(receiveBuffer, offset);
                    offset += sizeof(float);

                    float w = BitConverter.ToSingle(receiveBuffer, offset);
                    offset += sizeof(float);

                    bones[i].localRotation = new Quaternion(x, y, z, w);
                }
            }
        }
    }

    private void OnDestroy()
    {
        udpClient?.Close();
    }
}
