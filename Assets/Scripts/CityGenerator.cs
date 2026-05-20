using System.Collections.Generic;
using UnityEngine;

/// Builds the entire playable world from code. Called automatically by
/// GameBootstrap when you press Play — you never have to touch this.
///
/// House/tree prefabs come from the Low Poly Houses Free Pack. Only
/// residential-scale prefabs are used (no city hall / temple landmarks).
public class CityGenerator : MonoBehaviour
{
    [Header("Houses")]
    public GameObject[] housePrefabs;
    public float houseTargetWidth = 8f;

    [Header("City")]
    public int  houseCount      = 14;
    public bool fixURPMaterials = true;

    GameObject[] treePrefabs;

    // ── Entry point ─────────────────────────────────────────────────────────
    public void GenerateCity()
    {
        if (housePrefabs == null || housePrefabs.Length == 0)
            housePrefabs = LoadPrefabs("Assets/Palmov Island/Low Poly Houses Free Pack/Prefabs/Houses/",
                                       "cute house", "big cottage 1 floor new", "pizzeria house", "post office");

        treePrefabs = LoadPrefabs("Assets/Palmov Island/Low Poly Houses Free Pack/Prefabs/Trees/",
                                  "round tree", "cottage tree 1", "cottage tree 2", "fir tree 1");

        SetupEnvironment();
        BuildGround();
        BuildRoads();
        BuildStreetLamps();
        BuildShop();
        BuildHousesAndTrees();
        BuildPerimeterTrees();
        BuildCar();
        BuildPlayer();
        BuildGameManager();
    }

    // ── Environment: sun, sky, fog, ambient ───────────────────────────────────
    void SetupEnvironment()
    {
        var sunGO = new GameObject("Sun");
        sunGO.transform.SetParent(transform);
        sunGO.transform.rotation = Quaternion.Euler(48f, 35f, 0f);
        var sun = sunGO.AddComponent<Light>();
        sun.type      = LightType.Directional;
        sun.color     = new Color(1f, 0.96f, 0.86f);
        sun.intensity = 1.15f;
        sun.shadows   = LightShadows.Soft;

        Shader skyShader = Shader.Find("Skybox/Procedural");
        if (skyShader != null)
        {
            var sky = new Material(skyShader);
            if (sky.HasProperty("_SkyTint"))     sky.SetColor("_SkyTint", new Color(0.5f, 0.62f, 0.82f));
            if (sky.HasProperty("_GroundColor")) sky.SetColor("_GroundColor", new Color(0.32f, 0.32f, 0.30f));
            if (sky.HasProperty("_AtmosphereThickness")) sky.SetFloat("_AtmosphereThickness", 1.1f);
            RenderSettings.skybox = sky;
            RenderSettings.sun    = sun;
        }

        RenderSettings.ambientMode         = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor     = new Color(0.55f, 0.62f, 0.75f);
        RenderSettings.ambientEquatorColor = new Color(0.45f, 0.45f, 0.42f);
        RenderSettings.ambientGroundColor  = new Color(0.25f, 0.25f, 0.22f);

        RenderSettings.fog              = true;
        RenderSettings.fogColor         = new Color(0.62f, 0.68f, 0.78f);
        RenderSettings.fogMode          = FogMode.Linear;
        RenderSettings.fogStartDistance = 90f;
        RenderSettings.fogEndDistance   = 240f;

        DynamicGI.UpdateEnvironment();
    }

    // ── Ground ────────────────────────────────────────────────────────────────
    void BuildGround()
    {
        var g = Primitive("Ground", new Vector3(0, -0.1f, 0), new Vector3(220f, 0.2f, 220f),
                          Mat(new Color(0.20f, 0.44f, 0.18f)));
        g.transform.SetParent(transform);
    }

    // ── Roads ─────────────────────────────────────────────────────────────────
    void BuildRoads()
    {
        var roadMat = Mat(new Color(0.09f, 0.09f, 0.09f));
        var kerbMat = Mat(new Color(0.85f, 0.85f, 0.85f));
        float len = 170f;

        Road("Ave N-S",  new Vector3(0, 0.05f, 0),   new Vector3(10f, 0.1f, len),  roadMat);
        Road("Street S", new Vector3(0, 0.06f, -20f), new Vector3(len, 0.1f, 8f),   roadMat);
        Road("Street N", new Vector3(0, 0.06f,  22f), new Vector3(len, 0.1f, 8f),   roadMat);
        Road("Kerb L",   new Vector3(-5.1f, 0.07f, 0), new Vector3(0.2f, 0.1f, len), kerbMat);
        Road("Kerb R",   new Vector3( 5.1f, 0.07f, 0), new Vector3(0.2f, 0.1f, len), kerbMat);

        // Centre dashes
        var dashMat = Mat(new Color(0.95f, 0.85f, 0.1f));
        for (float z = -70f; z <= 70f; z += 8f)
            Road("Dash", new Vector3(0, 0.08f, z), new Vector3(0.25f, 0.1f, 3f), dashMat);
    }

    void Road(string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var r = Primitive(name, pos, scale, mat);
        r.transform.SetParent(transform);
        DestroyImmediate(r.GetComponent<Collider>());
    }

    // ── Street lamps (emissive, no real lights → URP-friendly) ────────────────
    void BuildStreetLamps()
    {
        var poleMat = Mat(new Color(0.18f, 0.18f, 0.20f));
        var bulbMat = Mat(new Color(1f, 0.85f, 0.45f), emissive: true);

        for (float z = -56f; z <= 56f; z += 16f)
        foreach (float x in new[] { -6.6f, 6.6f })
        {
            var lamp = new GameObject("StreetLamp");
            lamp.transform.SetParent(transform);
            lamp.transform.position = new Vector3(x, 0, z);

            var pole = Primitive("Pole", Vector3.zero, new Vector3(0.18f, 5f, 0.18f), poleMat);
            pole.transform.SetParent(lamp.transform);
            pole.transform.localPosition = new Vector3(0, 2.5f, 0);
            DestroyImmediate(pole.GetComponent<Collider>());

            float arm = x < 0 ? 0.5f : -0.5f;
            var bulb = Primitive("Bulb", Vector3.zero, new Vector3(0.55f, 0.28f, 0.55f), bulbMat);
            bulb.transform.SetParent(lamp.transform);
            bulb.transform.localPosition = new Vector3(arm, 5f, 0);
            DestroyImmediate(bulb.GetComponent<Collider>());
        }
    }

    // ── Shop ─────────────────────────────────────────────────────────────────
    void BuildShop()
    {
        var root = new GameObject("Takeaway Shop");
        root.transform.SetParent(transform);
        root.transform.position = new Vector3(0, 0, -62f);

        var body = Primitive("Body", new Vector3(0, 3f, 0), new Vector3(14f, 6f, 10f), Mat(new Color(1f, 0.42f, 0.04f)));
        body.transform.SetParent(root.transform);

        AddDeco(root, "Roof",    new Vector3(0, 6.1f, 0),    new Vector3(14.4f, 0.2f, 10.4f), Mat(new Color(0.15f, 0.15f, 0.15f)));
        AddDeco(root, "Awning",  new Vector3(0, 4.6f, -5.3f), new Vector3(13f, 0.15f, 2.5f),  Mat(new Color(1f, 0.15f, 0.1f)));
        AddDeco(root, "Sign",    new Vector3(0, 5.5f, -5.1f), new Vector3(10f, 0.9f, 0.1f),   Mat(new Color(1f, 0.9f, 0f), true));
        AddDeco(root, "PickupZone", new Vector3(0, 0.02f, -5.5f), new Vector3(4f, 0.04f, 3f), Mat(new Color(0f, 0.85f, 0.15f), true));

        var counter = Primitive("Counter", new Vector3(0, 1.1f, 1.5f), new Vector3(10f, 0.9f, 0.9f), Mat(new Color(0.55f, 0.35f, 0.1f)));
        counter.transform.SetParent(root.transform);
    }

    // ── Houses + filler trees ─────────────────────────────────────────────────
    static readonly Vector2[] LotCenters =
    {
        new(-28,-42), new(-14,-42), new(14,-42), new(28,-42),
        new(-28,-32), new(-14,-32), new(14,-32), new(28,-32),
        new(-28, -8), new(-14, -8), new(14, -8), new(28, -8),
        new(-28,  5), new(-14,  5), new(14,  5), new(28,  5),
        new(-28, 34), new(-14, 34), new(14, 34), new(28, 34),
    };

    void BuildHousesAndTrees()
    {
        var lots = new List<Vector2>(LotCenters);
        for (int i = lots.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (lots[i], lots[j]) = (lots[j], lots[i]);
        }

        int houses = Mathf.Min(houseCount, lots.Count);
        for (int i = 0; i < lots.Count; i++)
        {
            if (i < houses) PlaceHouse(i + 1, lots[i]);
            else            PlaceTreeCluster(lots[i]);   // empty lots become little gardens
        }
    }

    void PlaceHouse(int index, Vector2 lot)
    {
        Vector3 pos = new Vector3(lot.x, 0, lot.y);
        GameObject house;

        if (housePrefabs != null && housePrefabs.Length > 0)
        {
            GameObject prefab = housePrefabs[Random.Range(0, housePrefabs.Length)];
            house = Instantiate(prefab, transform);
            house.transform.position = pos;
            house.transform.rotation = Quaternion.Euler(0, lot.x < 0 ? 90f : -90f, 0);

            NormalisePrefabSize(house, houseTargetWidth);
            GroundHouse(house);
            EnsureBoxColliderFromBounds(house);
            if (fixURPMaterials) UpgradeMaterials(house);
        }
        else
        {
            house = BuildFallbackHouse(pos);
        }

        house.name = "Customer House " + index;

        var dh = house.AddComponent<DeliveryHouse>();
        dh.houseName    = "House " + index;
        dh.doorWorldPos = house.transform.position + house.transform.forward * -3.8f + Vector3.up * 1.2f;

        var beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        beacon.name = "__Beacon";
        beacon.transform.SetParent(house.transform);
        beacon.transform.position   = house.transform.position + Vector3.up * 7f;
        beacon.transform.localScale = Vector3.one * 1.4f;
        beacon.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0f, 1f, 0.2f), true);
        DestroyImmediate(beacon.GetComponent<Collider>());
        beacon.SetActive(false);
    }

    GameObject BuildFallbackHouse(Vector3 pos)
    {
        var house = new GameObject("House");
        house.transform.SetParent(transform);
        house.transform.position = pos + Vector3.up * 3f;

        var body = Primitive("Body", Vector3.zero, new Vector3(7f, 6f, 7f), Mat(new Color(0.74f, 0.72f, 0.66f)));
        body.transform.SetParent(house.transform);

        var roofMat = Mat(new Color(0.5f, 0.22f, 0.18f));
        AddDeco(house, "Roof", new Vector3(0, 3.4f, 0), new Vector3(8f, 0.6f, 8f), roofMat);

        var winMat = Mat(new Color(1f, 0.95f, 0.6f), true);
        AddDeco(house, "Win L", new Vector3(-2f, 0.4f, -3.55f), new Vector3(1.2f, 1f, 0.08f), winMat);
        AddDeco(house, "Win R", new Vector3( 2f, 0.4f, -3.55f), new Vector3(1.2f, 1f, 0.08f), winMat);
        AddDeco(house, "Door",  new Vector3(0, -1f, -3.55f),    new Vector3(0.9f, 2f, 0.1f), Mat(new Color(0.32f, 0.16f, 0.05f)));

        EnsureBoxCollider(house, new Vector3(7f, 6f, 7f));
        return house;
    }

    // ── Trees ─────────────────────────────────────────────────────────────────
    void PlaceTreeCluster(Vector2 lot)
    {
        if (treePrefabs == null || treePrefabs.Length == 0) return;
        int n = Random.Range(2, 4);
        for (int i = 0; i < n; i++)
        {
            Vector2 off = Random.insideUnitCircle * 5f;
            PlaceTree(new Vector3(lot.x + off.x, 0, lot.y + off.y));
        }
    }

    void BuildPerimeterTrees()
    {
        if (treePrefabs == null || treePrefabs.Length == 0) return;
        for (float t = -80f; t <= 80f; t += 12f)
        {
            PlaceTree(new Vector3(-66f, 0, t));
            PlaceTree(new Vector3( 66f, 0, t));
            PlaceTree(new Vector3(t, 0, -78f));
            PlaceTree(new Vector3(t, 0,  72f));
        }
    }

    void PlaceTree(Vector3 pos)
    {
        var prefab = treePrefabs[Random.Range(0, treePrefabs.Length)];
        var tree   = Instantiate(prefab, transform);
        tree.name  = "Tree";
        tree.transform.position    = pos;
        tree.transform.rotation    = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        tree.transform.localScale *= Random.Range(0.8f, 1.35f);
        if (fixURPMaterials) UpgradeMaterials(tree);
        foreach (var c in tree.GetComponentsInChildren<Collider>()) DestroyImmediate(c);
    }

    // ── Car ───────────────────────────────────────────────────────────────────
    void BuildCar()
    {
        var car = new GameObject("Manual Delivery Car");
        car.transform.SetParent(transform);
        car.transform.position = new Vector3(0, 1.2f, -48f);

        var red    = Mat(new Color(0.88f, 0.06f, 0.06f));
        var dark   = Mat(new Color(0.15f, 0.15f, 0.18f));
        var grey   = Mat(new Color(0.35f, 0.35f, 0.35f));
        var black  = Mat(new Color(0.04f, 0.04f, 0.04f));
        var orange = Mat(new Color(1f, 0.55f, 0f), true);
        var boxMat = Mat(new Color(0.9f, 0.75f, 0.1f));

        AddVisual(car, "Body",        new Vector3(0,  0.00f,  0.0f), new Vector3(2.2f, 0.65f, 4.4f), red);
        AddVisual(car, "Cabin",       new Vector3(0,  0.68f, -0.2f), new Vector3(1.85f, 0.72f, 2.2f), dark);
        AddVisual(car, "Bumper F",    new Vector3(0, -0.05f,  2.2f), new Vector3(2.0f, 0.28f, 0.18f), grey);
        AddVisual(car, "Bumper R",    new Vector3(0, -0.05f, -2.2f), new Vector3(2.0f, 0.28f, 0.18f), grey);
        AddVisual(car, "Headlight L", new Vector3(-0.85f, 0.05f, 2.26f), new Vector3(0.4f, 0.22f, 0.08f), orange);
        AddVisual(car, "Headlight R", new Vector3( 0.85f, 0.05f, 2.26f), new Vector3(0.4f, 0.22f, 0.08f), orange);
        AddVisual(car, "DeliveryBox", new Vector3(0, 1.16f, -0.5f), new Vector3(1.3f, 0.85f, 1.5f), boxMat);
        AddVisual(car, "BoxLid",      new Vector3(0, 1.60f, -0.5f), new Vector3(1.35f, 0.08f, 1.55f), boxMat);

        AddWheel(car, new Vector3(-1.1f, -0.42f,  1.4f), black);
        AddWheel(car, new Vector3( 1.1f, -0.42f,  1.4f), black);
        AddWheel(car, new Vector3(-1.1f, -0.42f, -1.4f), black);
        AddWheel(car, new Vector3( 1.1f, -0.42f, -1.4f), black);

        var col = car.AddComponent<BoxCollider>();
        col.center = new Vector3(0, 0.28f, 0);
        col.size   = new Vector3(2.2f, 1.2f, 4.4f);

        car.AddComponent<Rigidbody>();          // configured in ManualCarController.Awake
        car.AddComponent<ManualCarController>();
        // Player's camera reparents onto the car when driving — no camera here.
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

        var cap = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        cap.name = "Mesh";
        cap.transform.SetParent(player.transform);
        cap.transform.localPosition = Vector3.zero;
        cap.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0.1f, 0.25f, 0.9f));
        DestroyImmediate(cap.GetComponent<Collider>());

        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.85f; cc.radius = 0.32f; cc.stepOffset = 0.4f;
        cc.center = new Vector3(0, 0.925f, 0);

        var camObj = new GameObject("PlayerCamera");
        camObj.transform.SetParent(player.transform);
        camObj.transform.localPosition = new Vector3(0, 1.7f, 0);
        camObj.AddComponent<Camera>().tag = "MainCamera";
        camObj.AddComponent<AudioListener>();

        var pc = player.AddComponent<PlayerController>();
        pc.camTransform = camObj.transform;
    }

    void BuildGameManager()
    {
        if (GetComponent<DeliveryGameManager>() == null)
            gameObject.AddComponent<DeliveryGameManager>();
    }

    // ── Prefab utilities ──────────────────────────────────────────────────────
    static GameObject[] LoadPrefabs(string folder, params string[] names)
    {
#if UNITY_EDITOR
        var list = new List<GameObject>();
        foreach (string n in names)
        {
            var p = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(folder + n + ".prefab");
            if (p != null) list.Add(p);
        }
        return list.ToArray();
#else
        return new GameObject[0];
#endif
    }

    static void NormalisePrefabSize(GameObject go, float targetWidth)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        float biggest = Mathf.Max(b.size.x, b.size.z);
        if (biggest < 0.01f) return;
        go.transform.localScale *= targetWidth / biggest;
    }

    static void GroundHouse(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        if (Mathf.Abs(b.min.y) > 0.01f)
            go.transform.position += Vector3.up * (-b.min.y);
    }

    static void EnsureBoxCollider(GameObject go, Vector3 size)
    {
        foreach (var c in go.GetComponentsInChildren<Collider>()) DestroyImmediate(c);
        var box = go.AddComponent<BoxCollider>();
        box.center = new Vector3(0, size.y * 0.5f, 0);
        box.size   = size;
    }

    static void EnsureBoxColliderFromBounds(GameObject go)
    {
        foreach (var c in go.GetComponentsInChildren<Collider>()) DestroyImmediate(c);
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        var box = go.AddComponent<BoxCollider>();
        box.center = go.transform.InverseTransformPoint(b.center);
        box.size   = go.transform.InverseTransformVector(b.size);
        box.size   = new Vector3(Mathf.Abs(box.size.x), Mathf.Abs(box.size.y), Mathf.Abs(box.size.z));
    }

    static void UpgradeMaterials(GameObject go)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) return;

        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            Material[] mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                if (mats[i].shader.name.StartsWith("Universal Render Pipeline")) continue;

                var up = new Material(urpLit) { name = mats[i].name + "_URP" };
                if (mats[i].HasProperty("_MainTex") && mats[i].GetTexture("_MainTex") != null)
                    up.SetTexture("_BaseMap", mats[i].GetTexture("_MainTex"));
                Color col = mats[i].HasProperty("_Color") ? mats[i].GetColor("_Color") : Color.white;
                up.SetColor("_BaseColor", col);
                mats[i] = up;
                changed = true;
            }
            if (changed) r.sharedMaterials = mats;
        }
    }

    // ── Generic helpers ───────────────────────────────────────────────────────
    void AddDeco(GameObject parent, string name, Vector3 lp, Vector3 ls, Material mat)
    {
        var g = Primitive(name, Vector3.zero, ls, mat);
        g.transform.SetParent(parent.transform);
        g.transform.localPosition = lp;
        DestroyImmediate(g.GetComponent<Collider>());
    }

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
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * 1.4f);
        }
        return m;
    }
}
