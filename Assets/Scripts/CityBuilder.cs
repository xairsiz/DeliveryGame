using UnityEngine;

[ExecuteAlways]
public class CityBuilder : MonoBehaviour
{
    public int blocksX = 4;
    public int blocksZ = 4;
    public float blockSize = 25f;
    public float roadWidth = 6f;
    public int housesPerBlock = 3;

    [ContextMenu("Generate City")]
    public void GenerateCity()
    {
        ClearCity();
        CreateGround();
        CreateRoads();
        CreateHouses();
        CreateStore();
    }

    [ContextMenu("Clear City")]
    public void ClearCity()
    {
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }

    void CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.SetParent(transform);
        ground.transform.position = new Vector3(0, -0.1f, 0);
        ground.transform.localScale = new Vector3(140, 0.2f, 140);
    }

    void CreateRoads()
    {
        for (int x = 0; x <= blocksX; x++)
        {
            float posX = x * blockSize - blocksX * blockSize / 2f;

            GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = "Road Vertical";
            road.transform.SetParent(transform);
            road.transform.position = new Vector3(posX, 0.05f, 0);
            road.transform.localScale = new Vector3(roadWidth, 0.1f, blocksZ * blockSize + roadWidth);
        }

        for (int z = 0; z <= blocksZ; z++)
        {
            float posZ = z * blockSize - blocksZ * blockSize / 2f;

            GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = "Road Horizontal";
            road.transform.SetParent(transform);
            road.transform.position = new Vector3(0, 0.06f, posZ);
            road.transform.localScale = new Vector3(blocksX * blockSize + roadWidth, 0.1f, roadWidth);
        }
    }

    void CreateHouses()
    {
        int houseNumber = 1;

        for (int x = 0; x < blocksX; x++)
        {
            for (int z = 0; z < blocksZ; z++)
            {
                Vector3 blockCenter = new Vector3(
                    x * blockSize - blocksX * blockSize / 2f + blockSize / 2f,
                    0,
                    z * blockSize - blocksZ * blockSize / 2f + blockSize / 2f
                );

                for (int i = 0; i < housesPerBlock; i++)
                {
                    GameObject house = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    house.name = "House " + houseNumber;
                    house.transform.SetParent(transform);

                    float randomX = Random.Range(-7f, 7f);
                    float randomZ = Random.Range(-7f, 7f);

                    house.transform.position = blockCenter + new Vector3(randomX, 2f, randomZ);
                    house.transform.localScale = new Vector3(5f, 4f, 5f);

                    houseNumber++;
                }
            }
        }
    }

    void CreateStore()
    {
        GameObject store = GameObject.CreatePrimitive(PrimitiveType.Cube);
        store.name = "Takeaway Store";
        store.transform.SetParent(transform);
        store.transform.position = new Vector3(0, 2.5f, -65f);
        store.transform.localScale = new Vector3(12f, 5f, 10f);
    }
}