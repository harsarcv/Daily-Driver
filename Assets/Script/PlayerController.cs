using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 8f;
    public float acceleration = 10f;
    public float turnSpeed = 3f;

    private Rigidbody rb;

    float moveInput;
    float turnInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // STABIL
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // BIAR GA OLENG
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
    }

    void Update()
    {
        moveInput = Input.GetAxis("Vertical");
        turnInput = Input.GetAxis("Horizontal");
    }

    void FixedUpdate()
    {
        // =========================
        // GERAK MAJU
        // =========================

        Vector3 targetVelocity =
            transform.forward *
            moveInput *
            speed;

        Vector3 smoothVelocity =
            Vector3.Lerp(
                rb.linearVelocity,
                new Vector3(
                    targetVelocity.x,
                    rb.linearVelocity.y,
                    targetVelocity.z
                ),
                acceleration * Time.fixedDeltaTime
            );

        rb.linearVelocity = smoothVelocity;

        // =========================
        // BEL0K HALUS
        // =========================

        if (Mathf.Abs(moveInput) > 0.1f)
        {
            float direction =
                moveInput > 0 ? 1f : -1f;

            rb.angularVelocity = new Vector3(
                0f,
                turnInput * turnSpeed * direction,
                0f
            );
        }
        else
        {
            // STOP PUTAR SAAT DIAM
            rb.angularVelocity = new Vector3(
                0f,
                0f,
                0f
            );
        }
    }
}