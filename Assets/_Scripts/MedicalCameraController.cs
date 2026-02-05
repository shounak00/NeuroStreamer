using UnityEngine;

public class MedicalCameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // The volume to orbit around
    public Vector3 targetOffset = Vector3.zero; // Offset from target center
    
    [Header("Orbit Settings")]
    [Range(5f, 30f)]
    public float distance = 10f;
    [Range(1f, 10f)]
    public float orbitSpeed = 3f;
    [Range(0.1f, 5f)]
    public float zoomSpeed = 2f;
    [Range(5f, 50f)]
    public float minDistance = 5f;
    [Range(10f, 100f)]
    public float maxDistance = 30f;
    
    [Header("Rotation Limits")]
    public bool limitVerticalRotation = true;
    [Range(-89f, 0f)]
    public float minVerticalAngle = -80f;
    [Range(0f, 89f)]
    public float maxVerticalAngle = 80f;
    
    [Header("Movement Settings")]
    public float panSpeed = 0.5f;
    public float smoothTime = 0.1f;
    
    [Header("Input Settings")]
    public bool invertY = false;
    public bool enableAutoRotate = false;
    [Range(1f, 20f)]
    public float autoRotateSpeed = 5f;
    
    [Header("Reset Position")]
    public Vector3 defaultPosition = new Vector3(7, 3, -7);
    public Vector3 defaultRotation = new Vector3(15, -45, 0);
    
    // Private variables
    private float currentX = 0f;
    private float currentY = 0f;
    private float currentDistance;
    private Vector3 currentTargetOffset;
    private Vector3 smoothVelocity;
    
    // Mouse tracking
    private Vector3 lastMousePosition;
    private bool isRightMouseDown = false;
    private bool isMiddleMouseDown = false;
    
    void Start()
    {
        // Auto-find target if not assigned
        if (target == null)
        {
            GameObject volumeSystem = GameObject.Find("VolumeSystem");
            if (volumeSystem != null)
            {
                target = volumeSystem.transform;
            }
        }
        
        currentDistance = distance;
        currentTargetOffset = targetOffset;
        
        // Initialize angles from current rotation
        Vector3 angles = transform.eulerAngles;
        currentX = angles.y;
        currentY = angles.x;
        
        lastMousePosition = Input.mousePosition;
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        HandleInput();
        UpdateCameraPosition();
    }
    
    void HandleInput()
    {
        Vector3 currentMousePosition = Input.mousePosition;
        Vector3 mouseDelta = currentMousePosition - lastMousePosition;
        lastMousePosition = currentMousePosition;
        
        // Right mouse button - Orbit
        if (Input.GetMouseButtonDown(1))
        {
            isRightMouseDown = true;
        }
        if (Input.GetMouseButtonUp(1))
        {
            isRightMouseDown = false;
        }
        
        if (isRightMouseDown)
        {
            float mouseX = mouseDelta.x * orbitSpeed * 0.1f;
            float mouseY = mouseDelta.y * orbitSpeed * 0.1f;
            
            currentX += mouseX;
            currentY -= invertY ? -mouseY : mouseY;
            
            // Clamp vertical rotation
            if (limitVerticalRotation)
            {
                currentY = Mathf.Clamp(currentY, minVerticalAngle, maxVerticalAngle);
            }
        }
        
        // Middle mouse button - Pan
        if (Input.GetMouseButtonDown(2))
        {
            isMiddleMouseDown = true;
        }
        if (Input.GetMouseButtonUp(2))
        {
            isMiddleMouseDown = false;
        }
        
        if (isMiddleMouseDown)
        {
            float panX = -mouseDelta.x * panSpeed * 0.01f;
            float panY = -mouseDelta.y * panSpeed * 0.01f;
            
            Vector3 right = transform.right * panX;
            Vector3 up = transform.up * panY;
            
            currentTargetOffset += (right + up);
        }
        
        // Mouse scroll - Zoom
        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0f)
        {
            currentDistance -= scroll * zoomSpeed;
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
        }
        
        // Auto-rotate with Space
        if (Input.GetKey(KeyCode.Space) || enableAutoRotate)
        {
            currentX += autoRotateSpeed * Time.deltaTime;
        }
        
        // Reset camera with R key
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetCamera();
        }
        
        // Focus on target with F key
        if (Input.GetKeyDown(KeyCode.F))
        {
            FocusOnTarget();
        }
        
        // Preset views
        if (Input.GetKeyDown(KeyCode.Alpha1)) // Front view
        {
            SetPresetView(0, 0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2)) // Right view
        {
            SetPresetView(90, 0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3)) // Top view
        {
            SetPresetView(0, 90);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4)) // Perspective view
        {
            SetPresetView(-45, 30);
        }
    }
    
    void UpdateCameraPosition()
    {
        // Calculate rotation
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        
        // Calculate position
        Vector3 targetPosition = target.position + currentTargetOffset;
        Vector3 direction = rotation * Vector3.back;
        Vector3 desiredPosition = targetPosition + direction * currentDistance;
        
        // Smooth camera movement
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref smoothVelocity, smoothTime);
        transform.LookAt(targetPosition);
    }
    
    public void ResetCamera()
    {
        // Reset to default position
        transform.position = defaultPosition;
        transform.rotation = Quaternion.Euler(defaultRotation);
        
        Vector3 angles = transform.eulerAngles;
        currentX = angles.y;
        currentY = angles.x;
        
        currentTargetOffset = targetOffset;
        currentDistance = distance;
        
        Debug.Log("Camera reset to default position");
    }
    
    public void FocusOnTarget()
    {
        // Center view on target
        currentTargetOffset = targetOffset;
        currentDistance = distance;
        
        Debug.Log("Camera focused on target");
    }
    
    public void SetPresetView(float horizontalAngle, float verticalAngle)
    {
        currentX = horizontalAngle;
        currentY = verticalAngle;
        currentTargetOffset = targetOffset;
        
        Debug.Log($"Camera view set to: H:{horizontalAngle}° V:{verticalAngle}°");
    }
    
    // Public methods for UI buttons
    public void SetFrontView() => SetPresetView(0, 0);
    public void SetRightView() => SetPresetView(90, 0);
    public void SetTopView() => SetPresetView(0, 90);
    public void SetPerspectiveView() => SetPresetView(-45, 30);
    
    public void ToggleAutoRotate()
    {
        enableAutoRotate = !enableAutoRotate;
    }
    
    // Draw orbit circle in scene view for debugging
    void OnDrawGizmosSelected()
    {
        if (target == null) return;
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(target.position + targetOffset, distance);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(target.position + targetOffset, transform.position);
    }
}
