using UnityEngine;
using UnityEngine.InputSystem;

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
        Cursor.lockState = CursorLockMode.Locked; // hides the cursor, press Esc to get it back
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (Mouse.current == null) return;

        Vector2 look = Mouse.current.delta.ReadValue() * mouseSensitivity;
        yaw += look.x;
        pitch = Mathf.Clamp(pitch - look.y, -85f, 85f);

        // turn the player left/right, tilt only the camera up/down
        player.rotation = Quaternion.Euler(0, yaw, 0);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0);
        transform.position = player.position + offset;
    }
}