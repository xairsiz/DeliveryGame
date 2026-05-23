using UnityEngine;

public class SimpleCar : MonoBehaviour
{
    [Header("Driving")]
    public float acceleration = 25f;
    public float reverseSpeed = 10f;
    public float brakeStrength = 15f;
    public float turnSpeed = 80f;
    public float maxSpeed = 20f;

    [Header("Enter / Exit")]
    public GameObject player;        // your Player object
    public Camera playerCamera;      // the camera on your Player
    public Transform exitPoint;      // where player appears on exit
    public Camera carCamera;         // camera that follows the car
    public float enterDistance = 4f;

    private Rigidbody rb;
    private bool isDriving = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0f, -0.5f, 0f);
        if (carCamera != null) carCamera.gameObject.SetActive(false);
    }

    void Update()
    {
        // Press E to enter or exit
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!isDriving) TryEnter();
            else ExitCar();
        }
    }

    void FixedUpdate()
    {
        if (!isDriving)
        {
            // slow to stop when parked
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 2f);
            return;
        }

        float throttle = Input.GetAxis("Vertical");
        float steer = Input.GetAxis("Horizontal");

        // forward / reverse / brake
        if (throttle > 0)
            rb.AddForce(transform.forward * throttle * acceleration, ForceMode.Acceleration);
        else if (throttle < 0)
        {
            if (Vector3.Dot(rb.linearVelocity, transform.forward) > 1f)
                rb.AddForce(-transform.forward * brakeStrength, ForceMode.Acceleration);
            else
                rb.AddForce(transform.forward * throttle * reverseSpeed, ForceMode.Acceleration);
        }

        // steering (only when moving)
        if (rb.linearVelocity.magnitude > 0.5f)
        {
            float turn = steer * turnSpeed * Time.fixedDeltaTime;
            if (Vector3.Dot(rb.linearVelocity, transform.forward) < 0) turn = -turn;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turn, 0f));
        }

        // speed cap
        rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, maxSpeed);
    }

    void TryEnter()
    {
        if (player == null) return;
        float dist = Vector3.Distance(player.transform.position, transform.position);
        if (dist > enterDistance) return;

        isDriving = true;
        player.SetActive(false);
        if (playerCamera != null) playerCamera.gameObject.SetActive(false);
        if (carCamera != null) carCamera.gameObject.SetActive(true);
    }

    void ExitCar()
    {
        isDriving = false;

        if (exitPoint != null)
            player.transform.position = exitPoint.position;
        else
            player.transform.position = transform.position + transform.right * 3f + Vector3.up;

        player.SetActive(true);
        if (playerCamera != null) playerCamera.gameObject.SetActive(true);
        if (carCamera != null) carCamera.gameObject.SetActive(false);
    }
}