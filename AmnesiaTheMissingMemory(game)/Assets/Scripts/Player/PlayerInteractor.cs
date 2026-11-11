using UnityEngine;
using UnityEngine.InputSystem;

// checks what the player is looking at. if it's something you can use,
// it shows "[E] ..." on screen and uses it when you press E.
// goes on the player.
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform; // leave empty and it uses the main camera
    [SerializeField] private float interactRange = 2.5f; // how far you can reach
    [SerializeField] private float aimRadius = 0.2f;     // bigger makes small stuff easier to aim at
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

    // shows a short text in the middle of the screen, like "Picked up Knife"
    public void ShowMessage(string text)
    {
        message = text;
        messageUntil = Time.time + messageSeconds;
    }

    Interactable FindTarget()
    {
        if (cameraTransform == null) return null;

        // shoot a thin invisible tube forward from the camera and see what it hits
        RaycastHit[] hits = Physics.SphereCastAll(cameraTransform.position, aimRadius, cameraTransform.forward,
                                                  interactRange, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance)); // closest first

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.IsChildOf(transform)) continue; // ignore the player's own body

            Interactable target = hit.collider.GetComponentInParent<Interactable>();
            if (target == null) return null; // hit a wall or something first
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
            // crosshair dot, gets bigger when you can use something
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
