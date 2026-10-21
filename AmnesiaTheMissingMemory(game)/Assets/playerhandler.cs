using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHandler : MonoBehaviour
{
    Rigidbody rb;
    [SerializeField] private float playerSpeed = 5f;
    

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        float x = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0);
        float z = (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0);

        Vector3 movement = (transform.right * x + transform.forward * z).normalized;
        rb.MovePosition(rb.position + movement * playerSpeed * Time.fixedDeltaTime);
    }
}