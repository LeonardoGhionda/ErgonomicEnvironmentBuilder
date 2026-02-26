using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class BoneDataReceiver : MonoBehaviour
{
    [SerializeField] private Transform RootBone;
    [SerializeField] private Transform PosXTarget, PosYTarget, PosZTarget;

    private float initPosX, initPosZ;

    private List<Transform> bones;
    public int port = 5000;

    private UdpClient udpClient;
    private IPEndPoint endPoint;
    private byte[] receiveBuffer;
    private bool hasNewData = false;

    void Start()
    {
        bones = new List<Transform>();
        GetBoneStructRecursive(RootBone);

        initPosX = PosXTarget.position.x;
        initPosZ = PosZTarget.position.z;

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

            udpClient.BeginReceive(ReceiveCallback, null);
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
                float rootX = BitConverter.ToSingle(receiveBuffer, offset);
                offset += sizeof(float);

                float rootY = BitConverter.ToSingle(receiveBuffer, offset);
                offset += sizeof(float);

                float rootZ = BitConverter.ToSingle(receiveBuffer, offset);
                offset += sizeof(float);

                PosXTarget.position = new Vector3(rootX + initPosX, PosXTarget.position.y, PosXTarget.position.z);
                PosYTarget.position = new Vector3(PosYTarget.position.x, rootY, PosYTarget.position.z);
                PosZTarget.position = new Vector3(PosZTarget.position.x, PosZTarget.position.y, rootZ + initPosZ);


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
