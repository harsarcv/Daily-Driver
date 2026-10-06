using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform player;
    public Transform cam;

    [Header("Follow")]
    public float followSpeed = 10f;
    public float height = 0.5f;

    [Header("Rotation")]
    public float mouseSensitivity = 120f;
    public float minXRotation = -20f;
    public float maxXRotation = 60f;
    public float returnSpeed = 2f;

    float yRotation = 0f;
    float xRotation = 15f;

    [Header("Zoom")]
    public float zoomSpeed = 5f;
    public float minDistance = 3f;
    public float maxDistance = 8f;
    public float defaultDistance = 3f;

    float currentDistance;

    [Header("Collision")]
    public LayerMask collisionMask;
    public float collisionOffset = 0.2f;
    public float sphereRadius = 0.3f;

    void Start()
    {
        currentDistance = defaultDistance;

        Vector3 angles = transform.eulerAngles;
        yRotation = angles.y;
        xRotation = angles.x;
    }

    void Update()
    {
        // ZOOM
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            currentDistance -= scroll * zoomSpeed;
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
        }

        // FREE LOOK (klik kiri / kanan)
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            yRotation += mouseX;
            xRotation -= mouseY;

            xRotation = Mathf.Clamp(xRotation, minXRotation, maxXRotation);
        }
    }

    void LateUpdate()
    {
        // FOLLOW PLAYER
        Vector3 targetPos = player.position + Vector3.up * height;
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);

        // AUTO BALIK KE BELAKANG
        if (!(Input.GetMouseButton(0) || Input.GetMouseButton(1)))
        {
            yRotation = Mathf.LerpAngle(yRotation, player.eulerAngles.y, Time.deltaTime * returnSpeed);
        }

        // ROTASI
        Quaternion rotation = Quaternion.Euler(xRotation, yRotation, 0f);
        transform.rotation = rotation;

        // POSISI NORMAL
        Vector3 desiredPosition = transform.position - transform.forward * currentDistance;

        // 🔥 SPHERECAST (ANTI TEMBUS)
        RaycastHit hit;
        if (Physics.SphereCast(transform.position, sphereRadius, -transform.forward, out hit, currentDistance, collisionMask))
        {
            float adjustedDistance = hit.distance - collisionOffset;
            desiredPosition = transform.position - transform.forward * adjustedDistance;

            // 🔥 LANGSUNG SET (biar ga nembus)
            cam.position = desiredPosition;
        }
        else
        {
            // SMOOTH kalau tidak kena
            cam.position = Vector3.Lerp(cam.position, desiredPosition, Time.deltaTime * followSpeed);
        }

        // LOOK AT PLAYER
        cam.LookAt(transform.position);
    }
}