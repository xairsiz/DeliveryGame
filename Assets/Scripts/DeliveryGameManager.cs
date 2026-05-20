using System.Collections;
using UnityEngine;

/// Master game logic: entering/exiting the car, picking up orders, delivering,
/// doorbell interaction, and the full IMGUI HUD.
/// All scene references are found automatically in Start() — no manual wiring.
public class DeliveryGameManager : MonoBehaviour
{
    // ── Runtime state ────────────────────────────────────────────────────────
    int money;
    int deliveries;
    bool inCar;
    bool hasFood;
    bool ringing;      // doorbell coroutine running

    DeliveryHouse currentTarget;
    string flashMsg    = "";
    float  flashTimer;

    // ── Scene refs (found via FindObjectOfType / Find in Start) ─────────────
    GameObject         player;
    PlayerController   playerCtrl;
    CharacterController playerCC;
    GameObject         car;
    Rigidbody          carRb;
    ManualCarController carCtrl;
    Transform          cam;
    GameObject         shop;
    DeliveryHouse[]    houses;

    AudioSource sfxSource;

    AudioClip sfxPickup;
    AudioClip sfxChaChing;
    AudioClip sfxDoorbell;
    AudioClip sfxError;

    // Camera local transforms
    readonly Vector3    fpCamLocalPos = new Vector3(0f, 0.65f, 0f);
    readonly Vector3    chaseCamLocalPos = new Vector3(0f, 2.2f, -5.5f);
    readonly Quaternion chaseCamLocalRot = default;

    // Chase cam smooth current
    Vector3    camCurPos;
    Quaternion camCurRot;
    float camPitchCar;
    float camYawCar;

    // ── Init ─────────────────────────────────────────────────────────────────
    void Start()
    {
        player    = GameObject.Find("Player");
        car       = GameObject.Find("Manual Delivery Car");
        shop      = GameObject.Find("Takeaway Shop");
        houses    = FindObjectsByType<DeliveryHouse>(FindObjectsSortMode.None);

        if (player != null)
        {
            playerCtrl = player.GetComponent<PlayerController>();
            playerCC   = player.GetComponent<CharacterController>();
            cam        = player.GetComponentInChildren<Camera>()?.transform;
        }

        if (car != null)
        {
            carRb   = car.GetComponent<Rigidbody>();
            carCtrl = car.GetComponent<ManualCarController>();
        }

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;

        sfxPickup   = MakeTones(new[]{ (523f,0.0f,0.12f),(784f,0.1f,0.18f) }, 0.28f, "Pickup");
        sfxChaChing = MakeTones(new[]{ (988f,0.0f,0.18f),(1319f,0.12f,0.32f) }, 0.46f, "ChaChing");
        sfxDoorbell = MakeTones(new[]{ (660f,0.0f,0.38f),(523f,0.32f,0.54f) }, 0.9f,  "Doorbell");
        sfxError    = MakeTones(new[]{ (200f,0.0f,0.15f) }, 0.2f, "Error");

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        camCurPos = chaseCamLocalPos;
        camCurRot = Quaternion.Euler(12f, 0f, 0f);
    }

    // ── Per-frame ─────────────────────────────────────────────────────────────
    void Update()
    {
        if (!Application.isPlaying) return;

        // Cursor toggle
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool locked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible   = locked;
        }

        // Flash timer
        if (flashTimer > 0f) flashTimer -= Time.deltaTime;
        else flashMsg = "";

        // Car camera look (locked yaw/pitch while driving, resets on exit)
        if (inCar) HandleCarCamera();

        // Actions
        if (Input.GetKeyDown(KeyCode.F)) TryToggleCar();
        if (Input.GetKeyDown(KeyCode.E) && !inCar) TryInteract();
    }

    void LateUpdate()
    {
        if (!Application.isPlaying || !inCar || cam == null) return;
        cam.localPosition = Vector3.Lerp(cam.localPosition, camCurPos, Time.deltaTime * 8f);
        cam.localRotation = Quaternion.Slerp(cam.localRotation, camCurRot, Time.deltaTime * 8f);
    }

    // ── Car camera ───────────────────────────────────────────────────────────
    void HandleCarCamera()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        float mx = Input.GetAxis("Mouse X") * 2f;
        float my = Input.GetAxis("Mouse Y") * 2f;
        camYawCar   = Mathf.Clamp(camYawCar   + mx,  -80f,  80f);
        camPitchCar = Mathf.Clamp(camPitchCar - my,  -30f,  50f);
        camCurPos   = chaseCamLocalPos;
        camCurRot   = Quaternion.Euler(camPitchCar + 12f, camYawCar, 0f);
    }

    // ── Enter / exit car ─────────────────────────────────────────────────────
    void TryToggleCar()
    {
        if (player == null || car == null || cam == null) return;

        if (!inCar)
        {
            float dist = Vector3.Distance(player.transform.position, car.transform.position);
            if (dist > 4.5f) { Flash("Too far from car"); return; }

            EnterCar();
        }
        else
        {
            ExitCar();
        }
    }

    void EnterCar()
    {
        // Reparent cam BEFORE disabling player (cam is player's child)
        cam.SetParent(car.transform);
        camYawCar   = 0f;
        camPitchCar = 12f;
        camCurPos   = chaseCamLocalPos;
        camCurRot   = Quaternion.Euler(camPitchCar, 0f, 0f);
        cam.localPosition = camCurPos;
        cam.localRotation = camCurRot;

        player.SetActive(false);

        if (carCtrl) carCtrl.CanDrive = true;
        inCar = true;
    }

    void ExitCar()
    {
        player.SetActive(true);

        // Teleport player beside car using CharacterController-safe method
        Vector3 exitPos = car.transform.position
                        + car.transform.right * 2.6f
                        + Vector3.up * 1.2f;
        playerCC.enabled = false;
        player.transform.position = exitPos;
        playerCC.enabled = true;

        // Reparent cam back to player
        cam.SetParent(player.transform);
        cam.localPosition = fpCamLocalPos;
        cam.localRotation = Quaternion.identity;

        if (playerCtrl) { playerCtrl.CanMove = true; }
        if (carCtrl)    { carCtrl.CanDrive   = false; }
        inCar = false;
    }

    // ── Foot interaction ─────────────────────────────────────────────────────
    void TryInteract()
    {
        if (player == null) return;

        Vector3 pos = player.transform.position;

        // Deliver to current target
        if (hasFood && currentTarget != null)
        {
            float d = Vector3.Distance(pos, currentTarget.doorWorldPos);
            if (d < 4f && !ringing)
            {
                StartCoroutine(DoDelivery());
                return;
            }
        }

        // Pick up at shop
        if (shop != null && !hasFood)
        {
            if (Vector3.Distance(pos, shop.transform.position) < 8f)
            {
                PickUpOrder();
                return;
            }
        }

        // Feedback
        if (hasFood && currentTarget != null)
            Flash("Drive to " + currentTarget.houseName);
        else if (hasFood)
            Flash("No target assigned!");
        else
            Flash("Go to the shop to pick up an order  (E)");
    }

    void PickUpOrder()
    {
        if (houses == null || houses.Length == 0) return;

        // Pick a random house (not the one we just delivered to)
        DeliveryHouse next = houses[Random.Range(0, houses.Length)];
        for (int i = 0; i < 8 && next == currentTarget; i++)
            next = houses[Random.Range(0, houses.Length)];

        // Clear old marker
        if (currentTarget != null) SetBeacon(currentTarget, false);

        currentTarget = next;
        currentTarget.isActiveTarget = true;
        SetBeacon(currentTarget, true);

        hasFood = true;
        sfxSource.PlayOneShot(sfxPickup);
        Flash("Order picked up!  Deliver to " + currentTarget.houseName);
    }

    IEnumerator DoDelivery()
    {
        ringing = true;
        sfxSource.PlayOneShot(sfxDoorbell);
        Flash("Ringing doorbell...");
        yield return new WaitForSeconds(1.4f);

        money      += 15;
        deliveries += 1;
        hasFood     = false;

        SetBeacon(currentTarget, false);
        currentTarget.isActiveTarget = false;
        currentTarget = null;

        sfxSource.PlayOneShot(sfxChaChing);
        Flash("Delivered!  +£15");
        ringing = false;
    }

    // ── Beacon helper ─────────────────────────────────────────────────────────
    void SetBeacon(DeliveryHouse house, bool on)
    {
        if (house == null) return;
        Transform beacon = house.transform.Find("__Beacon");
        if (beacon != null) beacon.gameObject.SetActive(on);
    }

    void Flash(string msg, float duration = 3.5f)
    {
        flashMsg   = msg;
        flashTimer = duration;
    }

    // ── HUD (IMGUI) ───────────────────────────────────────────────────────────
    void OnGUI()
    {
        if (!Application.isPlaying) return;

        GUIStyle box  = GUI.skin.box;
        GUIStyle lbl  = GUI.skin.label;

        // ── Left panel ──
        GUI.Box(new Rect(10, 10, 260, inCar ? 230 : 100), "");

        int y = 28;
        GUI.Label(new Rect(22, y, 240, 24), $"£{money}   |   Deliveries: {deliveries}"); y += 28;

        if (hasFood && currentTarget != null)
            GUI.Label(new Rect(22, y, 240, 24), "Deliver to: " + currentTarget.houseName);
        else if (!hasFood)
            GUI.Label(new Rect(22, y, 240, 24), "Go pick up an order at the shop");
        y += 28;

        if (inCar)
        {
            GUI.Label(new Rect(22, y, 240, 24), "─────────────────────"); y += 20;

            string gearLabel = carCtrl ? GearLabel(carCtrl.Gear) : "?";
            GUI.Label(new Rect(22, y, 240, 24), $"Gear: {gearLabel}"); y += 26;

            float kmh = carCtrl ? carCtrl.SpeedKmh : 0f;
            GUI.Label(new Rect(22, y, 240, 24), $"Speed: {kmh:F0} km/h"); y += 26;

            float rpm = carCtrl ? carCtrl.RPM : 0f;
            GUI.Label(new Rect(22, y, 240, 24), $"RPM: {rpm:F0}"); y += 26;

            if (carCtrl != null && !carCtrl.EngineOn)
            {
                Color prev = GUI.color;
                GUI.color = Color.red;
                GUI.Label(new Rect(22, y, 240, 24), "ENGINE STALLED  — Press R");
                GUI.color = prev;
            }
        }

        // ── Right controls panel ──
        GUI.Box(new Rect(Screen.width - 275, 10, 265, inCar ? 180 : 80), "");
        int ry = 28;
        GUI.Label(new Rect(Screen.width - 263, ry, 250, 22), "F = Enter / Exit car"); ry += 24;
        if (!inCar)
            GUI.Label(new Rect(Screen.width - 263, ry, 250, 22), "E = Interact / Pickup / Deliver");
        else
        {
            GUI.Label(new Rect(Screen.width - 263, ry, 250, 22), "L.Shift = Clutch");      ry += 22;
            GUI.Label(new Rect(Screen.width - 263, ry, 250, 22), "E = Gear Up  |  Q = Gear Down"); ry += 22;
            GUI.Label(new Rect(Screen.width - 263, ry, 250, 22), "R = Restart engine");     ry += 22;
            string clutch = (carCtrl != null && carCtrl.ClutchDown) ? "DOWN ✓" : "UP";
            GUI.Label(new Rect(Screen.width - 263, ry, 250, 22), "Clutch: " + clutch);
        }

        // ── Flash message ──
        if (flashTimer > 0f)
        {
            float alpha = Mathf.Clamp01(flashTimer);
            Color prev = GUI.color;
            GUI.color = new Color(1f, 1f, 0f, alpha);
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            GUI.Label(new Rect(Screen.width / 2f - 250f, Screen.height - 80f, 500f, 40f), flashMsg, style);
            GUI.color = prev;
        }
    }

    static string GearLabel(int g) => g switch
    {
        -1 => "R",
         0 => "N",
         _ => g.ToString()
    };

    // ── Procedural audio helper ───────────────────────────────────────────────
    static AudioClip MakeTones((float freq, float start, float end)[] notes, float total, string name)
    {
        const int sr = 44100;
        int n = Mathf.CeilToInt(sr * total);
        float[] s = new float[n];
        foreach (var (freq, start, end) in notes)
        {
            int from = Mathf.FloorToInt(start * sr);
            int to   = Mathf.CeilToInt(end   * sr);
            for (int i = from; i < to && i < n; i++)
            {
                float t   = (float)(i - from) / sr;
                float env = Mathf.Exp(-4f * t / (end - start));
                s[i] += Mathf.Sin(2f * Mathf.PI * freq * t) * 0.28f * env;
            }
        }
        var clip = AudioClip.Create(name, n, 1, sr, false);
        clip.SetData(s, 0);
        return clip;
    }
}
