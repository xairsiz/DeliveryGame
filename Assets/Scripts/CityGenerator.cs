using System.Collections.Generic;
using UnityEngine;

/// [ExecuteAlways] scene generator.
/// Right-click this component → "Generate City" to build the entire map
/// in the editor. The result is editable just like any normal scene.
///
/// House prefabs used:  Assets/Palmov Island/Low Poly Houses Free Pack/Prefabs/Houses/
/// Only residential-scale prefabs are in the default list.
/// Avoid assigning: city hall, catholic temple (landmarks — too large).
[ExecuteAlways]
public class CityGenerator : MonoBehaviour
{
    [Header("Houses")]
    public GameObject[] housePrefabs;        // assign in inspector; auto-filled if left empty
    public float        houseTargetWidth = 8f;  // rescale each prefab to this footprint

    [Header("City")]
    public int  houseCount = 16;
    public bool fixURPMaterials = true;   // auto-upgrade Built-in → URP shaders

    // ── Context menus ─────────────────────────────────────────────────────────
    [ContextMenu("Generate City")]
    public void GenerateCity()
    {
        ClearCity();

        if (housePrefabs == null || housePrefabs.Length == 0)
            TryLoadDefaultPrefabs();

        BuildGround();
        BuildRoads();
        BuildShop();
        BuildHouses();
        BuildCar();
        BuildPlayer();
        BuildGameManager();

        Debug.Log("[CityGenerator] Done — press Play to start the game.");
    }

    [ContextMenu("Clear City")]
    public void ClearCity()
    {
        while (transform.childCount > 0)
            DestroyImmediate(transform.GetChild(0).gameObject);
    }

    // ── Ground ────────────────────────────────────────────────────────────────
    void BuildGround()
    {
        var g = Primitive("Ground", new Vector3(0, -0.1f, 0), new Vector3(200f, 0.2f, 200f),
                           Mat(new Color(0.18f, 0.42f, 0.16f)));  // grass
        g.transform.SetParent(transform);
    }

    // ── Roads ─────────────────────────────────────────────────────────────────
    // Layout:  N-S avenue at x = 0 (10m wide)
    //          E-W streets at z = -20 and z = 22 (8m wide)
    void BuildRoads()
    {
        var roadMat = Mat(new Color(0.08f, 0.08f, 0.08f));
        float len = 160f;

        Road("Ave N-S",  new Vector3(0f,      0.05f, 0f),  new Vector3(10f, 0.1f, len),  roadMat);
        Road("Street S", new Vector3(0f,      0.06f, -20f), new Vector3(len, 0.1f, 8f),   roadMat);
        Road("Street N", new Vector3(0f,      0.06f,  22f), new Vector3(len, 0.1f, 8f),   roadMat);

        // Kerb lines (thin white strips along avenue)
        var kerbMat = Mat(new Color(0.85f, 0.85f, 0.85f));
        Road("Kerb L",  new Vector3(-5.1f, 0.07f, 0f), new Vector3(0.2f, 0.1f, len), kerbMat);
        Road("Kerb R",  new Vector3( 5.1f, 0.07f, 0f), new Vector3(0.2f, 0.1f, len), kerbMat);
    }

    void Road(string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var r = Primitive(name, pos, scale, mat);
        r.transform.SetParent(transform);
        DestroyImmediate(r.GetComponent<Collider>());
    }

    // ── Shop ─────────────────────────────────────────────────────────────────
    // South of the map, centred on the avenue.  Player picks up orders here.
    void BuildShop()
    {
        var root = new GameObject("Takeaway Shop");
        root.transform.SetParent(transform);
        root.transform.position = new Vector3(0f, 0f, -62f);

        // Main body
        var bodyMat = Mat(new Color(1f, 0.42f, 0.04f));
        var body    = Primitive("Body", new Vector3(0, 3f, 0), new Vector3(14f, 6f, 10f), bodyMat);
        body.transform.SetParent(root.transform);

        // Flat roof
        var roofMat = Mat(new Color(0.15f, 0.15f, 0.15f));
        var roof    = Primitive("Roof", new Vector3(0, 6.1f, 0), new Vector3(14.4f, 0.2f, 10.4f), roofMat);
        roof.transform.SetParent(root.transform);
        DestroyImmediate(roof.GetComponent<Collider>());

        // Awning
        var awningMat = Mat(new Color(1f, 0.15f, 0.1f));
        var awning    = Primitive("Awning", new Vector3(0, 4.6f, -5.3f), new Vector3(13f, 0.15f, 2.5f), awningMat);
        awning.transform.SetParent(root.transform);
        DestroyImmediate(awning.GetComponent<Collider>());

        // Emissive sign
        var signMat = Mat(new Color(1f, 0.9f, 0.0f), emissive: true);
        var sign    = Primitive("Sign", new Vector3(0, 5.5f, -5.1f), new Vector3(10f, 0.9f, 0.1f), signMat);
        sign.transform.SetParent(root.transform);
        DestroyImmediate(sign.GetComponent<Collider>());

        // Counter inside (visible through door gap)
        var counterMat = Mat(new Color(0.55f, 0.35f, 0.1f));
        var counter    = Primitive("Counter", new Vector3(0, 1.1f, 1.5f), new Vector3(10f, 0.9f, 0.9f), counterMat);
        counter.transform.SetParent(root.transform);

        // Pickup zone marker on floor
        var zoneMat = Mat(new Color(0.0f, 0.85f, 0.15f), emissive: true);
        var zone    = Primitive("PickupZone", new Vector3(0, 0.02f, -5.5f), new Vector3(4f, 0.04f, 3f), zoneMat);
        zone.transform.SetParent(root.transform);
        DestroyImmediate(zone.GetComponent<Collider>());
    }

    // ── Houses ────────────────────────────────────────────────────────────────
    // 4 columns (x = ±14, ±28) × 5 rows (z = -40,-8,2,32,46) = 20 valid lots.
    // Columns are outside the avenue (|x| > 5), rows are outside E-W roads (z ≠ -20±4 or 22±4).
    static readonly Vector2[] LotCenters =
    {
        // south band (z < -24, well clear of z=-20 road)
        new(-28,-42), new(-14,-42), new(14,-42), new(28,-42),
        new(-28,-32), new(-14,-32), new(14,-32), new(28,-32),
        // middle band (-16 < z < 18, between roads)
        new(-28, -8), new(-14, -8), new(14,  -8), new(28,  -8),
        new(-28,  5), new(-14,  5), new(14,   5), new(28,   5),
        // north band (z > 26)
        new(-28, 34), new(-14, 34), new(14,  34), new(28,  34),
    };

    void BuildHouses()
    {
        // Shuffle lot list
        var lots = new List<Vector2>(LotCenters);
        for (int i = lots.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (lots[i], lots[j]) = (lots[j], lots[i]);
        }

        int count = Mathf.Min(houseCount, lots.Count);
        for (int i = 0; i < count; i++)
            PlaceHouse(i + 1, lots[i]);
    }

    void PlaceHouse(int index, Vector2 lot)
    {
        Vector3 pos = new Vector3(lot.x, 0f, lot.y);

        GameObject house;

        if (housePrefabs != null && housePrefabs.Length > 0)
        {
            GameObject prefab = housePrefabs[Random.Range(0, housePrefabs.Length)];
#if UNITY_EDITOR
            house = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, transform);
#else
            house = Instantiate(prefab, transform);
#endif
            house.transform.position = pos;
            // Face the avenue (positive x lots face left, negative x face right)
            house.transform.rotation = Quaternion.Euler(0f, lot.x < 0 ? 90f : -90f, 0f);

            NormalisePrefabSize(house, houseTargetWidth);
            GroundHouse(house);

            if (fixURPMaterials)
                UpgradeMaterials(house);
        }
        else
        {
            // Fallback: plain cube house
            house = new GameObject("House Fallback");
            house.transform.SetParent(transform);
            house.transform.position = pos + Vector3.up * 3f;

            var body = Primitive("Body", Vector3.zero, new Vector3(7f, 6f, 7f), Mat(new Color(0.72f, 0.72f, 0.68f)));
            body.transform.SetParent(house.transform);

            var roofMat = Mat(new Color(0.48f, 0.22f, 0.18f));
            for (int s = 0; s < 4; s++)
            {
                float angle   = s * 90f;
                var   slab    = Primitive("Roof" + s, Vector3.zero, new Vector3(8f, 0.15f, 5f), roofMat);
                slab.transform.SetParent(house.transform);
                slab.transform.localPosition = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * 1.5f, 3.3f, Mathf.Cos(angle * Mathf.Deg2Rad) * 1.5f);
                slab.transform.localRotation = Quaternion.Euler(25f * (s % 2 == 0 ? 1 : -1), angle, 0f);
                DestroyImmediate(slab.GetComponent<Collider>());
            }

            // Windows (emissive)
            var winMat = Mat(new Color(1f, 0.95f, 0.6f), emissive: true);
            for (int w = -1; w <= 1; w += 2)
            {
                var win = Primitive("Window", Vector3.zero, new Vector3(1.2f, 1.0f, 0.08f), winMat);
                win.transform.SetParent(house.transform);
                win.transform.localPosition = new Vector3(w * 2f, 0.2f, -3.55f);
                DestroyImmediate(win.GetComponent<Collider>());
            }

            // Door
            var doorMat = Mat(new Color(0.32f, 0.16f, 0.05f));
            var door    = Primitive("Door", Vector3.zero, new Vector3(0.9f, 2.0f, 0.1f), doorMat);
            door.transform.SetParent(house.transform);
            door.transform.localPosition = new Vector3(0f, -1f, -3.55f);
            DestroyImmediate(door.GetComponent<Collider>());

            EnsureBoxCollider(house, new Vector3(7f, 6f, 7f));
        }

        house.name = "Customer House " + index;

        // DeliveryHouse component
        var dh = house.AddComponent<DeliveryHouse>();
        dh.houseName    = "House " + index;
        dh.doorWorldPos = house.transform.position + house.transform.forward * -3.8f + Vector3.up * 1.2f;

        // Delivery beacon (a glowing sphere above the house, hidden until activated)
        var beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        beacon.name = "__Beacon";
        beacon.transform.SetParent(house.transform);
        beacon.transform.localPosition = new Vector3(0f, 7f, 0f);
        beacon.transform.localScale    = Vector3.one * 1.4f;
        beacon.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0f, 1f, 0.2f), emissive: true);
        DestroyImmediate(beacon.GetComponent<Collider>());
        beacon.SetActive(false);
    }

    // ── Car ───────────────────────────────────────────────────────────────────
    void BuildCar()
    {
        var car = new GameObject("Manual Delivery Car");
        car.transform.SetParent(transform);
        car.transform.position = new Vector3(0f, 1.2f, -48f);
        car.transform.rotation = Quaternion.identity;

        // Visual parts (no colliders — compound single box on root)
        var red    = Mat(new Color(0.88f, 0.06f, 0.06f));
        var dark   = Mat(new Color(0.15f, 0.15f, 0.18f));
        var grey   = Mat(new Color(0.35f, 0.35f, 0.35f));
        var black  = Mat(new Color(0.04f, 0.04f, 0.04f));
        var orange = Mat(new Color(1f, 0.55f, 0f), emissive: true);

        AddVisual(car, "Body",      new Vector3(0,  0.00f,  0.0f), new Vector3(2.2f, 0.65f, 4.4f), red);
        AddVisual(car, "Cabin",     new Vector3(0,  0.68f, -0.2f), new Vector3(1.85f, 0.72f, 2.2f), dark);
        AddVisual(car, "Bumper F",  new Vector3(0, -0.05f,  2.2f), new Vector3(2.0f,  0.28f, 0.18f), grey);
        AddVisual(car, "Bumper R",  new Vector3(0, -0.05f, -2.2f), new Vector3(2.0f,  0.28f, 0.18f), grey);
        AddVisual(car, "Headlight L", new Vector3(-0.85f, 0.05f, 2.26f), new Vector3(0.4f, 0.22f, 0.08f), orange);
        AddVisual(car, "Headlight R", new Vector3( 0.85f, 0.05f, 2.26f), new Vector3(0.4f, 0.22f, 0.08f), orange);

        // Delivery box on roof
        var boxMat = Mat(new Color(0.9f, 0.75f, 0.1f));
        AddVisual(car, "DeliveryBox",  new Vector3(0, 1.16f, -0.5f), new Vector3(1.3f, 0.85f, 1.5f), boxMat);
        AddVisual(car, "BoxLid",       new Vector3(0, 1.60f, -0.5f), new Vector3(1.35f, 0.08f, 1.55f), boxMat);

        // Wheels
        AddWheel(car, new Vector3(-1.1f, -0.42f,  1.4f), black);
        AddWheel(car, new Vector3( 1.1f, -0.42f,  1.4f), black);
        AddWheel(car, new Vector3(-1.1f, -0.42f, -1.4f), black);
        AddWheel(car, new Vector3( 1.1f, -0.42f, -1.4f), black);

        // Physics
        var col  = car.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 0.28f, 0f);
        col.size   = new Vector3(2.2f, 1.2f, 4.4f);

        var rb = car.AddComponent<Rigidbody>();  // ManualCarController's Awake will configure it
        car.AddComponent<ManualCarController>();

        // No camera here — the player's camera reparents onto the car when driving.
    }

    void AddVisual(GameObject parent, string name, Vector3 lp, Vector3 ls, Material mat)
    {
        var g = Primitive(name, Vector3.zero, ls, mat);
        g.transform.SetParent(parent.transform);
        g.transform.localPosition = lp;
        DestroyImmediate(g.GetComponent<Collider>());
    }

    void AddWheel(GameObject parent, Vector3 lp, Material mat)
    {
        var w = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        w.name = "Wheel";
        w.transform.SetParent(parent.transform);
        w.transform.localPosition = lp;
        w.transform.localRotation = Quaternion.Euler(0, 0, 90f);
        w.transform.localScale    = new Vector3(0.45f, 0.18f, 0.45f);
        w.GetComponent<Renderer>().sharedMaterial = mat;
        DestroyImmediate(w.GetComponent<Collider>());
    }

    // ── Player ────────────────────────────────────────────────────────────────
    void BuildPlayer()
    {
        var player = new GameObject("Player");
        player.transform.SetParent(transform);
        player.transform.position = new Vector3(3f, 1.2f, -50f);

        // Capsule mesh (visual only)
        var cap = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        cap.name = "Mesh";
        cap.transform.SetParent(player.transform);
        cap.transform.localPosition = Vector3.zero;
        cap.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0.1f, 0.25f, 0.9f));
        DestroyImmediate(cap.GetComponent<Collider>());

        var cc = player.AddComponent<CharacterController>();
        cc.height     = 1.85f;
        cc.radius     = 0.32f;
        cc.stepOffset = 0.4f;
        cc.center     = new Vector3(0f, 0.925f, 0f);

        // Camera
        var camObj = new GameObject("PlayerCamera");
        camObj.transform.SetParent(player.transform);
        camObj.transform.localPosition = new Vector3(0f, 1.7f, 0f);
        camObj.AddComponent<Camera>().tag = "MainCamera";
        camObj.AddComponent<AudioListener>();

        var pc = player.AddComponent<PlayerController>();
        pc.camTransform = camObj.transform;
    }

    // ── Game manager ──────────────────────────────────────────────────────────
    void BuildGameManager()
    {
        // Attach to this same GameObject so it's easy to find
        if (GetComponent<DeliveryGameManager>() == null)
            gameObject.AddComponent<DeliveryGameManager>();
    }

    // ── Prefab utilities ──────────────────────────────────────────────────────
    void TryLoadDefaultPrefabs()
    {
#if UNITY_EDITOR
        // Residential-scale prefabs from the Low Poly Houses Free Pack
        string basePath = "Assets/Palmov Island/Low Poly Houses Free Pack/Prefabs/Houses/";
        string[] names  = { "cute house", "big cottage 1 floor new", "pizzeria house", "post office" };

        var list = new List<GameObject>();
        foreach (string n in names)
        {
            var p = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(basePath + n + ".prefab");
            if (p != null) list.Add(p);
        }
        housePrefabs = list.ToArray();
        if (housePrefabs.Length > 0)
            Debug.Log($"[CityGenerator] Auto-loaded {housePrefabs.Length} house prefab(s).");
        else
            Debug.LogWarning("[CityGenerator] Could not find Low Poly Houses prefabs. Using fallback cubes.");
#endif
    }

    static void NormalisePrefabSize(GameObject house, float targetWidth)
    {
        var renderers = house.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);

        float biggestSide = Mathf.Max(b.size.x, b.size.z);
        if (biggestSide < 0.01f) return;

        house.transform.localScale *= (targetWidth / biggestSide);
    }

    static void GroundHouse(GameObject house)
    {
        var renderers = house.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);

        float bottomY = b.min.y;
        if (Mathf.Abs(bottomY) > 0.01f)
            house.transform.position += Vector3.up * (-bottomY);
    }

    static void EnsureBoxCollider(GameObject house, Vector3 size)
    {
        foreach (var c in house.GetComponentsInChildren<Collider>())
            DestroyImmediate(c);

        var box    = house.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, size.y * 0.5f, 0f);
        box.size   = size;
    }

    // ── URP material upgrade ──────────────────────────────────────────────────
    static void UpgradeMaterials(GameObject go)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) return;

        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            Material[] mats = r.sharedMaterials;
            bool changed    = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                if (mats[i].shader.name.StartsWith("Universal Render Pipeline")) continue;

                var upgraded = new Material(urpLit) { name = mats[i].name + "_URP" };

                if (mats[i].HasProperty("_MainTex") && mats[i].GetTexture("_MainTex") != null)
                    upgraded.SetTexture("_BaseMap", mats[i].GetTexture("_MainTex"));

                Color col = Color.white;
                if (mats[i].HasProperty("_Color"))   col = mats[i].GetColor("_Color");
                upgraded.SetColor("_BaseColor", col);

                mats[i]  = upgraded;
                changed   = true;
            }
            if (changed) r.sharedMaterials = mats;
        }
    }

    // ── Generic helpers ───────────────────────────────────────────────────────
    static GameObject Primitive(string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name;
        g.transform.position   = pos;
        g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = mat;
        return g;
    }

    static Material Mat(Color c, bool emissive = false)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color");
        var m = new Material(s) { name = $"M_{c.r:F1}_{c.g:F1}_{c.b:F1}" };
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color"))     m.SetColor("_Color", c);
        if (emissive)
        {
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor"))
                m.SetColor("_EmissionColor", c * 1.4f);
        }
        return m;
    }
}
