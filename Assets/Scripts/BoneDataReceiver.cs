using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using UnityEngine;

public class BoneDataReceiver : MonoBehaviour
{
    // -- Bones --
    [SerializeField] private Transform RootBone; 
    [SerializeField] private Transform Avatar;
    [SerializeField] private Transform HeadBone;
    [SerializeField] private Transform Hip;
    private List<Transform> bones;

    // -- Calibration --
    //[Header("Calibration")]
    //[SerializeField] private Vector2 HeadOffset = Vector2.zero;
    //[SerializeField] private float YRotationStep = 0.0f;

    public int port = 5000;

    private UdpClient udpClient;
    private IPEndPoint endPoint;
    private byte[] receiveBuffer;
    private bool hasNewData = false;


    void Start()
    {

        //Vector3 localHeadPos = Avatar.InverseTransformPoint(HeadBone.position);
        //HeadOffset = new Vector2(localHeadPos.x, localHeadPos.z);
        

        bones = new List<Transform>();
        GetBoneStructRecursive(RootBone);

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
        catch (ObjectDisposedException)
        { }
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

            int expectedSize = sizeof(float) * 3 + (sizeof(float) * 4 * bones.Count);

            if (receiveBuffer.Length >= expectedSize)
            {
                // Casts byte memory to floats directly to avoid conversion overhead and memory allocation.
                ReadOnlySpan<float> floats = MemoryMarshal.Cast<byte, float>(receiveBuffer);

                // Set Avatar position using the first 3 floats
                Avatar.localPosition = new (floats[0], Avatar.localPosition.y, floats[2]);
                Hip.localPosition = new(Hip.localPosition.x, floats[1], Hip.localPosition.z);

                for (int i = 0; i < bones.Count; i++)
                {
                    // Offset index by 3 to skip the position data
                    int floatIndex = 3 + (i * 4);

                    bones[i].localRotation = new Quaternion(
                        floats[floatIndex],
                        floats[floatIndex + 1],
                        floats[floatIndex + 2],
                        floats[floatIndex + 3]
                    );
                }
            }
        }
    }

    private void OnDestroy()
    {
        udpClient?.Close();
    }
}