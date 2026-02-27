using System.Collections.Generic;
using UnityEngine;

public class FollowMannequin : MonoBehaviour
{
    [SerializeField] private Transform Head;
    [SerializeField] private Camera Camera;

    [SerializeField] private float heightOffset = 0.25f;

    [Header("Stabilization")]
    [SerializeField, Range(1, 20)] private int frameBufferCount = 3;

    private Queue<Vector3> positionBuffer = new Queue<Vector3>();
    private Queue<Quaternion> rotationBuffer = new Queue<Quaternion>();

    private void LateUpdate()
    {
        Vector3 faceForward = -Head.up;
        Vector3 topOfHead = -Head.right;

        Vector3 rawPosition = Head.position + (topOfHead * heightOffset);
        Quaternion rawRotation = Quaternion.LookRotation(faceForward, topOfHead);

        positionBuffer.Enqueue(rawPosition);
        rotationBuffer.Enqueue(rawRotation);

        if (positionBuffer.Count > frameBufferCount)
        {
            positionBuffer.Dequeue();
        }

        if (rotationBuffer.Count > frameBufferCount)
        {
            rotationBuffer.Dequeue();
        }

        Camera.transform.position = GetAveragePosition();
        Camera.transform.rotation = GetAverageRotation();
    }

    private Vector3 GetAveragePosition()
    {
        Vector3 sum = Vector3.zero;
        foreach (Vector3 pos in positionBuffer)
        {
            sum += pos;
        }
        return sum / positionBuffer.Count;
    }

    private Quaternion GetAverageRotation()
    {
        if (rotationBuffer.Count == 0) return Quaternion.identity;

        Quaternion averageRotation = rotationBuffer.Peek();
        int count = 0;

        foreach (Quaternion rot in rotationBuffer)
        {
            if (count > 0)
            {
                float weight = 1f / (count + 1);
                averageRotation = Quaternion.Slerp(averageRotation, rot, weight);
            }
            count++;
        }
        return averageRotation;
    }
}