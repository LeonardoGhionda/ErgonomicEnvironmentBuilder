using System;
using UnityEngine;

public class GenerateTourusCollider : MonoBehaviour
{

    [SerializeField] int segmnets;
    [SerializeField] float capsuleRadius, tourousRadius;

    void Start()
    {
        AddTourusCollider(gameObject, segmnets, capsuleRadius, tourousRadius);
    }


    /// <summary>
    /// Add to a game object a number of capsule collider to simulate a torus shaped collider
    /// </summary>
    /// <param name="target">Gameobject to which collider will be applied</param>
    /// <param name="segments">Number of capsule collider, more for better approximation</param>
    /// <param name="capsuleRadius">External radius - Internal Radius of the torous (thickness)</param>
    /// <param name="torousRadius">Radius of the tourus</param>
    private void AddTourusCollider(GameObject target, int segments, float capsuleRadius, float torousRadius = 1f)
    {
        //angle between each capsule
        float dAngle = Mathf.PI * 2 / segments;

        int stepN = segments / 4;
        int stepLeft = (int)Math.Ceiling((double)stepN / 2); ;
        int currentDir = 2;

        for (int i = 0; i < segments; i++)
        {
            //add collider
            var cc = target.AddComponent<CapsuleCollider>();
            cc.radius = capsuleRadius;
            cc.height = dAngle * torousRadius;
            cc.isTrigger = true;

            //set capsule tangent to the cirle 
            cc.direction = currentDir;
            if (--stepLeft == 0)
            {
                stepLeft = stepN;
                currentDir = (currentDir == 0) ? 2 : 0;
            }
            //set position
            cc.center = new(torousRadius * Mathf.Cos(dAngle * i), 0f, torousRadius * Mathf.Sin(dAngle * i));
        }
    }
}
