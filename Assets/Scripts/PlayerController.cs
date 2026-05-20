using UnityEngine;

/// First-person on-foot controller. Uses the legacy Input System (Input.GetAxis).
/// Set CanMove = false from DeliveryGameManager while the player is in the car.
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float mouseSensitivity = 2.5f;
    public Transform camTransform;   // assigned by CityGenerator

    [HideInInspector] public bool CanMove = true;

    CharacterController cc;
    float pitch;
    float vSpeed;

    void Awake() => cc = GetComponent<CharacterController>();

    void OnEnable() { pitch = 0f; vSpeed = 0f; }   // clean up on re-enter

    void Update()
    {
        if (!Application.isPlaying || !CanMove) return;

        // ── Mouse look ────────────────────────────────────────────────────
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
            float my = Input.GetAxis("Mouse Y") * mouseSensitivity;

            transform.Rotate(0f, mx, 0f);
            pitch = Mathf.Clamp(pitch - my, -75f, 75f);
            if (camTransform) camTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        // ── WASD move ─────────────────────────────────────────────────────
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = transform.forward * v + transform.right * h;
        if (move.sqrMagnitude > 1f) move.Normalize();
        move *= moveSpeed;

        vSpeed = cc.isGrounded ? -1f : vSpeed + Physics.gravity.y * Time.deltaTime;
        move.y = vSpeed;

        cc.Move(move * Time.deltaTime);
    }
}
