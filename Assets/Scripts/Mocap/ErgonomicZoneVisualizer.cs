using UnityEngine;

public class ErgonomicZoneVisualizer : MonoBehaviour
{
    [SerializeField] Transform ShoulderL, ShoulderR;
    [SerializeField] Transform ElbowL, ElbowR;
    [SerializeField] Transform WristL, WristR;
    [SerializeField] Transform ThirdTipL, ThirdTipR; // middle (longest) finger tip
    [SerializeField] Transform ThirdKnuckleL, ThirdKnuckleR;
    [SerializeField] Transform KneeL, KneeR;
    [SerializeField] Transform GrabPointL, GrabPointR;
    
    [SerializeField] Material ControllerMatL, ControllerMatR;

    float _upperarmLenght, _forearmLenght, _handLenght;

    float _greenLimit, _yellowLimit;

    float _kneeHeight, _knucklesHeight, _elbowHeight, _shoulderHeight;


    void Awake()
    {
        // Body measure 
        _upperarmLenght = Vector3.Distance(ShoulderL.position, ElbowL.position);
        _forearmLenght = Vector3.Distance(ElbowL.position, WristL.position);
        _handLenght = Vector3.Distance(WristL.position, ThirdTipL.position);

        _kneeHeight = KneeL.position.y;

        float shoulderToKnuckles = Vector3.Distance(ShoulderL.position, ThirdKnuckleL.position);
        _knucklesHeight = ShoulderL.position.y - shoulderToKnuckles;

        float shoulderToElbow = Vector3.Distance(ShoulderL.position, ElbowL.position);
        _elbowHeight = ShoulderL.position.y - shoulderToElbow;

        _shoulderHeight = ShoulderL.position.y;

        // Horizontal parameters initialization
        _greenLimit = _forearmLenght + _handLenght;
        _yellowLimit = _greenLimit + _upperarmLenght;
    }



    private void LateUpdate()
    {
        // --- Horizontal ---

        // --- Left ---
        Vector2 p = GrabPointL.position.horizontalPlane();
        Vector2 anchor = ShoulderL.position.horizontalPlane();

        float d = Vector2.Distance(p, anchor);

        if (d > _yellowLimit) ControllerMatL.color = Color.red;
        else if (d < _yellowLimit && d > _greenLimit) ControllerMatL.color = Color.yellow;
        else ControllerMatL.color = Color.green;

        // --- Right ---
        p = GrabPointR.position.horizontalPlane();
        anchor = ShoulderR.position.horizontalPlane();

        d = Vector2.Distance(p, anchor);

        if (d > _yellowLimit) ControllerMatR.color = Color.red;
        else if (d < _yellowLimit && d > _greenLimit) ControllerMatR.color = Color.yellow;
        else ControllerMatR.color = Color.green;
   
        // --- Vertical ---

        // --- Left ---
        float h = GrabPointL.position.y;

        if (ControllerMatL.color != Color.red)
        {
            if (h > _shoulderHeight || h < _kneeHeight)
            {
                ControllerMatL.color = Color.red;
            }
            else if(ControllerMatL.color == Color.green) 
            {
                if (h < _knucklesHeight || h > _elbowHeight)
                {
                    ControllerMatL.color = Color.yellow;
                }
                //else green -> (nothing to change) 
            }
        }

        // --- Right ---
        h = GrabPointR.position.y;

        if (ControllerMatR.color != Color.red)
        {
            if (h > _shoulderHeight || h < _kneeHeight)
            {
                ControllerMatR.color = Color.red;
            }
            else if (ControllerMatR.color == Color.green)
            {
                if (h < _knucklesHeight || h > _elbowHeight)
                {
                    ControllerMatR.color = Color.yellow;
                }
                //else green -> (nothing to change) 
            }
        }
    }

    private void OnEnable()
    {
        ControllerMatL.color = Color.white;
        ControllerMatR.color = Color.white;
    }

    private void OnDisable()
    {
        ControllerMatL.color = Color.white;
        ControllerMatR.color = Color.white;
    }
}
