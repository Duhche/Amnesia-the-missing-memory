using UnityEngine;
using UnityEngine.InputSystem;

// first person camera, sits at the player's eyes and looks around with the mouse.
// goes on the Main Camera (not inside the player) with the player dragged into "Player".
public class FollowPlayer : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Vector3 offset = new Vector3(0, 0.6f, 0); // how high the eyes are
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
            // esc lets the mouse go in the editor, clicking grabs it again
            if (mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked) LockCursor();

            Vector2 look = mouse.delta.ReadValue() * mouseSensitivity;
            yaw += look.x;
            pitch = Mathf.Clamp(pitch - look.y, -85f, 85f); // can't look so far up or down that it flips
        }

        // PlayerHandler turns the body to match this
        transform.rotation = Quaternion.Euler(pitch, yaw, 0);
        transform.position = player.position + offset;
    }

    static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
