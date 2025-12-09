using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
public enum GizmoMode
{
    None,
    Translate,
    Rotate,
    Scale
}


public class RuntimeGizmoTransform : MonoBehaviour
{
    //called when GizmoMode change
    //used by dropdown Menu
    public Action<GizmoMode> OnModeChanged;
    GizmoMode currentMode = GizmoMode.None;
    public GizmoMode GizmoMode
    {
        get { return currentMode; }
        set
        {
            if (value == GizmoMode.Scale)
            {
                LocalTranform = true;
            }
            currentMode = value;
            OnModeChanged?.Invoke(currentMode);
        }
    }

    private bool localTranform = true;
    private Dictionary<GizmoMode, Mesh> meshes;

    public bool LocalTranform
    {
        set
        {
            localTranform = value;

            if(GizmoMode == GizmoMode.Scale)
            {
                LocalTranform = true;
            }

            //adapt gizmo to current mode
            //by destroing
            DestroyAllHandles();
            //and recreating the handles
            CreateHandles(meshes[currentMode]);
        }
        get { return localTranform; }
    }

    //INPUT 
    private InputActionMap gizmoActionMap;
    private InputAction selectAction;
    private InputAction mousePosAction;
    private InputAction enableCameraAction;
    private InputAction snapAction;
    private InputAction deleteAction;

    private Camera cam;
    private FreeCameraController fCam;
    private Transform xHandle, yHandle, zHandle;
    private Transform uniformScaleHandle;
    private Transform currentHandle;

    private Vector2 lastMousePos;

    private int mouseWrapMarginX;
    private int mouseWrapMarginY;

    public static readonly string colliderVisualName = "ColliderVisual";

    // Reusable array to avoid multiple allocations
    //32 should be enough for the number of elements that can be hit at once
    private static readonly RaycastHit[] raycastHits = new RaycastHit[32];

    //SNAPTOOL
    Vector3 hitNormal;
    Vector3 hitPoint;
    private readonly float minDistanceToSnap = .2f;
    private readonly float minAngleToSnap = 15.0f;
    private readonly int ignoreRaycastLayer = 2;
    //check if there was a translation to avoid useless snap detections
    private bool translationThisFrame = false;
    private List<BoxCollider> snappedObjectColliders;

    private void Start()
    {
        mouseWrapMarginX = Screen.width / 200;
        mouseWrapMarginY = Screen.height / 200;

        // Input Actions
        gizmoActionMap = InputSystem.actions.FindActionMap("Gizmo");
        selectAction = gizmoActionMap.FindAction("SelectTarget");
        mousePosAction = gizmoActionMap.FindAction("MousePosition");
        enableCameraAction = gizmoActionMap.FindAction("EnableCamera");
        snapAction = gizmoActionMap.FindAction("TranslationSnapping");
        deleteAction = gizmoActionMap.FindAction("Delete");

        //Loading meshes
        meshes = new()
        {
            { GizmoMode.None, null },
            { GizmoMode.Translate, Resources.Load<Mesh>("Handles/Translate") },
            { GizmoMode.Rotate, Resources.Load<Mesh>("Handles/Rotation") },
            { GizmoMode.Scale, Resources.Load<Mesh>("Handles/Scale") }
        };

        // Camera
        cam = Camera.main;
        fCam = cam.GetComponent<FreeCameraController>();

        CreateHandles(null);

        snappedObjectColliders = new List<BoxCollider>();
        ShowBoxCollider(GetComponent<BoxCollider>());
    }

    private void Update()
    {

        ScaleHandlesByCameraDistance();

        if(deleteAction.WasPressedThisFrame())
        {
            SelectionManager selectionManager = FindAnyObjectByType<SelectionManager>();
            selectionManager.DeleteSelected();
            return;
        }

        if (enableCameraAction.IsInProgress())
        {
            fCam.enabled = true;
            return;
        }
        if (enableCameraAction.WasReleasedThisFrame())
        {
            fCam.enabled = false;
        }

        if (selectAction.WasPressedThisFrame() && currentMode != GizmoMode.None)
        {
            Vector2 mouseScreenPos = mousePosAction.ReadValue<Vector2>();
            Ray ray = cam.ScreenPointToRay(mouseScreenPos);
            Physics.RaycastNonAlloc(ray, raycastHits);

            RaycastHit hit = new();
            bool foundHit = true;
            // get closest hit that is one of the handles or null
            try
            {
                hit = raycastHits
                .OrderBy(h => h.distance)
                .First(h =>
                h.transform == xHandle ||
                h.transform == yHandle ||
                h.transform == zHandle ||
                (uniformScaleHandle != null && h.transform == uniformScaleHandle)
                );            
            }
            catch
            {
                foundHit = false;
            }


            Array.Clear(raycastHits, 0, raycastHits.Length);

            if (foundHit)
            {
                currentHandle = hit.transform;
                HideNonSelectedHandle();
                lastMousePos = mousePosAction.ReadValue<Vector2>();
            }
        }


        if (selectAction.IsPressed() && currentHandle != null && currentMode != GizmoMode.None)
        {
            Vector2 mousePos = mousePosAction.ReadValue<Vector2>();
            var (warped, newPos) = WarpMouse(mousePos);

            if (warped)
            {
                mousePos = newPos;
                lastMousePos = newPos;
            }

            Vector2 delta = mousePos - lastMousePos;

            Vector3 screenP0 = cam.WorldToScreenPoint(transform.position);
            Vector3 screenP1 = cam.WorldToScreenPoint(transform.position + currentHandle.up);
            //screen-space direction of the axis
            Vector2 axisScreen = (screenP1 - screenP0);

            //handles that in ortho alligned pefectly with view so need different handling in ortho
            bool specialHandle = currentHandle == yHandle || currentHandle == uniformScaleHandle;

            if (axisScreen.sqrMagnitude > 0.0001f || (cam.orthographic && specialHandle))
            {
                //scalar amount of movement along the axis.
                float projected = Vector2.Dot(delta, axisScreen.normalized);
                //Make movement in world space proportional to distance from camera
                float distance = (transform.position - cam.transform.position).magnitude;
                float worldScale = distance * 0.001f;

                if (cam.orthographic && specialHandle)
                {
                    projected = Vector2.Distance(mousePos, lastMousePos);
                    worldScale = cam.orthographicSize * 0.001f;

                    var mouseWC = cam.WorldToScreenPoint(mousePos);
                    mouseWC.y = transform.position.y;
                    var lastMouseWC = cam.WorldToScreenPoint(lastMousePos);
                    lastMouseWC.y = transform.position.y;

                    if (Vector3.Distance(mouseWC, transform.position) < Vector3.Distance(lastMouseWC, transform.position))
                        projected *= -1;
                }

                //TRANSLATION
                //--------------------------------
                if (currentMode == GizmoMode.Translate)
                {
                    var translation = projected * worldScale * currentHandle.up;
                    transform.Translate(translation, Space.World);
                    if (translation.magnitude > 0.0001f)
                    {
                        translationThisFrame = true;
                        gameObject.TryGetComponent(out BoxCollider ObjectBC);
                        snappedObjectColliders.RemoveAll(bc => bc == null);
                        snappedObjectColliders.RemoveAll(bc =>
                        {
                            Vector3 closestPoint = bc.ClosestPoint(transform.position);
                            float colliderSize = GetColliderSizeAlongNormal(
                                ObjectBC,
                                Vector3.Normalize(closestPoint - transform.position)
                            );
                            bool remove = Vector3.Distance(closestPoint, transform.position) > colliderSize + minDistanceToSnap;
                            return remove;
                        });

                    }

                    //change gizmoActionMap positions to match
                    xHandle.localPosition = transform.position;
                    yHandle.localPosition = transform.position;
                    zHandle.localPosition = transform.position;
                }
                //ROTATION
                //-------------------------------
                if (currentMode == GizmoMode.Rotate)
                {
                    float angle = projected * worldScale;
                    float rotSpeed = 20f;
                    Quaternion deltaRotation = Quaternion.AngleAxis(rotSpeed * angle, currentHandle.up);
                    transform.rotation = deltaRotation * transform.rotation;

                    //clear snapped objects
                    snappedObjectColliders.Clear();
                }
                //SCALE
                //-------------------------------
                if (currentMode == GizmoMode.Scale)
                {
                    //local scale 
                    float scaleAmount = projected * worldScale * 0.1f;
                    Vector3 direction = new();

                    if(currentHandle == uniformScaleHandle)
                    {
                        float factor = 1 + scaleAmount;       
                        transform.localScale *= Mathf.Abs(factor);
                    }
                    else
                    {
                        if (currentHandle.name.Contains("X", StringComparison.OrdinalIgnoreCase))
                            direction = new(1, 0, 0);
                        else if (currentHandle.name.Contains("Y", StringComparison.OrdinalIgnoreCase))
                            direction = new(0, 1, 0);
                        else if (currentHandle.name.Contains("Z", StringComparison.OrdinalIgnoreCase))
                            direction = new(0, 0, 1);

                        var newScale = transform.localScale + scaleAmount * direction;
                        transform.localScale = newScale.Abs();
                    }
                }

                //update ui
                FindAnyObjectByType<SelectionManager>().UpdateTransformBox();
            }

            // Warp cursor to wrapped position
            if (warped)
                Mouse.current.WarpCursorPosition(mousePos + new Vector2(mouseWrapMarginX, mouseWrapMarginY));

            // Update last
            lastMousePos = mousePos;
        }

        //SNAP LOGIC
        //-------------------------------------
        if (snapAction.IsInProgress() &&
            translationThisFrame && 
            selectAction.IsPressed() && 
            currentHandle != null && 
            currentMode == GizmoMode.Translate)
        {
            //set objs in a layer where they are excluded from the raycast 
            SetLayerRecursively(gameObject, ignoreRaycastLayer);
            xHandle.gameObject.layer = ignoreRaycastLayer;
            yHandle.gameObject.layer = ignoreRaycastLayer;
            zHandle.gameObject.layer = ignoreRaycastLayer;

            //WORKS ONLY FOR GameObjects WITH A BOXCOLLIDER
            if (!gameObject.TryGetComponent<BoxCollider>(out var bc))
            {
                Debug.LogWarning("SnapTool: No BoxCollider found");
                return;
            }

            if (bc != null)
            {
                Debug.Log("box not null");

                Vector3[] normals = GetBoxNormals(bc);
                RaycastHit bestHit = new();
                bool first = true;
                bool hitFound = false;

                //for each face of the box collider a ray is created
                for (int i = 0; i < normals.Length; i++)
                {
                    Ray ray = new(
                            bc.transform.TransformPoint(bc.center),
                            normals[i]
                        );

                    Physics.Raycast(ray, out RaycastHit hit);
                    //Find best face to snap by the distance from the object 

                    if (hit.collider == null)
                        continue;

                    //skips hits with object it's currently already snapping
                    if (hit.collider is BoxCollider && snappedObjectColliders.Contains(hit.collider))
                        continue;

                    if (first)
                    {
                        hitFound = true;
                        first = false;
                        bestHit = hit;
                    }
                    else if (bestHit.distance > hit.distance)
                    {
                        bestHit = hit;
                    }

                }

                //SNAP is possible 
                if (hitFound && bestHit.collider != null && bestHit.distance < minDistanceToSnap + bc.size.MaxComponent()) 
                {
                    hitNormal = bestHit.normal;
                    hitPoint = bestHit.point;

                    Vector3 bestMatchNormal = Vector3.zero;
                    float bestDot = -2f;

                    // Find best normal by highest dot product because normals are in the opposite direction
                    foreach (var normal in normals)
                    {
                        float dot = Vector3.Dot(normal, -hitNormal);

                        if (dot > bestDot)
                        {
                            bestDot = dot;
                            bestMatchNormal = normal;
                        }
                    }

                    float angle = Vector3.Angle(hitNormal, -bestMatchNormal);

                    //execute the snap
                    if (angle < minAngleToSnap)
                    {
                        //Size of the object along the best match normal
                        float sizeAlongNormal = GetColliderSizeAlongNormal(bc, bestMatchNormal);

                        //SNAP found
                        if (bestHit.distance < minDistanceToSnap + sizeAlongNormal)
                        {
                            snappedObjectColliders.Add((BoxCollider)bestHit.collider);

                            //if anchor is not in the Box collider center
                            Quaternion rotationDelta = Quaternion.FromToRotation(bestMatchNormal, -hitNormal);
                            Quaternion finalRotation = rotationDelta * transform.rotation;
                            Vector3 pivotToCenter = bc.transform.TransformPoint(bc.center) - transform.position;
                            Vector3 rotatedPivotToCenter = rotationDelta * pivotToCenter;
                            Vector3 targetCenterPosition = hitPoint + (hitNormal * sizeAlongNormal);
                            Vector3 finalPosition = targetCenterPosition - rotatedPivotToCenter;

                            // Apply transformations
                            transform.SetPositionAndRotation(finalPosition, finalRotation);

                            // remove handle control
                            currentHandle = null;
                            CreateHandles(meshes[GizmoMode.Translate]);
                            ShowAllHandles();
                        }     
                    }
                }

                //set layers back to default
                SetLayerRecursively(gameObject, 0);
                xHandle.gameObject.layer = 0;
                yHandle.gameObject.layer = 0;
                zHandle.gameObject.layer = 0;
            }
        }

        if (selectAction.WasReleasedThisFrame())
        {
            //recreate the handles to match the object new local rotatation
            if (currentMode == GizmoMode.Rotate && localTranform)
            {
                CreateHandles(meshes[currentMode]);
            }

            currentHandle = null;
            ShowAllHandles();
        }

        translationThisFrame = false;
    }

    //HANDLES FUNCTIONS
    //----------------------------------
    public void ResetHandles()
    {
        DestroyAllHandles();
        CreateHandles(meshes[currentMode]);
    }

    private void CreateHandles(Mesh mesh)
    {

        DestroyAllHandles();

        if (mesh == null)
        {
            if (currentMode != GizmoMode.None)
                Debug.LogError("Mesh is null");
            return;
        }

        // Create handles
        xHandle = CreateHandle(mesh, Resources.Load<Material>("Materials/Red_AlwaysOnTop"), Vector3.right);
        yHandle = CreateHandle(mesh, Resources.Load<Material>("Materials/Green_AlwaysOnTop"), Vector3.up);
        zHandle = CreateHandle(mesh, Resources.Load<Material>("Materials/Blue_AlwaysOnTop"), Vector3.forward);

        //create uniform scale handle 
        uniformScaleHandle = null;
        if(GizmoMode == GizmoMode.Scale)
        {
            var cube  = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.AddComponent<CapsuleCollider>().isTrigger = true;
            cube.name = "Handle_uniform";
            cube.transform.localPosition = transform.position;
            cube.GetComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/Magenta_AlwaysOnTop");
            uniformScaleHandle = cube.transform;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="aotMaterial">Always On Top Material (From custom shader)</param>
    /// <param name="direction"></param>
    /// <returns></returns>
    private Transform CreateHandle(Mesh mesh, Material aotMaterial, Vector3 direction)
    {
        string dirName = "";
        if (direction.x > 0) dirName = "X";
        else if (direction.y > 0) dirName = "Y";
        else if (direction.z > 0) dirName = "Z";

        string name = "Handle_" + currentMode.ToString() + "_" + dirName;


        GameObject go = new(name);

        go.AddComponent<MeshFilter>().mesh = mesh;
        go.AddComponent<MeshRenderer>().material = aotMaterial;
        if (currentMode == GizmoMode.Rotate)
        {
            AddTourusCollider(go, 16, .08f);
        }
        else
        {
            go.AddComponent<CapsuleCollider>().isTrigger = true;
        }

        if (localTranform)
        {
            direction = transform.localRotation * direction;
        }

        go.transform.up = direction;
        go.transform.localPosition = transform.position;

        return go.transform;
    }

    private void AddTourusCollider(GameObject target, int segments, float capsuleRadius, float torousRadius = 1f)
    {
        float dAngle = Mathf.PI * 2 / segments;
        int stepN = segments / 4;
        int stepLeft = (int)Math.Ceiling((double)stepN / 2); ;

        int currentDir = 2;

        for (int i = 0; i < segments; i++)
        {
            var cc = target.AddComponent<CapsuleCollider>();
            cc.radius = capsuleRadius;
            cc.height = dAngle * torousRadius;
            cc.isTrigger = true;

            cc.direction = currentDir;
            if (--stepLeft == 0)
            {
                stepLeft = stepN;
                currentDir = (currentDir == 0) ? 2 : 0;
            }
            cc.center = new(torousRadius * Mathf.Cos(dAngle * i), 0f, torousRadius * Mathf.Sin(dAngle * i));
        }
    }

    private void DestroyAllHandles()
    {
        if (xHandle != null)
            Destroy(xHandle.gameObject);
        if (yHandle != null)
            Destroy(yHandle.gameObject);
        if (zHandle != null)
            Destroy(zHandle.gameObject);
        if(uniformScaleHandle != null)
            Destroy(uniformScaleHandle.gameObject); 
    }

    void HideNonSelectedHandle()
    {
        if (xHandle == null || yHandle == null || zHandle == null) return;

        xHandle.gameObject.SetActive(currentHandle == xHandle);
        yHandle.gameObject.SetActive(currentHandle == yHandle);
        zHandle.gameObject.SetActive(currentHandle == zHandle);

        if(uniformScaleHandle != null)
            uniformScaleHandle.gameObject.SetActive(currentHandle == uniformScaleHandle);

    }

    void ShowAllHandles()
    {
        if (xHandle == null || yHandle == null || zHandle == null) return;

        xHandle.gameObject.SetActive(true);
        yHandle.gameObject.SetActive(true);
        zHandle.gameObject.SetActive(true);

        if(uniformScaleHandle != null)
            uniformScaleHandle.gameObject.SetActive(true);
    }

    void ScaleHandlesByCameraDistance()
    {
        float scaleFactor;
        if (fCam.Ortho)
        {
            float size = cam.orthographicSize;
            scaleFactor = size / 4;
        }
        else
        {
            float distance = Vector3.Distance(transform.position, fCam.transform.position);
            scaleFactor = distance / 8;
        }
        if (xHandle != null)
            xHandle.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
        if (yHandle != null)
            yHandle.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
        if (zHandle != null)
            zHandle.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
        if (uniformScaleHandle != null)
        {
            scaleFactor *= 0.4f;
            uniformScaleHandle.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
        }
    }

    //----------------------------------------------------------
    //END HANDLE FUNCTIONS

    /// <summary>
    /// Move mouse position to opposite side of screen if it goes out of bounds
    /// </summary>
    /// <param name="pos">currentPos</param>
    /// <returns></returns>
    private (bool, Vector2) WarpMouse(Vector2 pos)
    {
        bool warped = false;
        float x = pos.x;
        float y = pos.y;

        if (x < 0 + mouseWrapMarginX)
        {
            x = Screen.width - 1;
            warped = true;
        }
        else if (x > Screen.width - mouseWrapMarginX)
        {
            x = 1;
            warped = true;
        }
        if (y < 0 + mouseWrapMarginY)
        {
            y = Screen.height - 1;
            warped = true;
        }
        else if (y > Screen.height - mouseWrapMarginY)
        {
            y = 1;
            warped = true;
        }
        return (warped, new Vector2(x, y));
    }


    public void SetMode(GizmoMode newMode)
    {
        if (newMode == currentMode) return;
        GizmoMode = newMode;
        CreateHandles(meshes[currentMode]);
        if (currentMode == GizmoMode.None)
        {
            gizmoActionMap.Disable();
            enableCameraAction.Enable();
            deleteAction.Enable();
        }
        else
            gizmoActionMap.Enable();
        ShowAllHandles();
    }

    //SNAP FUNCTIONS
    //-------------------------
    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;

        for (int i = 0; i < obj.transform.childCount; i++)
        {
            SetLayerRecursively(obj.transform.GetChild(i).gameObject, layer);
        }
    }

    Vector3[] GetBoxNormals(BoxCollider box)
    {
        Vector3[] normals = new Vector3[6];

        // local axes
        normals[0] = box.transform.right;    // +X
        normals[1] = -box.transform.right;   // -X
        normals[2] = box.transform.up;       // +Y
        normals[3] = -box.transform.up;      // -Y
        normals[4] = box.transform.forward;  // +Z
        normals[5] = -box.transform.forward; // -Z

        return normals;
    }

    private float GetColliderSizeAlongNormal(BoxCollider bc, Vector3 normal)
    {
        //translation distance
        //get local normal
        Vector3 localNormal = transform.InverseTransformDirection(normal);

        //Compute the (world space) half size of the box
        Vector3 worldHalfSize = Vector3.Scale(bc.size, transform.lossyScale) * 0.5f;

        return Mathf.Abs(localNormal.x * worldHalfSize.x) +
               Mathf.Abs(localNormal.y * worldHalfSize.y) +
               Mathf.Abs(localNormal.z * worldHalfSize.z);
    }
    //------------------------------------------
    //END SNAP FUNCTIONS

    int cnt;

    public void ShowBoxCollider(BoxCollider bc)
    {
        if (bc == null)
        {
            Debug.LogWarning("ShowBoxCollider: collider not found for go: " + gameObject.name);
            return;
        }

        cnt = SelectionManager.Cnt;

        // Create the visual GameObject
        GameObject go = new(colliderVisualName);

        go.transform.SetParent(bc.transform, false);

        LineRenderer lr = go.AddComponent<LineRenderer>();


        lr.material = Resources.Load<Material>("Materials/ColliderVisualMat");

        lr.loop = false;
        lr.useWorldSpace = false;

        Vector3 c = bc.center;
        Vector3 s = bc.size * 0.5f;

        // Define the 8 corners in Local Space
        Vector3[] corners = new Vector3[8]
        {
            c + new Vector3(-s.x, -s.y, -s.z), // 0: Bottom-Front-Left
            c + new Vector3( s.x, -s.y, -s.z), // 1: Bottom-Front-Right
            c + new Vector3( s.x, -s.y,  s.z), // 2: Bottom-Back-Right
            c + new Vector3(-s.x, -s.y,  s.z), // 3: Bottom-Back-Left
            c + new Vector3(-s.x,  s.y, -s.z), // 4: Top-Front-Left
            c + new Vector3( s.x,  s.y, -s.z), // 5: Top-Front-Right
            c + new Vector3( s.x,  s.y,  s.z), // 6: Top-Back-Right
            c + new Vector3(-s.x,  s.y,  s.z)  // 7: Top-Back-Left
        };

        Vector3[] linePoints = new Vector3[]
        {
            //bottom
            corners[0], corners[1], corners[2], corners[3], corners[0],            
            corners[4],  
            //top
            corners[5], corners[6], corners[7], corners[4],
            corners[5],
            corners[1],         
            corners[2],
            corners[6],
            corners[7],
            corners[3]
        };

        lr.positionCount = linePoints.Length;
        lr.SetPositions(linePoints);
        lr.enabled = true;

        lr.widthMultiplier = 0f;
        lr.AddComponent<ColliderVisualWidthHandler>();
    }


    void OnDestroy()
    {  
        SetLayerRecursively(gameObject, 0);
        DestroyAllHandles();
    }
}
