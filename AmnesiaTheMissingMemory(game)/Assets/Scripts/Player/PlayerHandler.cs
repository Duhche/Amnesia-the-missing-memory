using UnityEngine;
using UnityEngine.InputSystem;

// moves the player with WASD.
// needs a Rigidbody and a Collider on the player.
[RequireComponent(typeof(Rigidbody))]
public class PlayerHandler : MonoBehaviour
{
    [SerializeField] private float playerSpeed = 5f;
    [SerializeField] private Transform cameraTransform; // leave empty and it uses the main camera

    Rigidbody rb;
    Vector3 moveDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // so the player doesn't tip over
        rb.interpolation = RigidbodyInterpolation.Interpolate; // makes the movement look smooth
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
    }

    // check the keys every frame
    void Update()
    {
        moveDirection = Vector3.zero;
        Keyboard kb = Keyboard.current;
        if (kb == null || GameState.InputLocked) return;

        float x = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0);
        float z = (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0);

        // walk the way the camera is facing, but stay flat on the ground
        Transform view = cameraTransform != null ? cameraTransform : transform;
        Vector3 forward = Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(view.right, Vector3.up).normalized;

        moveDirection = (right * x + forward * z).normalized;
    }

    // physics stuff goes here
    void FixedUpdate()
    {
        Vector3 velocity = moveDirection * playerSpeed;
        velocity.y = rb.linearVelocity.y; // keep falling with gravity
        rb.linearVelocity = velocity;

        // turn the body the same way as the camera
        if (cameraTransform != null)
            rb.MoveRotation(Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0));
    }
}
