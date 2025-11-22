using UnityEngine;

/// <summary>
/// Player movement that reads MobileJoystick input and moves a Rigidbody player.
/// Attach to Player (sphere) which must have a Rigidbody and Collider.
/// Uses MobileJoystick.Direction() which returns Vector2 in range [-1..1].
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerJoystickController : MonoBehaviour
{
    public enum MovementMode { Torque, Force }

    [Header("References")]
    public MobileJoystick joystick;        // Drag JoystickBG here (has MobileJoystick component)
    public Camera referenceCamera;         // Drag Main Camera here (for relative movement)

    [Header("Movement")]
    public MovementMode movementMode = MovementMode.Torque;
    [Tooltip("Torque strength for rolling ball (try 12-30)")]
    public float torqueStrength = 18f;
    [Tooltip("Force strength when using Force mode")]
    public float forceStrength = 8f;
    [Tooltip("Maximum horizontal speed (world units/sec)")]
    public float maxSpeed = 14f;
    [Tooltip("Ignore tiny joystick inputs")]
    [Range(0f, 0.5f)]
    public float deadZone = 0.12f;

    [Header("Jump (optional)")]
    public float jumpForce = 6f;
    public LayerMask groundMask;
    public float groundCheckDistance = 0.6f;
    public Transform groundCheckOrigin;    // child transform near bottom of player (optional)

    [Header("Stabilization")]
    [Tooltip("Slerp factor for angular stabilization when touching walls/obstacles (0 = none, 1 = instant)")]
    [Range(0f, 1f)]
    public float angularDampingOnContact = 0.35f;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (referenceCamera == null) referenceCamera = Camera.main;
        if (joystick == null) Debug.LogWarning("PlayerJoystickController: Assign MobileJoystick in Inspector.");
    }

    void FixedUpdate()
    {
        if (joystick == null) return;

        // read joystick direction (already normalized -1..1)
        Vector2 in2 = joystick.Direction();
        if (in2.magnitude < deadZone)
        {
            // small deadzone: do nothing except maybe damp lateral velocity slightly
            // nothing else to do
            return;
        }

        // Map joystick input to world directions relative to camera yaw (ignore camera pitch)
        Vector3 camForward = referenceCamera.transform.forward;
        camForward.y = 0f;
        camForward.Normalize();
        Vector3 camRight = referenceCamera.transform.right;
        camRight.y = 0f;
        camRight.Normalize();

        Vector3 moveDir = (camRight * in2.x + camForward * in2.y);
        if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

        if (movementMode == MovementMode.Torque)
        {
            // Rolling ball: apply torque around axis perpendicular to movement direction
            Vector3 torqueAxis = Vector3.Cross(Vector3.up, moveDir).normalized;
            Vector3 torque = torqueAxis * (in2.magnitude * torqueStrength);
            rb.AddTorque(torque, ForceMode.Force);

            // clamp horizontal speed
            Vector3 horizontal = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            if (horizontal.magnitude > maxSpeed)
            {
                Vector3 clamped = horizontal.normalized * maxSpeed;
                rb.velocity = new Vector3(clamped.x, rb.velocity.y, clamped.z);
            }
        }
        else // Force mode
        {
            Vector3 desiredVel = moveDir * forceStrength;
            Vector3 velChange = desiredVel - new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            rb.AddForce(velChange, ForceMode.VelocityChange);

            Vector3 horizontal = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            if (horizontal.magnitude > maxSpeed)
            {
                Vector3 clamped = horizontal.normalized * maxSpeed;
                rb.velocity = new Vector3(clamped.x, rb.velocity.y, clamped.z);
            }
        }
    }

    /// <summary>
    /// Jump method can be wired to a UI Button (Jump) or invoked by code.
    /// </summary>
    public void Jump()
    {
        if (IsGrounded())
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        }
    }

    bool IsGrounded()
    {
        Vector3 origin = groundCheckOrigin != null ? groundCheckOrigin.position : transform.position;
        // slightly longer ray to be robust on uneven road
        return Physics.Raycast(origin, Vector3.down, groundCheckDistance, groundMask);
    }
}
