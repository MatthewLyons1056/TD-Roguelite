using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening; // Import DOTween

public class CameraControls : MonoBehaviour
{
    [TabGroup("CameraSystem", "Position", false, 2)] public bool canPan = true;
    [TabGroup("CameraSystem", "Position", false, 2)] public float panSpeed = 5f;
    [TabGroup("CameraSystem", "Position", false, 2)] public float panBorderThickness = 10f;
    [TabGroup("CameraSystem", "Position", false, 2)] public bool canDragCamera = true;
    [TabGroup("CameraSystem", "Position", false, 2)] public float dragPanSpeed = .5f;
    [TabGroup("CameraSystem", "Position", false, 2)] public bool arrowKeysController = true;
    [TabGroup("CameraSystem", "Position", false, 2)] public float keyControlSpeed = 5f; // New key control speed modifier

    [TabGroup("CameraSystem", "Zoom", false, 2), SerializeField] private bool canZoom = true; // Zoom speed
    [TabGroup("CameraSystem", "Zoom", false, 2), SerializeField] private float zoomSpeed = 5f; // Zoom speed
    [TabGroup("CameraSystem", "Zoom", false, 2), SerializeField] private float minZoom = 5f; // Minimum zoom distance
    [TabGroup("CameraSystem", "Zoom", false, 2), SerializeField] private float maxZoom = 20f; // Maximum zoom distance

    [TabGroup("CameraSystem", "Bounds", false, 2), SerializeField] private Vector2 boundsSize = new Vector2(50f, 50f); // Bounds size as Vector2
    [TabGroup("CameraSystem", "Bounds", false, 2), SerializeField] private Transform CenterOfMap; // Reference to the center of the map

    public Transform cameraTransform;
    public Transform referenceObject;

    private bool dragPanMoveActive;
    private float dragWaitTimer = .1f, dragtime;
    private Vector2 lastMousePosition;
    private const float targetFrameRate = 60f; // Target frame rate for normalization
    private Vector3 initialCameraPosition;

    void Start()
    {
        // Set the initial camera position based on the CenterOfMap position
        if (CenterOfMap != null)
        {
            initialCameraPosition = CenterOfMap.position;
        }
        else
        {
            initialCameraPosition = cameraTransform.position;
            Debug.LogWarning("CenterOfMap is not assigned. Using camera's initial position as the bounds center.");
        }
    }

    void Update()
    {
        HandleMouseInput();
        HandleKeyboardInput();
        HandleZoomInput();
    }

    void LateUpdate()
    {
        if (dragPanMoveActive && canDragCamera && StateMachine.Instance.CurrentState != StateMachine.GameState.Paused && StateMachine.Instance.CurrentState != StateMachine.GameState.Cinematic && StateMachine.Instance.CurrentState != StateMachine.GameState.BuildMode)
        {
            dragtime += Time.deltaTime;
            if (dragtime > dragWaitTimer)
            {
                DragCamera();
            }
        }
        else
        {
            dragtime = 0;
        }

        MousePanController();
    }

    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(2))
        {
            dragPanMoveActive = true;
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(2))
        {
            dragPanMoveActive = false;
        }
    }

    void HandleKeyboardInput()
    {
        if (arrowKeysController && StateMachine.Instance.CurrentState != StateMachine.GameState.Paused && StateMachine.Instance.CurrentState != StateMachine.GameState.Console)
        {
            Vector3 direction = Vector3.zero;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                direction += referenceObject.forward;
            }
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                direction -= referenceObject.forward;
            }
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                direction -= referenceObject.right;
            }
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                direction += referenceObject.right;
            }

            if (direction != Vector3.zero)
            {
                Pan(direction.normalized * keyControlSpeed);
            }
        }
    }

    void HandleZoomInput()
    {
        if (canZoom)
        {
            if (StateMachine.Instance.CurrentState != StateMachine.GameState.Paused && StateMachine.Instance.CurrentState != StateMachine.GameState.Console && StateMachine.Instance.CurrentState != StateMachine.GameState.Cinematic)
            {
                float scroll = Input.mouseScrollDelta.y;
                if (scroll != 0f)
                {
                    Zoom(-scroll);
                }
            }
        }
    }

    void DragCamera()
    {
        Vector2 mouseMovementDelta = (Vector2)Input.mousePosition - lastMousePosition;
        Quaternion referenceRotation = Quaternion.Euler(0, referenceObject.eulerAngles.y, 0);
        Vector3 dragPanDirection = referenceRotation * new Vector3(mouseMovementDelta.x, 0, mouseMovementDelta.y);

        Vector3 targetPosition = cameraTransform.position - (transform.forward * dragPanDirection.z + transform.right * dragPanDirection.x) * dragPanSpeed;
        targetPosition = ClampToBounds(targetPosition);
        cameraTransform.DOMove(targetPosition, 0.2f).SetUpdate(true); // Using DOTween to move smoothly
        lastMousePosition = Input.mousePosition;
    }

    void MousePanController()
    {
        if (canPan && !Input.GetMouseButton(1))
        {
            Vector3 direction = Vector3.zero;

            if (Input.mousePosition.x >= Screen.width - panBorderThickness)
            {
                direction += referenceObject.right;
            }
            else if (Input.mousePosition.x <= panBorderThickness)
            {
                direction -= referenceObject.right;
            }

            if (Input.mousePosition.y >= Screen.height - panBorderThickness)
            {
                direction += referenceObject.forward;
            }
            else if (Input.mousePosition.y <= panBorderThickness)
            {
                direction -= referenceObject.forward;
            }

            if (direction != Vector3.zero)
            {
                Pan(direction.normalized);
            }
        }
    }

    void Pan(Vector3 direction)
    {
        if (StateMachine.Instance.CurrentState != StateMachine.GameState.Paused)
        {
            direction.y = 0f;
            Vector3 translation = direction * (panSpeed * 1) * 10 * (1f / targetFrameRate); // Normalize to target frame rate
            Vector3 targetPosition = cameraTransform.position + translation;
            targetPosition = ClampToBounds(targetPosition);
            cameraTransform.DOMove(targetPosition, 0.2f).SetUpdate(true); // Using DOTween to move smoothly
        }
    }

    void Zoom(float increment)
    {
        float targetZoom = cameraTransform.localPosition.y + increment * zoomSpeed;
        targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        cameraTransform.DOLocalMoveY(targetZoom, 0.2f).SetUpdate(true); // Using DOTween to zoom smoothly
    }

    Vector3 ClampToBounds(Vector3 targetPosition)
    {
        Vector3 clampedPosition = new Vector3(
            Mathf.Clamp(targetPosition.x, initialCameraPosition.x - boundsSize.x / 2, initialCameraPosition.x + boundsSize.x / 2),
            targetPosition.y,
            Mathf.Clamp(targetPosition.z, initialCameraPosition.z - boundsSize.y / 2, initialCameraPosition.z + boundsSize.y / 2)
        );

        return clampedPosition;
    }

    void OnDrawGizmosSelected()
    {
        if (CenterOfMap != null)
        {
            initialCameraPosition = CenterOfMap.position;
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(initialCameraPosition, new Vector3(boundsSize.x, 1, boundsSize.y));
    }
}
