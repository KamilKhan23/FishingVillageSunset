using UnityEngine;

/// <summary>
/// Keyboard explorer for the Fishing Village at Sunset.
/// W/A/S/D move, Q/E go down/up (frame-rate independent), Left Shift moves faster.
/// Arrow keys turn and tilt the view (holding the right mouse button also looks around - optional extra).
/// Keys 1, 2 and 3 snap the camera to the three viewpoint GameObjects assigned in the Inspector.
/// </summary>
public class ExplorerCamera : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float verticalSpeed = 6f;
    [SerializeField] private float sprintMultiplier = 2.5f;

    [Header("Looking (arrow keys, or hold right mouse button)")]
    [SerializeField] private float keyTurnSpeed = 75f;
    [SerializeField] private float mouseSensitivity = 3f;
    [SerializeField] private float minPitch = -60f;
    [SerializeField] private float maxPitch = 70f;

    [Header("Viewpoints (keys 1, 2, 3)")]
    [SerializeField] private Transform[] viewpoints = new Transform[3];

    [Header("World limits")]
    [SerializeField] private Terrain terrain;
    [SerializeField] private float waterLevel = 10f;
    [SerializeField] private float minHeightAboveGround = 1.8f;
    [SerializeField] private float maxHeightAboveGround = 30f;
    [SerializeField] private Vector3 boundsCentre = new Vector3(160f, 0f, 140f);
    [SerializeField] private float boundsRadius = 130f;

    private float yaw;
    private float pitch;

    private void Start()
    {
        SyncAnglesFromTransform();
    }

    private void Update()
    {
        HandleViewpointKeys();
        HandleLook();
        HandleMovement();
        KeepInsideWorld();
    }

    private void HandleViewpointKeys()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) SnapToViewpoint(0);
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) SnapToViewpoint(1);
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) SnapToViewpoint(2);
    }

    private void SnapToViewpoint(int index)
    {
        if (viewpoints == null || index >= viewpoints.Length || viewpoints[index] == null)
        {
            Debug.LogWarning("Viewpoint " + (index + 1) + " is not assigned on " + name);
            return;
        }

        transform.SetPositionAndRotation(viewpoints[index].position, viewpoints[index].rotation);
        SyncAnglesFromTransform();
    }

    private void HandleLook()
    {
        float turn = 0f;
        float tilt = 0f;
        if (Input.GetKey(KeyCode.LeftArrow)) turn -= 1f;
        if (Input.GetKey(KeyCode.RightArrow)) turn += 1f;
        if (Input.GetKey(KeyCode.UpArrow)) tilt -= 1f;
        if (Input.GetKey(KeyCode.DownArrow)) tilt += 1f;

        yaw += turn * keyTurnSpeed * Time.deltaTime;
        pitch += tilt * keyTurnSpeed * Time.deltaTime;

        if (Input.GetMouseButton(1))
        {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        }

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void HandleMovement()
    {
        Vector3 input = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) input.z += 1f;
        if (Input.GetKey(KeyCode.S)) input.z -= 1f;
        if (Input.GetKey(KeyCode.D)) input.x += 1f;
        if (Input.GetKey(KeyCode.A)) input.x -= 1f;

        float vertical = 0f;
        if (Input.GetKey(KeyCode.E)) vertical += 1f;
        if (Input.GetKey(KeyCode.Q)) vertical -= 1f;

        float speedScale = Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f;

        // Move across the ground plane, so looking up or down does not make W fly or dive.
        Quaternion heading = Quaternion.Euler(0f, yaw, 0f);
        Vector3 horizontal = heading * Vector3.ClampMagnitude(input, 1f) * moveSpeed;
        Vector3 velocity = horizontal + Vector3.up * vertical * verticalSpeed;

        // Time.deltaTime makes the speed the same at any frame rate.
        transform.position += velocity * speedScale * Time.deltaTime;
    }

    private void KeepInsideWorld()
    {
        Vector3 pos = transform.position;

        Vector3 offset = new Vector3(pos.x - boundsCentre.x, 0f, pos.z - boundsCentre.z);
        if (offset.magnitude > boundsRadius)
        {
            offset = offset.normalized * boundsRadius;
            pos.x = boundsCentre.x + offset.x;
            pos.z = boundsCentre.z + offset.z;
        }

        if (terrain != null)
        {
            float ground = terrain.SampleHeight(pos) + terrain.transform.position.y;
            ground = Mathf.Max(ground, waterLevel);
            pos.y = Mathf.Clamp(pos.y, ground + minHeightAboveGround, ground + maxHeightAboveGround);
        }

        transform.position = pos;
    }

    private void SyncAnglesFromTransform()
    {
        Vector3 euler = transform.rotation.eulerAngles;
        yaw = euler.y;
        pitch = euler.x > 180f ? euler.x - 360f : euler.x;
    }
}
