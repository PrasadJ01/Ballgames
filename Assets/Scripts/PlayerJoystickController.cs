using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerJoystickController : MonoBehaviour
{
    [Header("References")]
    public MobileJoystick joystick;      // Drag your joystick handle parent here
    public Camera referenceCamera;       // Usually Main Camera

    [Header("Movement Settings")]
    public float moveForce = 15f;
    public float maxSpeed = 12f;
    public float damping = 0.9f;         // When joystick released

    [Header("Jump Settings")]
    public float jumpForce = 8f;
    public bool alwaysJump = true;       // No ground check

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (referenceCamera == null)
            referenceCamera = Camera.main;

        if (joystick == null)
            Debug.LogWarning("⚠️ PlayerJoystickController: Joystick not assigned!");
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    // ---------------------------------------------------------
    // MOVEMENT
    // ---------------------------------------------------------
    void HandleMovement()
    {
        if (joystick == null) return;

        Vector2 joy = joystick.Direction();

        // No movement → apply slight damping
        if (joy.magnitude < 0.05f)
        {
            Vector3 slow = rb.velocity;
            slow.x *= damping;
            slow.z *= damping;
            rb.velocity = slow;
            return;
        }

        // Camera-relative controls
        Vector3 camForward = referenceCamera.transform.forward; camForward.y = 0; camForward.Normalize();
        Vector3 camRight = referenceCamera.transform.right; camRight.y = 0; camRight.Normalize();

        Vector3 moveDir = camRight * joy.x + camForward * joy.y;

        rb.AddForce(moveDir * moveForce, ForceMode.Acceleration);

        // Speed limit
        Vector3 horizontalVel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        if (horizontalVel.magnitude > maxSpeed)
        {
            horizontalVel = horizontalVel.normalized * maxSpeed;
            rb.velocity = new Vector3(horizontalVel.x, rb.velocity.y, horizontalVel.z);
        }
    }

    // ---------------------------------------------------------
    // JUMP (UI Button calls this)
    // ---------------------------------------------------------
    public void Jump()
    {
        if (!alwaysJump)
        {
            Debug.Log("Jump suppressed (alwaysJump = false). Enable if needed.");
            return;
        }

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        Debug.Log("🟢 Player Jump!");
    }
}
