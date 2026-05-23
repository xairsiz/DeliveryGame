using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float walkSpeed = 6f;
    public float mouseSensitivity = 3f;
    public Camera playerCamera;

    private CharacterController controller;
    private float cameraPitch = 0f;
    private float verticalVelocity = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        Look();
        Move();
    }

    void Look()
    {
        if (playerCamera == null) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(0, mouseX, 0);

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -75f, 75f);
        playerCamera.transform.localRotation = Quaternion.Euler(cameraPitch, 0, 0);
    }

    void Move()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = transform.forward * v + transform.right * h;
        move *= walkSpeed;

        if (controller.isGrounded)
            verticalVelocity = -1f;
        else
            verticalVelocity -= 20f * Time.deltaTime;

        move.y = verticalVelocity;
        controller.Move(move * Time.deltaTime);
    }
}