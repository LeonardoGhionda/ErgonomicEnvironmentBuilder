using UnityEngine;

public class ColliderVisual : MonoBehaviour
{
    [SerializeField] private GameObject edgeTemplate;
    [SerializeField] private float colliderThickness = 0.2f;

    GameObject[] _edges;
    readonly int cubeEdgeN = 12;

    BoxCollider _boxCollider;

    private void Awake()
    {
        _edges = new GameObject[cubeEdgeN];
        for (int i = 0; i < cubeEdgeN; i++)
        {
            _edges[i] = Instantiate(edgeTemplate);
            _edges[i].name = $"edge {i}";
            _edges[i].SetActive(true);
            _edges[i].transform.SetParent(transform);
        }

        gameObject.SetActive(false);
    }

    public void ChangeTarget(BoxCollider boxCollider)
    {
        if (boxCollider == null)
        {
            ClearTarget();
            return;
        }

        _boxCollider = boxCollider;

        gameObject.SetActive(true);
        transform.SetParent(boxCollider.transform, false);
        transform.SetLocalPositionAndRotation(boxCollider.center, Quaternion.identity);
        transform.localScale = Vector3.one;

        // Edges
        // --------

        // --- GROUP 1: Z-Axis Edges (0-3) ---
        // Rotated 90 on X: Local Y aligns with World Z (Length)
        // Local X aligns with World X | Local Z aligns with World Y

        _edges[0].transform.localPosition = new Vector3(boxCollider.size.x / 2f, boxCollider.size.y / 2f, 0f);
        _edges[0].transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        _edges[1].transform.localPosition = new Vector3(boxCollider.size.x / 2f, -boxCollider.size.y / 2f, 0f);
        _edges[1].transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        _edges[2].transform.localPosition = new Vector3(-boxCollider.size.x / 2f, boxCollider.size.y / 2f, 0f);
        _edges[2].transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        _edges[3].transform.localPosition = new Vector3(-boxCollider.size.x / 2f, -boxCollider.size.y / 2f, 0f);
        _edges[3].transform.localRotation = Quaternion.Euler(90f, 0f, 0f);


        // --- GROUP 2: Y-Axis Edges (4-7) ---
        // No Rotation: Local Y aligns with World Y (Length)
        // Local X aligns with World X | Local Z aligns with World Z

        _edges[4].transform.localPosition = new Vector3(boxCollider.size.x / 2f, 0f, boxCollider.size.z / 2f);
        _edges[4].transform.localRotation = Quaternion.identity;

        _edges[5].transform.localPosition = new Vector3(boxCollider.size.x / 2f, 0f, -boxCollider.size.z / 2f);
        _edges[5].transform.localRotation = Quaternion.identity;

        _edges[6].transform.localPosition = new Vector3(-boxCollider.size.x / 2f, 0f, boxCollider.size.z / 2f);
        _edges[6].transform.localRotation = Quaternion.identity;

        _edges[7].transform.localPosition = new Vector3(-boxCollider.size.x / 2f, 0f, -boxCollider.size.z / 2f);
        _edges[7].transform.localRotation = Quaternion.identity;


        // --- GROUP 3: X-Axis Edges (8-11) ---
        // Rotated 90 on Z: Local Y aligns with World X (Length)
        // Local X aligns with World Y | Local Z aligns with World Z

        _edges[8].transform.localPosition = new Vector3(0f, boxCollider.size.y / 2f, boxCollider.size.z / 2f);
        _edges[8].transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

        _edges[9].transform.localPosition = new Vector3(0f, boxCollider.size.y / 2f, -boxCollider.size.z / 2f);
        _edges[9].transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

        _edges[10].transform.localPosition = new Vector3(0f, -boxCollider.size.y / 2f, boxCollider.size.z / 2f);
        _edges[10].transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

        _edges[11].transform.localPosition = new Vector3(0f, -boxCollider.size.y / 2f, -boxCollider.size.z / 2f);
        _edges[11].transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

        UpdateEdgesTickness();

    }

    public void ClearTarget()
    {
        transform.SetParent(null);
        gameObject.SetActive(false);
    }

    private void Update()
    {
        UpdateEdgesTickness();
    }

    void UpdateEdgesTickness()
    {
        // Safety check
        if (_boxCollider == null) return;

        Vector3 pScale = transform.lossyScale;


        float pX = Mathf.Abs(pScale.x) < 0.001f ? 1f : Mathf.Abs(pScale.x);
        float pY = Mathf.Abs(pScale.y) < 0.001f ? 1f : Mathf.Abs(pScale.y);
        float pZ = Mathf.Abs(pScale.z) < 0.001f ? 1f : Mathf.Abs(pScale.z);

        // Pre-calculate compensated thicknesses
        float thickX = colliderThickness / pX;
        float thickY = colliderThickness / pY;
        float thickZ = colliderThickness / pZ;

        Vector3 scaleZ = new Vector3(thickX, _boxCollider.size.z / 2f, thickY);

        _edges[0].transform.localScale = scaleZ;
        _edges[1].transform.localScale = scaleZ;
        _edges[2].transform.localScale = scaleZ;
        _edges[3].transform.localScale = scaleZ;

        Vector3 scaleY = new Vector3(thickX, _boxCollider.size.y / 2f, thickZ);

        _edges[4].transform.localScale = scaleY;
        _edges[5].transform.localScale = scaleY;
        _edges[6].transform.localScale = scaleY;
        _edges[7].transform.localScale = scaleY;

        Vector3 scaleX = new Vector3(thickY, _boxCollider.size.x / 2f, thickZ);

        _edges[8].transform.localScale = scaleX;
        _edges[9].transform.localScale = scaleX;
        _edges[10].transform.localScale = scaleX;
        _edges[11].transform.localScale = scaleX;
    }
}