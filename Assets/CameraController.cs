using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

public class CameraController : MonoBehaviour
{
    [FormerlySerializedAs("SeekTime")] public float seekTime = 1.0f;

    [FormerlySerializedAs("RotationRate")] public float rotationRate = 5f;

    private const float SpeedFactor = 0.01f;
    private const float CameraOrthoMinSize = 0.01f;
    private const float CameraOrthoMinDistance = 0.025f;

    public GameObject target;

    public float focalDistance = 2;
    public float ZoomDistance
    {
        get => MinOrbitDistrance();
    }

    private Vector2 _mouseDelta = new(0, 0);
    private Vector2 _mousePosition = new(0, 0);

    private bool _leftaltdown = false;
    private bool _seekmodeenabled = false;
    private bool _seeking;

    private bool _ctrlDown;
    private bool _shiftDown;

    private bool _leftMouseButtonDown;
    private bool _rightMouseButtonDown;
    private bool _middleMouseButtonDown;

    public GameObject sceneRoot;

    private Applience _currentOrbit;
    [SerializeField] private ApplienceCameraMarker[] orbits;
    public Dictionary<Applience, ApplienceCameraMarker> _orbitsDict = new();

    public Camera TargetCamera { get; private set; }

    public bool Enabled
    {
        get => enabled;
        set => enabled = value;
    }

    private void Awake()
    {
        TargetCamera = GetComponent<Camera>();
        _orbitsDict = orbits.ToDictionary(i => i.Applience, j => j);
        _currentOrbit = Applience.Lights;
    }

    private void Update()
    {
        HandleInput();
    }

  

    private float MinOrbitDistrance()
    {
        float minOrbit = float.MaxValue;

        for (int i = 0; i < orbits.Length; i++)
        {
            var distance = (orbits[i].transform.position - transform.position).magnitude;
            if (distance < minOrbit)
                minOrbit = distance;
        }

        return minOrbit;
    }

    private void HandleInput()
    {
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        _ctrlDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        _shiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        _leftaltdown = Input.GetKey(KeyCode.LeftAlt);
        _middleMouseButtonDown = Input.GetMouseButton(2);
        _rightMouseButtonDown = Input.GetMouseButton(1);

        if (Input.GetMouseButtonDown(0))
            LeftMouseButtonPressed();
        if (Input.GetMouseButtonUp(0))
            LeftMouseButtonUnpressed();

        MousePositionPerformed(Input.mousePosition);

        if ((Input.GetKeyDown(KeyCode.Tab)))
            SwitchBetweenOriginInternal(Applience.Lights);

        Scroll(Input.mouseScrollDelta);
    }

    public void SwitchBetweenOrigins(Applience app)
    {
        SwitchBetweenOriginInternal(app);
    }
    public Tween SwitchBetweenOriginInternal(Applience app, float duration = 1)
    {
        _currentOrbit = app;
        target = _orbitsDict[app].gameObject;
        Vector3 delta = Vector3.up * 3;
        var sequence = DOTween.Sequence();
        Tween tw1 = transform.DOMove(target.transform.position + delta, duration);
        Tween tw2 = transform.DORotateQuaternion(target.transform.rotation, duration);
        sequence.Append(tw1);
        sequence.Join(tw2);
        return sequence;
    }


    public void EnableCameraControl()
    {
        if (Enabled)
            return;
        Enabled = true;
    }

    public void DisableCameraControl()
    {
        if (!Enabled)
            return;
        Enabled = false;
    }

    public void SwitchSeekMouseMode()
    {
        _seekmodeenabled = !_seekmodeenabled;
    }

    private void LeftMouseButtonPressed()
    {
        _leftMouseButtonDown = true;
        LeftMouseButton();
    }

    private void LeftMouseButtonUnpressed()
    {
        _leftMouseButtonDown = false;
    }

    private void MousePositionPerformed(Vector2 mPos)
    {
        var position = mPos;
        MouseDelta(_mousePosition - position);
        _mousePosition.x = position.x;
        _mousePosition.y = position.y;
    }

    private void MouseDelta(Vector2 mPos)
    {
        _mouseDelta = mPos;

        Vector3 p0 = new Vector3(_mousePosition.x, _mousePosition.y, focalDistance);
        Vector3 p1 = new Vector3(_mousePosition.x + _mouseDelta.x, _mousePosition.y + _mouseDelta.y, focalDistance);

        Vector3 move = (TargetCamera.ScreenToWorldPoint(p0) - TargetCamera.ScreenToWorldPoint(p1)) / 2.0f;

        if (_leftMouseButtonDown && _middleMouseButtonDown)
        {
            Zoom(move);
            return;
        }

        if (_middleMouseButtonDown
          || (_ctrlDown && _leftMouseButtonDown)
          || (_shiftDown && _leftMouseButtonDown))
        {
            //Pan(move);
            return;
        }

        if (_leftMouseButtonDown && !TargetCamera.orthographic)
        {
            OrbitTurnTable(p0, p1);
        }
    }

    private void LeftMouseButton()
    {
        if (_seekmodeenabled)
        {
            Seek(new Vector3(_mousePosition.x, _mousePosition.y, 0));
            _seekmodeenabled = false;

            return;
        }
    }
    private IEnumerator LerpTowards(Transform to, float duration, Action onAnimEnded = null)
    {
        float counter = 0;
        Vector3 frompos = transform.position;
        Quaternion fromrot = transform.rotation;

        while (counter < duration)
        {
            counter += Time.deltaTime;
            float t = counter / duration;
            transform.position = Vector3.Lerp(frompos, to.position, t);
            transform.rotation = Quaternion.Lerp(fromrot, to.rotation, t);
            yield return null;
        }

        onAnimEnded?.Invoke();
    }

    private void Scroll(Vector2 delta)
    {
        _mouseDelta = delta;
        if (Math.Abs(_mouseDelta.magnitude) < 0.0001f)
            return;


        // linux returns 1 mousewheel units, scale to universal 120 mousewheel units
        if (Math.Abs(_mouseDelta.x) <= 1f) _mouseDelta.x *= 120f;
        if (Math.Abs(_mouseDelta.y) <= 1f) _mouseDelta.y *= 120f;

        Vector3 move = new Vector3(0, _mouseDelta.y / 100.0f, 0);
        Zoom(move);
    }


    public void ViewAll()
    {
        if (TargetCamera.orthographic)
        {
            ViewAllOrthographic();
        }
        else
        {
            ViewAllPerspective();
        }
    }

    public Bounds GetSceneBounds()
    {
        Bounds bounds = new Bounds();

        if (sceneRoot != null)
        {
            var b = new Bounds(sceneRoot.transform.position, Vector3.zero);
            foreach (Renderer r in sceneRoot.GetComponentsInChildren<Renderer>())
            {
                b.Encapsulate(r.bounds);
            }
            bounds.Encapsulate(b);
        }
        return bounds;
    }

    public static float GetFitObjectToCameraDistance(Bounds bounds, Camera cam)
    {
        const float margin = 1.1f;
        float objectSize = bounds.extents.magnitude;
        float fieldOfView = cam.orthographic ? 1.0f : Mathf.Tan(0.5f * Mathf.Deg2Rad * cam.fieldOfView);
        float distance = objectSize * margin / fieldOfView;
        return distance;
    }

    public void ViewAllPerspective()
    {
        var bounds = GetSceneBounds();
        focalDistance = GetFitObjectToCameraDistance(bounds, TargetCamera);
        transform.position = bounds.center - transform.forward.normalized * focalDistance;
    }

    public void ViewAllOrthographic()
    {
        ViewAllPerspective();
        TargetCamera.orthographicSize = focalDistance;
    }

    private void AKeyPressed()
    {
        SetProjectionMode(!TargetCamera.orthographic);
        ViewAll();
    }


    public void SetProjectionMode(bool orthographic)
    {
        if (!orthographic)
            SetPerspectiveProjection();
        else
            SetOrthographicProjection(sceneRoot);
    }

    public bool GetProjectionMode()
    {
        return TargetCamera.orthographic;
    }

    public void SetCameraOrthographic(bool orthographic)
    {
        TargetCamera.orthographic = orthographic;
        if (orthographic)
        {
            var orthographicSize = focalDistance * Mathf.Tan(TargetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            TargetCamera.orthographicSize = orthographicSize;
        }
    }

    public void SetOrthographicProjection(GameObject targetObject)
    {
        SetOrthographicProjection(targetObject, targetObject.transform.position);
    }

    public void SetOrthographicProjection(GameObject targetObject, Vector3 targetPoint)
    {
        var sign = Vector3.Dot(targetObject.transform.forward, TargetCamera.transform.forward);
        sign = sign >= 0 ? 1 : -1;

        target.transform.LookAt(targetPoint, Vector3.forward);

        SetCameraOrthographic(true);
        target.transform.position = targetPoint - sign * targetObject.transform.forward * focalDistance;
    }

    public void SetPerspectiveProjection()
    {
        SetCameraOrthographic(false);
        var distance = TargetCamera.orthographicSize / Mathf.Tan(TargetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);

        transform.Translate(new Vector3(0, 0, CameraOrthoMinDistance - distance));
        focalDistance = distance;

    }

    private void Shift(Vector3 shift)
    {
        transform.position += shift;
    }

    public void Seek(Vector3 screenPos)
    {
        RaycastHit hit;
        Ray ray = TargetCamera.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            GameObject obj = hit.collider.gameObject;
            Renderer renderer = obj.GetComponent<Renderer>();
            Bounds bounds = renderer.bounds;

            target.transform.position = transform.position;
            target.transform.LookAt(hit.point);

            focalDistance = (hit.point - transform.position).magnitude;
            if (focalDistance > bounds.size.magnitude)
            {
                focalDistance = bounds.size.magnitude;
            }
            focalDistance *= 0.5f;
            target.transform.position = hit.point - target.transform.forward.normalized * focalDistance;

            _seeking = true;
            StartCoroutine(LerpTowards(target.transform, seekTime, () =>
            {
                _seeking = false;
                _seekmodeenabled = false;
            }));

        }
    }

    public void OrbitTurnTable(Vector3 p0, Vector3 p1)
    {
        Vector3 previousPosition = TargetCamera.ScreenToViewportPoint(p0);
        Vector3 newPosition = TargetCamera.ScreenToViewportPoint(p1);
        Vector3 direction = previousPosition - newPosition;
        direction.y *= Math.Abs(direction.x) > Math.Abs(direction.y) ? 0 : 1;
        direction.x *= Math.Abs(direction.x) > Math.Abs(direction.y) ? 1 : 0;


        float rotationAroundYAxis = -direction.x * 180; // camera moves horizontally
        float rotationAroundXAxis = direction.y * 180 * 0; // camera moves vertically
        Transform target = _orbitsDict[_currentOrbit].transform;
        //  transform.position = target;

        Quaternion oldRotation = transform.rotation;
        transform.Rotate(Vector3.right, rotationAroundXAxis);

        transform.RotateAround(target.position, Vector3.up, rotationAroundYAxis);

        if (transform.rotation.eulerAngles.x < 12 || transform.rotation.eulerAngles.x > 70)
            transform.rotation = oldRotation;

        if (transform.rotation.eulerAngles.y < 160 || transform.rotation.eulerAngles.y > 270)
            transform.rotation = oldRotation;


        // transform.position = target - transform.forward.normalized * focalDistance;
    }


    private void Pan(Vector3 move)
    {
        float panMultiplier = 3;
        transform.position += move * panMultiplier;
    }

    public void Zoom(Vector3 move)
    {
        float zoomDirection = move.y > 0 ? -1.0f : 1.0f;
        if (focalDistance > 35 && zoomDirection < 0)
            return;
        if (focalDistance < 3 && zoomDirection > 0)
            return;

        if (TargetCamera.orthographic)
        {
            var size = TargetCamera.orthographicSize;
            float linearZoom = LogarithmicZoom(1.0f, size);
            size += zoomDirection * linearZoom;
            TargetCamera.orthographicSize = Mathf.Max(CameraOrthoMinSize, size);
        }
        else
        {
            float zoomDistance = focalDistance;
            var screenPos = new Vector3(_mousePosition.x, _mousePosition.y, 0);
            RaycastHit hit;
            Ray ray = TargetCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out hit))
            {
                GameObject obj = hit.collider.gameObject;
                Renderer renderer = obj.GetComponent<Renderer>();
                Bounds bounds = renderer.bounds;

                zoomDistance = (hit.point - transform.position).magnitude;
            }

            Vector3 target = transform.position + transform.forward.normalized * zoomDistance;


            float linearZoom = LogarithmicZoom(1.0f, zoomDistance * zoomDistance);
            transform.Translate(new Vector3(0, 0, zoomDirection * linearZoom));
            focalDistance = (transform.position - target).magnitude;
        }
    }

    private float LogarithmicZoom(float min, float max)
    {

        const float minZoomStep = 0.005f;
        const float step = 1.0f;
        const float maxSteps = 10.0f;
        const float logPositiveOffset = 1.0f;

        float logMinZoom = Mathf.Log(min);
        float logMaxZoom = Mathf.Log(max + logPositiveOffset);
        float logZoom = logMinZoom + (logMaxZoom - logMinZoom) * step / (maxSteps);
        float linearZoom = Mathf.Max(Mathf.Exp(logZoom) - logPositiveOffset, minZoomStep);
        return linearZoom;
    }
}
