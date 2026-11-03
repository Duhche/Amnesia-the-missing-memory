using UnityEngine;
using UnityEngine.InputSystem;

// Walks the player with WASD.
// Put it on the player together with a Rigidbody and a Collider.
[RequireComponent(typeof(Rigidbody))]
public class PlayerHandler : MonoBehaviour
{
    [SerializeField] private float playerSpeed = 5f;
    [SerializeField] private Transform cameraTransform; // leave empty to use the Main Camera

    Rigidbody rb;
    Vector3 moveDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // stops the capsule from falling over
        rb.interpolation = RigidbodyInterpolation.Interpolate; // smooth movement for the camera
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
    }

    // read keys every frame so no press is missed
    void Update()
    {
        moveDirection = Vector3.zero;
        Keyboard kb = Keyboard.current;
        if (kb == null || GameState.InputLocked) return;

        float x = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0);
        float z = (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0);

        // walk relative to where the camera looks, flattened onto the ground
        Transform view = cameraTransform != null ? cameraTransform : transform;
        Vector3 forward = Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(view.right, Vector3.up).normalized;

        moveDirection = (right * x + forward * z).normalized;
    }

    // apply physics at a fixed rate
    void FixedUpdate()
    {
        Vector3 velocity = moveDirection * playerSpeed;
        velocity.y = rb.linearVelocity.y; // keep gravity working
        rb.linearVelocity = velocity;

        // body always faces the same way as the camera
        if (cameraTransform != null)
            rb.MoveRotation(Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0));
    }
}
