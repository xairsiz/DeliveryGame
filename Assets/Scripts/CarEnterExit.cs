using UnityEngine;

public class CarEnterExit : MonoBehaviour
{
    [Header("References")]
    public GameObject player;          // your player object
    public Camera playerCamera;        // walking camera
    public Transform exitPoint;        // where player appears on exit
    public Camera carCamera;           // camera that follows the car

    [Header("Settings")]
    public float enterDistance = 4f;
    public KeyCode enterKey = KeyCode.F;

    private CarDrive carDrive;
    private bool inCar = false;

    void Start()
    {
        carDrive = GetComponent<CarDrive>();
        if (carCamera != null) carCamera.gameObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(enterKey))
        {
            if (!inCar)
                TryEnter();
            else
                ExitCar();
        }
    }

    void TryEnter()
    {
        if (player == null) return;

        float dist = Vector3.Distance(player.transform.position, transform.position);
        if (dist > enterDistance) return;

        inCar = true;
        carDrive.isDriving = true;

        player.SetActive(false);
        if (playerCamera != null) playerCamera.gameObject.SetActive(false);
        if (carCamera != null) carCamera.gameObject.SetActive(true);
    }

    void ExitCar()
    {
        inCar = false;
        carDrive.isDriving = false;

        // Place the player at the exit point
        if (exitPoint != null)
            player.transform.position = exitPoint.position;
        else
            player.transform.position = transform.position + transform.right * 3f + Vector3.up;

        player.SetActive(true);
        if (playerCamera != null) playerCamera.gameObject.SetActive(true);
        if (carCamera != null) carCamera.gameObject.SetActive(false);
    }
}