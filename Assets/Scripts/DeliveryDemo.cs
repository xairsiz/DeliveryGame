using UnityEngine;

[ExecuteAlways]
public class DeliveryDemo : MonoBehaviour
{
    [Header("City Settings")]
    public int houseCount = 20;
    public float citySize = 100f;

    [Header("Gameplay")]
    public int money = 0;
    public int rewardPerDelivery = 15;
    public float interactDistance = 7f;

    [Header("Manual Car")]
    public int gear = 0; // 0 = Neutral
    public int maxGear = 5;

    public float acceleration = 32f;
    public float reverseAcceleration = 10f;
    public float brakeStrength = 12f;
    public float turnStrength = 95f;

    public float rpm = 900f;
    public float idleRpm = 900f;
    public float maxRpm = 7000f;
    public float stallRpm = 500f;

    public float speedMph = 0f;
    public bool engineOn = true;
    public bool clutchPressed = false;

    public float[] gearSpeedLimits = { 0, 12, 22, 34, 48, 65 };
    public float[] gearTorque = { 0, 1.8f, 1.35f, 1.05f, 0.8f, 0.6f };

    private GameObject player;
    private CharacterController playerController;
    private GameObject car;
    private Rigidbody carRb;
    private GameObject shop;
    private Camera cam;

    private AudioSource audioSource;
    private AudioSource engineAudio;

    private DeliveryHouse currentTarget;
    private bool hasFood = false;
    private bool inCar = false;

    private float cameraPitch = 0f;
    private float cameraYaw = 0f;
    private float verticalVelocity = 0f;

    [ContextMenu("Generate Playable Demo")]
    public void GenerateDemo()
    {
        ClearDemo();

        CreateAudio();
        CreateGround();
        CreateRoads();
        CreateShop();
        CreateHouses();
        CreateCar();
        CreatePlayer();

        Debug.Log("Demo created. Press Play.");
    }

    [ContextMenu("Clear Demo")]
    public void ClearDemo()
    {
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }

    void Start()
    {
        FindObjects();

        if (Application.isPlaying)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        if (!Application.isPlaying) return;

        clutchPressed = Input.GetKey(KeyCode.LeftShift);

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (inCar)
                GearUp();
            else
                Interact();
        }

        if (Input.GetKeyDown(KeyCode.Q) && inCar)
        {
            GearDown();
        }

        if (Input.GetKeyDown(KeyCode.R) && inCar)
        {
            engineOn = true;
            rpm = idleRpm;
            PlayTone(500f, 0.12f);
            Debug.Log("Engine restarted.");
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleCar();
        }

        if (inCar)
        {
            CarCameraLook();
        }
        else
        {
            PlayerLook();
            PlayerMove();
        }
    }

    void FixedUpdate()
    {
        if (!Application.isPlaying) return;

        if (inCar)
        {
            DriveCar();
            FakeSuspension();
        }
    }

    void OnGUI()
    {
        if (!Application.isPlaying) return;

        GUI.Box(new Rect(10, 10, 430, 235), "");

        GUI.Label(new Rect(25, 25, 390, 25), "Money: £" + money);
        GUI.Label(new Rect(25, 50, 390, 25), "Food picked up: " + (hasFood ? "YES" : "NO"));
        GUI.Label(new Rect(25, 75, 390, 25), "Mode: " + (inCar ? "Driving" : "Walking"));

        string gearText = gear == 0 ? "N" : gear.ToString();
        GUI.Label(new Rect(25, 100, 390, 25), "Gear: " + gearText);
        GUI.Label(new Rect(25, 125, 390, 25), "Speed: " + Mathf.RoundToInt(speedMph) + " mph");
        GUI.Label(new Rect(25, 150, 390, 25), "RPM: " + Mathf.RoundToInt(rpm));
        GUI.Label(new Rect(25, 175, 390, 25), "Engine: " + (engineOn ? "ON" : "STALLED / OFF"));

        string target = currentTarget == null ? "None" : currentTarget.houseName;
        GUI.Label(new Rect(25, 200, 390, 25), "Deliver to: " + target);

        GUI.Box(new Rect(Screen.width - 350, 10, 340, 160), "");
        GUI.Label(new Rect(Screen.width - 335, 25, 320, 25), "MANUAL GEARBOX");
        GUI.Label(new Rect(Screen.width - 335, 50, 320, 25), "Left Shift = clutch");
        GUI.Label(new Rect(Screen.width - 335, 75, 320, 25), "E = gear up | Q = gear down");
        GUI.Label(new Rect(Screen.width - 335, 100, 320, 25), "R = restart engine if stalled");
        GUI.Label(new Rect(Screen.width - 335, 125, 320, 25), "Clutch: " + (clutchPressed ? "DOWN" : "UP"));
    }

    void FindObjects()
    {
        player = GameObject.Find("Player");
        car = GameObject.Find("Manual Delivery Car");
        shop = GameObject.Find("Takeaway Shop");

        if (player != null)
        {
            playerController = player.GetComponent<CharacterController>();
            cam = player.GetComponentInChildren<Camera>();
        }

        if (car != null)
            carRb = car.GetComponent<Rigidbody>();

        AudioSource[] sources = GetComponents<AudioSource>();

        if (sources.Length >= 2)
        {
            audioSource = sources[0];
            engineAudio = sources[1];
        }
        else
        {
            CreateAudio();
        }
    }

    void CreateAudio()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        engineAudio = gameObject.AddComponent<AudioSource>();
        engineAudio.loop = true;
        engineAudio.playOnAwake = false;
        engineAudio.clip = CreateTone(120f, 1f);
        engineAudio.volume = 0.25f;
    }

    void CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.SetParent(transform);
        ground.transform.position = new Vector3(0, -0.05f, 0);
        ground.transform.localScale = new Vector3(citySize + 50f, 0.1f, citySize + 50f);
        ground.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Grass", new Color(0.18f, 0.45f, 0.18f));
    }

    void CreateRoads()
    {
        Material roadMat = MakeMaterial("Road", new Color(0.04f, 0.04f, 0.04f));

        for (int i = -2; i <= 2; i++)
        {
            GameObject roadH = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roadH.name = "Road Horizontal";
            roadH.transform.SetParent(transform);
            roadH.transform.position = new Vector3(0, 0.002f, i * 20);
            roadH.transform.localScale = new Vector3(citySize, 0.01f, 8f);
            roadH.GetComponent<Renderer>().sharedMaterial = roadMat;

            Collider roadHCollider = roadH.GetComponent<Collider>();
            if (roadHCollider != null)
                DestroyImmediate(roadHCollider);

            GameObject roadV = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roadV.name = "Road Vertical";
            roadV.transform.SetParent(transform);
            roadV.transform.position = new Vector3(i * 20, 0.003f, 0);
            roadV.transform.localScale = new Vector3(8f, 0.01f, citySize);
            roadV.GetComponent<Renderer>().sharedMaterial = roadMat;

            Collider roadVCollider = roadV.GetComponent<Collider>();
            if (roadVCollider != null)
                DestroyImmediate(roadVCollider);
        }
    }

    void CreateShop()
    {
        shop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shop.name = "Takeaway Shop";
        shop.transform.SetParent(transform);
        shop.transform.position = new Vector3(0, 3f, -45);
        shop.transform.localScale = new Vector3(14f, 6f, 10f);
        shop.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Shop Orange", new Color(1f, 0.45f, 0.05f));

        GameObject pickupZone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pickupZone.name = "Pickup Zone";
        pickupZone.transform.SetParent(shop.transform);
        pickupZone.transform.localPosition = new Vector3(0, -0.45f, -0.9f);
        pickupZone.transform.localScale = new Vector3(0.8f, 0.08f, 0.3f);
        pickupZone.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Pickup Green", new Color(0f, 0.9f, 0.1f));

        GameObject spawnPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        spawnPoint.name = "Car Spawn Point";
        spawnPoint.transform.SetParent(transform);
        spawnPoint.transform.position = new Vector3(0, 0.5f, -32);
        spawnPoint.transform.localScale = new Vector3(1f, 1f, 1f);
        spawnPoint.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Spawn Blue", new Color(0f, 0.4f, 1f));

        Collider spawnCol = spawnPoint.GetComponent<Collider>();
        if (spawnCol != null)
            spawnCol.isTrigger = true;
    }

    void CreateHouses()
    {
        Material houseMat = MakeMaterial("House", new Color(0.65f, 0.65f, 0.65f));
        Material doorMat = MakeMaterial("Door", new Color(0.35f, 0.18f, 0.05f));

        for (int i = 1; i <= houseCount; i++)
        {
            Vector3 pos = GetRandomHousePosition();

            GameObject house = GameObject.CreatePrimitive(PrimitiveType.Cube);
            house.name = "Customer House " + i;
            house.transform.SetParent(transform);
            house.transform.position = pos;

            float width = Random.Range(5f, 8f);
            float height = Random.Range(4f, 8f);
            float depth = Random.Range(5f, 8f);

            house.transform.localScale = new Vector3(width, height, depth);
            house.GetComponent<Renderer>().sharedMaterial = houseMat;

            DeliveryHouse h = house.AddComponent<DeliveryHouse>();
            h.houseName = house.name;

            GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "Door Bell";
            door.transform.SetParent(house.transform);
            door.transform.localPosition = new Vector3(0, -0.25f, -0.52f);
            door.transform.localScale = new Vector3(0.25f, 0.45f, 0.05f);
            door.GetComponent<Renderer>().sharedMaterial = doorMat;
        }
    }

    Vector3 GetRandomHousePosition()
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            float x = Random.Range(-42f, 42f);
            float z = Random.Range(-20f, 35f);

            if (Mathf.Abs(x % 20f) < 6f) x += 9f;
            if (Mathf.Abs(z % 20f) < 6f) z += 9f;

            Vector3 pos = new Vector3(x, 3f, z);

            Vector3 shopPos = new Vector3(0, 3f, -45f);
            Vector3 carSpawnPos = new Vector3(0f, 1.2f, -32f);

            if (Vector3.Distance(pos, shopPos) < 28f)
                continue;

            if (Vector3.Distance(pos, carSpawnPos) < 24f)
                continue;

            return pos;
        }

        return new Vector3(Random.Range(-40f, 40f), 3f, Random.Range(5f, 35f));
    }

    void CreateCar()
    {
        car = new GameObject("Manual Delivery Car");
        car.transform.SetParent(transform);
        car.transform.position = new Vector3(0, 1.2f, -32f);
        car.transform.rotation = Quaternion.identity;

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Car Body";
        body.transform.SetParent(car.transform);
        body.transform.localPosition = new Vector3(0, 0, 0);
        body.transform.localScale = new Vector3(2.2f, 0.7f, 4.2f);
        body.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Car Red", new Color(0.9f, 0.05f, 0.05f));

        GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabin.name = "Car Cabin";
        cabin.transform.SetParent(car.transform);
        cabin.transform.localPosition = new Vector3(0, 0.65f, -0.1f);
        cabin.transform.localScale = new Vector3(1.8f, 0.75f, 2.1f);
        cabin.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Cabin Dark", new Color(0.2f, 0.2f, 0.25f));

        GameObject frontBumper = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frontBumper.name = "Front Bumper";
        frontBumper.transform.SetParent(car.transform);
        frontBumper.transform.localPosition = new Vector3(0, -0.05f, 2.15f);
        frontBumper.transform.localScale = new Vector3(2.0f, 0.35f, 0.2f);
        frontBumper.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Bumper", new Color(0.1f, 0.1f, 0.1f));

        GameObject rearBumper = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rearBumper.name = "Rear Bumper";
        rearBumper.transform.SetParent(car.transform);
        rearBumper.transform.localPosition = new Vector3(0, -0.05f, -2.15f);
        rearBumper.transform.localScale = new Vector3(2.0f, 0.35f, 0.2f);
        rearBumper.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Bumper", new Color(0.1f, 0.1f, 0.1f));

        GameObject windscreen = GameObject.CreatePrimitive(PrimitiveType.Cube);
        windscreen.name = "Windscreen";
        windscreen.transform.SetParent(car.transform);
        windscreen.transform.localPosition = new Vector3(0, 0.8f, 0.55f);
        windscreen.transform.localScale = new Vector3(1.6f, 0.45f, 0.08f);
        windscreen.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Glass", new Color(0.4f, 0.7f, 0.9f));

        GameObject rearWindow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rearWindow.name = "Rear Window";
        rearWindow.transform.SetParent(car.transform);
        rearWindow.transform.localPosition = new Vector3(0, 0.8f, -0.75f);
        rearWindow.transform.localScale = new Vector3(1.6f, 0.4f, 0.08f);
        rearWindow.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Glass", new Color(0.4f, 0.7f, 0.9f));

        DestroyImmediate(body.GetComponent<Collider>());
        DestroyImmediate(cabin.GetComponent<Collider>());
        DestroyImmediate(frontBumper.GetComponent<Collider>());
        DestroyImmediate(rearBumper.GetComponent<Collider>());
        DestroyImmediate(windscreen.GetComponent<Collider>());
        DestroyImmediate(rearWindow.GetComponent<Collider>());

        BoxCollider mainCol = car.AddComponent<BoxCollider>();
        mainCol.center = new Vector3(0, 0.25f, 0);
        mainCol.size = new Vector3(2.2f, 1.2f, 4.2f);

        carRb = car.AddComponent<Rigidbody>();
        carRb.mass = 1000f;
        carRb.linearDamping = 0.25f;
        carRb.angularDamping = 3f;
        carRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        carRb.interpolation = RigidbodyInterpolation.Interpolate;
        carRb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        CreateWheel(new Vector3(-1.05f, -0.45f, 1.35f));
        CreateWheel(new Vector3(1.05f, -0.45f, 1.35f));
        CreateWheel(new Vector3(-1.05f, -0.45f, -1.35f));
        CreateWheel(new Vector3(1.05f, -0.45f, -1.35f));
    }

    void CreateWheel(Vector3 localPos)
    {
        GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        wheel.name = "Wheel";
        wheel.transform.SetParent(car.transform);
        wheel.transform.localPosition = localPos;
        wheel.transform.localRotation = Quaternion.Euler(0, 0, 90);
        wheel.transform.localScale = new Vector3(0.45f, 0.18f, 0.45f);
        wheel.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Wheel Black", new Color(0.03f, 0.03f, 0.03f));

        Collider col = wheel.GetComponent<Collider>();
        if (col != null)
            DestroyImmediate(col);
    }

    void CreatePlayer()
    {
        player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.SetParent(transform);
        player.transform.position = new Vector3(0, 1.2f, -35);
        player.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Player Blue", new Color(0.05f, 0.2f, 1f));

        CapsuleCollider oldCollider = player.GetComponent<CapsuleCollider>();
        if (oldCollider != null)
            DestroyImmediate(oldCollider);

        playerController = player.AddComponent<CharacterController>();
        playerController.height = 2f;
        playerController.radius = 0.35f;
        playerController.stepOffset = 0.45f;

        GameObject cameraObject = new GameObject("Player Camera");
        cameraObject.transform.SetParent(player.transform);
        cameraObject.transform.localPosition = new Vector3(0, 0.65f, 0);
        cam = cameraObject.AddComponent<Camera>();
        cam.tag = "MainCamera";
    }

    void PlayerLook()
    {
        if (player == null || cam == null) return;

        float mouseX = Input.GetAxis("Mouse X") * 3f;
        float mouseY = Input.GetAxis("Mouse Y") * 3f;

        player.transform.Rotate(0, mouseX, 0);

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -75f, 75f);

        cam.transform.localRotation = Quaternion.Euler(cameraPitch, 0, 0);
    }

    void CarCameraLook()
    {
        if (cam == null) return;

        float mouseX = Input.GetAxis("Mouse X") * 2f;
        float mouseY = Input.GetAxis("Mouse Y") * 2f;

        cameraYaw += mouseX;
        cameraPitch -= mouseY;

        cameraYaw = Mathf.Clamp(cameraYaw, -90f, 90f);
        cameraPitch = Mathf.Clamp(cameraPitch, -40f, 55f);

        cam.transform.localRotation = Quaternion.Euler(cameraPitch, cameraYaw, 0);
    }

    void PlayerMove()
    {
        if (playerController == null || player == null) return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = player.transform.forward * v + player.transform.right * h;
        move *= 6f;

        if (playerController.isGrounded)
            verticalVelocity = -1f;
        else
            verticalVelocity -= 20f * Time.deltaTime;

        move.y = verticalVelocity;

        playerController.Move(move * Time.deltaTime);
    }

    void ToggleCar()
    {
        if (player == null || car == null || cam == null) return;

        float distance = Vector3.Distance(player.transform.position, car.transform.position);

        if (!inCar && distance < 4f)
        {
            inCar = true;
            player.SetActive(false);

            cam.transform.SetParent(car.transform);
            cam.transform.localPosition = new Vector3(0, 2.1f, -5f);
            cam.transform.localRotation = Quaternion.Euler(12f, 0, 0);

            cameraPitch = 12f;
            cameraYaw = 0f;

            PlayTone(500f, 0.08f);
        }
        else if (inCar)
        {
            inCar = false;
            player.SetActive(true);

            player.transform.position = car.transform.position + car.transform.right * 3f + Vector3.up * 1f;

            cam.transform.SetParent(player.transform);
            cam.transform.localPosition = new Vector3(0, 0.65f, 0);
            cam.transform.localRotation = Quaternion.identity;

            cameraPitch = 0f;
            cameraYaw = 0f;

            PlayTone(350f, 0.08f);
        }
    }

    void DriveCar()
    {
        if (carRb == null || car == null) return;

        float throttle = Input.GetAxis("Vertical");
        float steering = Input.GetAxis("Horizontal");

        speedMph = carRb.linearVelocity.magnitude * 2.237f;

        UpdateEngine(throttle);

        if (!engineOn)
            return;

        if (gear == 0)
        {
            if (throttle < 0)
                carRb.AddForce(-car.transform.forward * brakeStrength, ForceMode.Acceleration);

            return;
        }

        float maxSpeedThisGear = gearSpeedLimits[gear];

        bool wrongHighGearAtLowSpeed =
            gear >= 3 && speedMph < 8f && !clutchPressed && throttle > 0.2f;

        if (wrongHighGearAtLowSpeed)
        {
            rpm -= 900f * Time.fixedDeltaTime;

            if (rpm < stallRpm)
            {
                engineOn = false;
                rpm = 0f;
                PlayTone(120f, 0.2f);
                Debug.Log("Engine stalled. You tried to pull away in too high gear.");
            }

            return;
        }

        if (throttle > 0 && speedMph < maxSpeedThisGear && !clutchPressed)
        {
            float torque = acceleration * gearTorque[gear];
            carRb.AddForce(car.transform.forward * throttle * torque, ForceMode.Acceleration);
        }

        if (throttle < 0)
        {
            if (Vector3.Dot(carRb.linearVelocity, car.transform.forward) > 1f)
                carRb.AddForce(-car.transform.forward * brakeStrength, ForceMode.Acceleration);
            else
                carRb.AddForce(car.transform.forward * throttle * reverseAcceleration, ForceMode.Acceleration);
        }

        if (carRb.linearVelocity.magnitude > 0.5f)
        {
            float turnAmount = steering * turnStrength * Time.fixedDeltaTime;
            carRb.MoveRotation(carRb.rotation * Quaternion.Euler(0, turnAmount, 0));
        }

        float maxSpeedMeters = maxSpeedThisGear / 2.237f;
        carRb.linearVelocity = Vector3.ClampMagnitude(carRb.linearVelocity, maxSpeedMeters);
    }

    void UpdateEngine(float throttle)
    {
        if (carRb == null) return;

        speedMph = carRb.linearVelocity.magnitude * 2.237f;

        if (!engineOn)
        {
            rpm = 0f;
            UpdateEngineSound();
            return;
        }

        if (gear == 0 || clutchPressed)
        {
            float targetRpm = idleRpm + Mathf.Abs(throttle) * 4500f;
            rpm = Mathf.Lerp(rpm, targetRpm, Time.fixedDeltaTime * 4f);
        }
        else
        {
            float gearRatio = Mathf.InverseLerp(0f, gearSpeedLimits[gear], speedMph);
            float targetRpm = Mathf.Lerp(1000f, maxRpm, gearRatio);

            if (Input.GetAxis("Vertical") > 0.1f)
                targetRpm += 700f;

            rpm = Mathf.Lerp(rpm, targetRpm, Time.fixedDeltaTime * 5f);
        }

        rpm = Mathf.Clamp(rpm, 0f, maxRpm);

        UpdateEngineSound();
    }

    void FakeSuspension()
    {
        if (carRb == null || car == null) return;

        Ray ray = new Ray(car.transform.position + Vector3.up * 0.5f, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, 2.2f))
        {
            float targetHeight = 1.1f;
            float currentHeight = hit.distance;
            float lift = targetHeight - currentHeight;

            if (lift > 0f)
                carRb.AddForce(Vector3.up * lift * 35f, ForceMode.Acceleration);
        }
    }

    void GearUp()
    {
        if (!clutchPressed)
        {
            Debug.Log("Press Left Shift clutch to change gear.");
            PlayTone(180f, 0.08f);
            return;
        }

        gear = Mathf.Clamp(gear + 1, 0, maxGear);
        rpm *= 0.65f;

        PlayTone(700f, 0.05f);
        Debug.Log("Gear up: " + (gear == 0 ? "N" : gear.ToString()));
    }

    void GearDown()
    {
        if (!clutchPressed)
        {
            Debug.Log("Press Left Shift clutch to change gear.");
            PlayTone(180f, 0.08f);
            return;
        }

        gear = Mathf.Clamp(gear - 1, 0, maxGear);
        rpm *= 1.25f;

        if (rpm > maxRpm)
            rpm = maxRpm;

        PlayTone(450f, 0.05f);
        Debug.Log("Gear down: " + (gear == 0 ? "N" : gear.ToString()));
    }

    void Interact()
    {
        if (player == null || shop == null) return;

        float shopDistance = Vector3.Distance(player.transform.position, shop.transform.position);

        if (shopDistance < interactDistance)
        {
            PickUpFood();
            return;
        }

        if (currentTarget != null)
        {
            float houseDistance = Vector3.Distance(player.transform.position, currentTarget.transform.position);

            if (houseDistance < interactDistance)
            {
                CompleteDelivery();
                return;
            }
        }

        PlayTone(200f, 0.08f);
        Debug.Log("Nothing to interact with.");
    }

    void PickUpFood()
    {
        if (hasFood)
        {
            Debug.Log("You already have food.");
            PlayTone(200f, 0.08f);
            return;
        }

        DeliveryHouse[] houses = FindObjectsByType<DeliveryHouse>(FindObjectsSortMode.None);

        if (houses.Length == 0)
        {
            Debug.Log("No houses found.");
            return;
        }

        currentTarget = houses[Random.Range(0, houses.Length)];
        hasFood = true;

        CreateCustomerMarker(currentTarget);

        PlayTone(650f, 0.1f);
        Debug.Log("Food picked up. Deliver to: " + currentTarget.houseName);
    }

    void CompleteDelivery()
    {
        if (!hasFood || currentTarget == null) return;

        money += rewardPerDelivery;
        hasFood = false;

        PlayChaChing();

        Transform oldMarker = currentTarget.transform.Find("ACTIVE DELIVERY MARKER");
        if (oldMarker != null)
            Destroy(oldMarker.gameObject);

        currentTarget = null;
    }

    void CreateCustomerMarker(DeliveryHouse house)
    {
        Transform oldMarker = house.transform.Find("ACTIVE DELIVERY MARKER");
        if (oldMarker != null)
            DestroyImmediate(oldMarker.gameObject);

        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "ACTIVE DELIVERY MARKER";
        marker.transform.SetParent(house.transform);
        marker.transform.localPosition = new Vector3(0, 1.2f, 0);
        marker.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
        marker.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Marker Green", new Color(0f, 1f, 0.1f));
    }

    void PlayTone(float frequency, float duration)
    {
        if (audioSource == null) return;

        AudioClip clip = CreateTone(frequency, duration);
        audioSource.PlayOneShot(clip);
    }

    void PlayChaChing()
    {
        PlayTone(900f, 0.08f);
        Invoke(nameof(SecondCashSound), 0.09f);
    }

    void SecondCashSound()
    {
        PlayTone(1200f, 0.12f);
    }

    void UpdateEngineSound()
    {
        if (engineAudio == null) return;

        if (!engineAudio.isPlaying)
            engineAudio.Play();

        if (!engineOn)
        {
            engineAudio.volume = Mathf.Lerp(engineAudio.volume, 0f, Time.deltaTime * 5f);
            return;
        }

        engineAudio.volume = 0.15f + Mathf.InverseLerp(idleRpm, maxRpm, rpm) * 0.35f;
        engineAudio.pitch = 0.7f + Mathf.InverseLerp(idleRpm, maxRpm, rpm) * 1.8f;
    }

    AudioClip CreateTone(float frequency, float duration)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * 0.25f;

        AudioClip clip = AudioClip.Create("Tone", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    Material MakeMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        Material mat = new Material(shader);
        mat.name = name;

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);

        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);

        return mat;
    }
}

public class DeliveryHouse : MonoBehaviour
{
    public string houseName;
}