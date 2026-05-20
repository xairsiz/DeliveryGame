using UnityEngine;

/// Rigidbody arcade car with a proper manual gearbox.
/// Gear -1 = Reverse, 0 = Neutral, 1-5 = forward gears.
/// Left Shift = clutch. E = gear up. Q = gear down. R = restart engine.
/// Set CanDrive = true from DeliveryGameManager when the player is inside.
[RequireComponent(typeof(Rigidbody))]
public class ManualCarController : MonoBehaviour
{
    [Header("Driving Feel")]
    public float acceleration  = 28f;
    public float reverseAccel  = 12f;
    public float brakeStrength = 18f;
    public float turnStrength  = 110f;
    public float grip          = 5f;

    [Header("Engine")]
    public float idleRPM = 950f;
    public float maxRPM  = 7200f;
    public float stallRPM = 480f;

    [Header("Gears  (index 0 unused, 1-5 are forward)")]
    public float[] gearMaxSpeed  = { 0f, 14f, 26f, 40f, 56f, 75f }; // km/h
    public float[] gearTorqueMul = { 0f, 1.9f, 1.4f, 1.05f, 0.8f, 0.6f };

    // ── State (read by DeliveryGameManager / ObjectiveUI) ──────────────────
    public bool CanDrive;          // set externally; gates all input
    public int  Gear    { get; private set; }   // -1=R  0=N  1-5=forward
    public float RPM    { get; private set; }
    public float SpeedKmh => rb ? rb.linearVelocity.magnitude * 3.6f : 0f;
    public bool EngineOn { get; private set; } = true;
    public bool ClutchDown { get; private set; }

    Rigidbody rb;
    AudioSource engineAudio;
    float stallCooldown;  // prevents instant re-stall after restart

    // ── Lifecycle ───────────────────────────────────────────────────────────
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = 1100f;
        rb.linearDamping  = 0.18f;
        rb.angularDamping = 4f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.centerOfMass = new Vector3(0f, -0.4f, 0f);

        engineAudio = gameObject.AddComponent<AudioSource>();
        engineAudio.clip      = BuildEngineLoop();
        engineAudio.loop      = true;
        engineAudio.volume    = 0f;
        engineAudio.spatialBlend = 0f;
        engineAudio.Play();

        RPM = idleRPM;
        Gear = 0;
    }

    void Update()
    {
        if (!Application.isPlaying) return;

        // Clutch
        ClutchDown = Input.GetKey(KeyCode.LeftShift);

        if (!CanDrive) { UpdateEngineAudio(); return; }

        // Gear shift — requires clutch
        if (Input.GetKeyDown(KeyCode.E) && ClutchDown)
            ShiftUp();
        if (Input.GetKeyDown(KeyCode.Q) && ClutchDown)
            ShiftDown();

        // Restart stalled engine
        if (Input.GetKeyDown(KeyCode.R) && !EngineOn)
        {
            EngineOn   = true;
            RPM        = idleRPM;
            stallCooldown = 2f;
        }

        UpdateRPM();
        UpdateEngineAudio();
    }

    void FixedUpdate()
    {
        if (!Application.isPlaying || !CanDrive) return;

        FakeSuspension();
        DrivePhysics();
        LateralGrip();
    }

    // ── Gearbox ─────────────────────────────────────────────────────────────
    void ShiftUp()
    {
        // R → N → 1 → 2 → 3 → 4 → 5
        if (Gear < 5) Gear++;
        RPM = Mathf.Clamp(RPM * 0.6f, idleRPM, maxRPM);
    }

    void ShiftDown()
    {
        // 5 → 4 → ... → 1 → N → R
        if (Gear > -1) Gear--;
        RPM = Mathf.Clamp(RPM * 1.3f, idleRPM, maxRPM);
    }

    // ── RPM simulation ──────────────────────────────────────────────────────
    void UpdateRPM()
    {
        if (stallCooldown > 0f) stallCooldown -= Time.deltaTime;

        if (!EngineOn) { RPM = Mathf.MoveTowards(RPM, 0f, 2000f * Time.deltaTime); return; }

        float throttle = Input.GetAxis("Vertical");

        if (Gear == 0 || ClutchDown)
        {
            // Neutral / clutch — rev freely
            float target = idleRPM + Mathf.Abs(throttle) * 4800f;
            RPM = Mathf.Lerp(RPM, target, Time.deltaTime * 5f);
        }
        else
        {
            int g = Mathf.Abs(Gear);
            float ratio = Mathf.InverseLerp(0f, gearMaxSpeed[g], SpeedKmh);
            float target = Mathf.Lerp(idleRPM, maxRPM * 0.95f, ratio);
            if (Mathf.Abs(throttle) > 0.1f) target += 600f;
            RPM = Mathf.Lerp(RPM, target, Time.deltaTime * 6f);

            // Stall — high gear + very low speed + throttle + clutch up
            if (stallCooldown <= 0f && Gear >= 3 && SpeedKmh < 6f && throttle > 0.15f)
            {
                RPM -= 1400f * Time.deltaTime;
                if (RPM < stallRPM) { EngineOn = false; RPM = 0f; }
            }
        }
        RPM = Mathf.Clamp(RPM, EngineOn ? idleRPM * 0.3f : 0f, maxRPM);
    }

    // ── Physics ─────────────────────────────────────────────────────────────
    void DrivePhysics()
    {
        if (!EngineOn || Gear == 0) return;

        float throttle = Input.GetAxis("Vertical");
        float steer    = Input.GetAxis("Horizontal");

        if (Gear > 0)   // Forward gears
        {
            float kmhMax = gearMaxSpeed[Gear];

            if (throttle > 0f && SpeedKmh < kmhMax && !ClutchDown)
            {
                float force = acceleration * gearTorqueMul[Gear] * throttle;
                rb.AddForce(transform.forward * force, ForceMode.Acceleration);
            }
            else if (throttle < 0f)   // Braking in forward gear
            {
                float fwdDot = Vector3.Dot(rb.linearVelocity, transform.forward);
                rb.AddForce(-transform.forward * brakeStrength * Mathf.Abs(throttle), ForceMode.Acceleration);
            }
        }
        else if (Gear == -1)   // Reverse
        {
            if (throttle < 0f && SpeedKmh < 20f && !ClutchDown)
                rb.AddForce(-transform.forward * reverseAccel * Mathf.Abs(throttle), ForceMode.Acceleration);
            else if (throttle > 0f)  // Braking in reverse
                rb.AddForce(transform.forward * brakeStrength * throttle, ForceMode.Acceleration);
        }

        // Steering (only meaningful above walking speed)
        if (SpeedKmh > 1.5f)
        {
            float speedFactor = Mathf.Clamp01(SpeedKmh / 20f);   // less snap at low speed
            rb.MoveRotation(rb.rotation *
                Quaternion.Euler(0f, steer * turnStrength * speedFactor * Time.fixedDeltaTime, 0f));
        }
    }

    void LateralGrip()
    {
        // Kill side-slip so the car doesn't slide like it's on ice
        Vector3 vel    = rb.linearVelocity;
        float   side   = Vector3.Dot(vel, transform.right);
        rb.linearVelocity -= transform.right * side * grip * Time.fixedDeltaTime;
    }

    void FakeSuspension()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 0.5f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 2.0f))
        {
            float targetHeight  = 1.0f;
            float error         = targetHeight - hit.distance;
            if (error > 0f)
                rb.AddForce(Vector3.up * error * 40f, ForceMode.Acceleration);
        }
    }

    // ── Audio ───────────────────────────────────────────────────────────────
    void UpdateEngineAudio()
    {
        if (engineAudio == null) return;
        float t = Mathf.InverseLerp(idleRPM, maxRPM, RPM);
        engineAudio.pitch  = Mathf.Lerp(0.65f, 2.4f, t);
        engineAudio.volume = EngineOn ? (CanDrive ? 0.35f : 0.15f) : Mathf.MoveTowards(engineAudio.volume, 0f, Time.deltaTime * 3f);
    }

    static AudioClip BuildEngineLoop()
    {
        const int sr = 44100;
        const float dur = 0.5f;
        int n = Mathf.CeilToInt(sr * dur);
        float[] s = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / sr;
            float v = Mathf.Sin(2f * Mathf.PI * 90f * t)
                    + 0.5f  * Mathf.Sin(2f * Mathf.PI * 180f * t)
                    + 0.25f * Mathf.Sin(2f * Mathf.PI * 270f * t)
                    + 0.15f * Mathf.PerlinNoise(t * 30f, 0f) - 0.075f;
            s[i] = v * 0.18f;
        }
        var clip = AudioClip.Create("EngineLoop", n, 1, sr, false);
        clip.SetData(s, 0);
        return clip;
    }

    public void ResetMotion()
    {
        rb.linearVelocity  = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        Gear = 0;
        RPM  = idleRPM;
        EngineOn = true;
        stallCooldown = 1f;
    }
}
