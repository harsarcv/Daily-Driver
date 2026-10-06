using UnityEngine;

public class CarFSM : MonoBehaviour
{
    public enum State
    {
        Move,
        Stop,
        Avoid
    }

    // ==================================================
    // REFERENCE
    // ==================================================

    [Header("Reference")]
    public NPCNodeMovement motor;  // Ganti ke NPCNodeMovement

    private Rigidbody rb;

    // ==================================================
    // DETECTION
    // ==================================================

    [Header("Detection")]
    public float detectDistance = 10f;

    public float sphereRadius = 1.2f;

    public float sideOffset = 2f;

    public LayerMask stopLayer;

    public LayerMask obstacleLayer;

    public LayerMask trafficLightLayer;

    // ==================================================
    // TRAFFIC LIGHT
    // ==================================================

    [Header("Traffic Light")]
    public float trafficStopDistance = 5f;

    // ==================================================
    // MOVEMENT
    // ==================================================

    [Header("Movement")]
    public float moveSpeed = 6f;

    public float rotateSpeed = 5f;

    // ==================================================
    // AVOID
    // ==================================================

    [Header("Avoid")]
    public float avoidTime = 1.2f;

    private float timer;

    private Vector3 avoidDirection;

    // ==================================================
    // STATE
    // ==================================================

    private State state;

    // ==================================================
    // START
    // ==================================================

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.interpolation = RigidbodyInterpolation.Interpolate;

        state = State.Move;
    }

    // ==================================================
    // FIXED UPDATE
    // ==================================================

    void FixedUpdate()
    {
        switch (state)
        {
            case State.Move:
                Move();
                break;

            case State.Stop:
                Stop();
                break;

            case State.Avoid:
                Avoid();
                break;
        }
    }

    // ==================================================
    // MOVE
    // ==================================================

    void Move()
    {
        // =============================================
        // DETECTION POINT
        // =============================================

        Vector3 forwardPoint = transform.position + transform.forward * detectDistance;
        Vector3 rightPoint = forwardPoint + transform.right * sideOffset;
        Vector3 leftPoint = forwardPoint - transform.right * sideOffset;

        // =============================================
        // DETECT OBSTACLE
        // =============================================

        bool center = Physics.CheckSphere(forwardPoint, sphereRadius, obstacleLayer);
        bool right = Physics.CheckSphere(rightPoint, sphereRadius, obstacleLayer);
        bool left = Physics.CheckSphere(leftPoint, sphereRadius, obstacleLayer);

        // =============================================
        // DETECT STOP
        // =============================================

        bool stop = Physics.CheckSphere(forwardPoint, sphereRadius, stopLayer);

        // =============================================
        // TRAFFIC LIGHT CHECK
        // =============================================

        Collider[] trafficLights = Physics.OverlapSphere(forwardPoint, sphereRadius, trafficLightLayer);

        foreach (Collider col in trafficLights)
        {
            TrafficLight light = col.GetComponent<TrafficLight>();

            if (light != null)
            {
                float dist = Vector3.Distance(transform.position, light.transform.position);

                if (light.IsRed() && dist > trafficStopDistance)
                {
                    state = State.Stop;
                    return;
                }
            }
        }

        // =============================================
        // STOP TOTAL
        // =============================================

        if (center && right && left)
        {
            state = State.Stop;
            return;
        }

        // =============================================
        // AVOID
        // =============================================

        if (center)
        {
            if (!right)
            {
                StartAvoid(true);
            }
            else if (!left)
            {
                StartAvoid(false);
            }
            return;
        }

        // =============================================
        // STOP
        // =============================================

        if (stop)
        {
            state = State.Stop;
            return;
        }

        // =============================================
        // WAYPOINT MOVEMENT
        // =============================================

        Vector3 dir = Vector3.zero;

        if (motor != null)
        {
            dir = motor.GetDirection();
        }
        else
        {
            NPCNodeMovement nodeMovement = GetComponent<NPCNodeMovement>();
            if (nodeMovement != null)
            {
                dir = nodeMovement.GetDirection();
            }
        }

        if (dir == Vector3.zero) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        rb.rotation = Quaternion.Slerp(rb.rotation, targetRot, rotateSpeed * Time.fixedDeltaTime);

        rb.MovePosition(rb.position + transform.forward * moveSpeed * Time.fixedDeltaTime);
    }

    // ==================================================
    // STOP
    // ==================================================

    void Stop()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 forwardPoint = transform.position + transform.forward * detectDistance;

        bool stillBlocked = Physics.CheckSphere(forwardPoint, sphereRadius, obstacleLayer);

        // =============================================
        // TRAFFIC LIGHT CHECK
        // =============================================

        bool redLight = false;

        Collider[] trafficLights = Physics.OverlapSphere(forwardPoint, sphereRadius, trafficLightLayer);

        foreach (Collider col in trafficLights)
        {
            TrafficLight light = col.GetComponent<TrafficLight>();

            if (light != null)
            {
                float dist = Vector3.Distance(transform.position, light.transform.position);

                if (light.IsRed() && dist > trafficStopDistance)
                {
                    redLight = true;
                }
            }
        }

        // =============================================
        // MOVE AGAIN
        // =============================================

        if (!stillBlocked && !redLight)
        {
            state = State.Move;
        }
    }

    // ==================================================
    // START AVOID
    // ==================================================

    void StartAvoid(bool goRight)
    {
        state = State.Avoid;
        timer = avoidTime;
        avoidDirection = (transform.forward + (goRight ? transform.right : -transform.right)).normalized;
    }

    // ==================================================
    // AVOID
    // ==================================================

    void Avoid()
    {
        timer -= Time.fixedDeltaTime;

        Quaternion targetRot = Quaternion.LookRotation(avoidDirection);
        rb.rotation = Quaternion.Slerp(rb.rotation, targetRot, rotateSpeed * Time.fixedDeltaTime);

        rb.MovePosition(rb.position + transform.forward * moveSpeed * Time.fixedDeltaTime);

        if (timer <= 0f)
        {
            state = State.Move;
        }
    }

    // ==================================================
    // GIZMOS
    // ==================================================

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Vector3 forwardPoint = transform.position + transform.forward * detectDistance;
        Vector3 rightPoint = forwardPoint + transform.right * sideOffset;
        Vector3 leftPoint = forwardPoint - transform.right * sideOffset;

        Gizmos.DrawWireSphere(forwardPoint, sphereRadius);
        Gizmos.DrawWireSphere(rightPoint, sphereRadius);
        Gizmos.DrawWireSphere(leftPoint, sphereRadius);
    }
}