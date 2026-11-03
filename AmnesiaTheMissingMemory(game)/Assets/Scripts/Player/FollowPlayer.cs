using UnityEngine;
using UnityEngine.InputSystem;

// First person camera: sits at the player's eyes and looks around with the mouse.
// Put it on the Main Camera (NOT as a child of the player) and drag the player into "Player".
public class FollowPlayer : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Vector3 offset = new Vector3(0, 0.6f, 0); // eye height above the player's center
    [SerializeField] private float mouseSensitivity = 0.1f;

    private float yaw;
    private float pitch;

    void Start()
    {
        yaw = player.eulerAngles.y;
        LockCursor();
    }

    void LateUpdate()
    {
        Mouse mouse = Mouse.current;
        if (mouse != null && !GameState.InputLocked)
        {
            // Esc frees the cursor in the editor, click the game view to grab it again
            if (mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked) LockCursor();

            Vector2 look = mouse.delta.ReadValue() * mouseSensitivity;
            yaw += look.x;
            pitch = Mathf.Clamp(pitch - look.y, -85f, 85f);
        }

        // the player's body turns to match the camera in PlayerHandler
        transform.rotation = Quaternion.Euler(pitch, yaw, 0);
        transform.position = player.position + offset;
    }

    static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
