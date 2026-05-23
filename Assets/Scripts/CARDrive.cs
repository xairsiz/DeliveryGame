using UnityEngine;

public class CarDrive : MonoBehaviour
{
    [Header("Driving Settings")]
    public float acceleration = 30f;
    public float reverseAcceleration = 12f;
    public float brakeStrength = 14f;
    public float turnStrength = 90f;
    public float maxSpeed = 22f; // in m/s, ~50mph

    [Header("Wheel Visuals (optional)")]
    public Transform[] wheels;       // drag your 4 tires here
    public float wheelSpinSpeed = 400f;

    [HideInInspector] public bool isDriving = false;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0f, -0.6f, 0f); // keeps it stable
    }

    void FixedUpdate()
    {
        if (!isDriving)
        {
            // Gently slow to a stop when nobody is driving
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 2f);
            return;
        }

        float throttle = Input.GetAxis("Vertical");
        float steering = Input.GetAxis("Horizontal");

        // Forward / reverse
        if (throttle > 0)
        {
            rb.AddForce(transform.forward * throttle * acceleration, ForceMode.Acceleration);
        }
        else if (throttle < 0)
        {
            // braking if moving forward, otherwise reverse
            if (Vector3.Dot(rb.linearVelocity, transform.forward) > 1f)
                rb.AddForce(-transform.forward * brakeStrength, ForceMode.Acceleration);
            else
                rb.AddForce(transform.forward * throttle * reverseAcceleration, ForceMode.Acceleration);
        }

        // Steering only works when moving
        if (rb.linearVelocity.magnitude > 0.5f)
        {
            float turn = steering * turnStrength * Time.fixedDeltaTime;
            // reverse steering when going backwards
            if (Vector3.Dot(rb.linearVelocity, transform.forward) < 0)
                turn = -turn;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0, turn, 0));
        }

        // Speed cap
        rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, maxSpeed);

        // Spin the visual wheels
        SpinWheels(throttle);
    }

    void SpinWheels(float throttle)
    {
        if (wheels == null) return;
        foreach (Transform w in wheels)
        {
            if (w != null)
                w.Rotate(Vector3.right, wheelSpinSpeed * throttle * Time.fixedDeltaTime, Space.Self);
        }
    }
}