using UnityEngine;
using UnityEngine.InputSystem;

// Looks at what the camera points at. If it's an Interactable in range,
// shows "[E] ..." on screen and calls Interact() when E is pressed.
// Put it on the player.
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform; // leave empty to use the Main Camera
    [SerializeField] private float interactRange = 2.5f; // how far from the eyes you can reach
    [SerializeField] private float aimRadius = 0.2f;     // bigger = easier to aim at small items
    [SerializeField] private float messageSeconds = 2.5f;

    public Interactable Current { get; private set; }

    string message;
    float messageUntil;

    void Awake()
    {
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        Current = GameState.InputLocked ? null : FindTarget();

        Keyboard kb = Keyboard.current;
        if (Current != null && kb != null && kb.eKey.wasPressedThisFrame)
            Current.Interact(this);
    }

    // short text in the middle of the screen, e.g. "Picked up Torn Letter"
    public void ShowMessage(string text)
    {
        message = text;
        messageUntil = Time.time + messageSeconds;
    }

    Interactable FindTarget()
    {
        if (cameraTransform == null) return null;

        RaycastHit[] hits = Physics.SphereCastAll(cameraTransform.position, aimRadius, cameraTransform.forward,
                                                  interactRange, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.IsChildOf(transform)) continue; // skip our own body

            Interactable target = hit.collider.GetComponentInParent<Interactable>();
            if (target == null) return null; // a wall or something else is in the way
            return target.CanInteract ? target : null;
        }
        return null;
    }

    void OnGUI()
    {
        float s = GameUI.Scale;
        Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);

        if (!GameState.InputLocked)
        {
            // small dot crosshair, turns bigger when something can be used
            float dot = (Current != null ? 8 : 4) * s;
            GameUI.Fill(new Rect(center.x - dot / 2, center.y - dot / 2, dot, dot), new Color(1, 1, 1, 0.8f));
        }

        if (Current != null)
        {
            Rect box = new Rect(center.x - 220 * s, Screen.height * 0.72f, 440 * s, 54 * s);
            GameUI.Fill(box, new Color(0, 0, 0, 0.6f));
            GUI.Label(box, $"[E]  {Current.Prompt}", GameUI.Text(26, TextAnchor.MiddleCenter, FontStyle.Bold));
        }

        if (!string.IsNullOrEmpty(message) && Time.time < messageUntil)
        {
            Rect box = new Rect(center.x - 300 * s, Screen.height * 0.62f, 600 * s, 44 * s);
            GUI.Label(box, message, GameUI.Text(22));
        }
    }
}
