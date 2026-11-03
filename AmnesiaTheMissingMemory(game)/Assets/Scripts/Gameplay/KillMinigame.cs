using UnityEngine;
using UnityEngine.InputSystem;

// The "mash SPACE" pop-up that runs when the killer attacks a Victim.
// Each press fills the bar, the bar slowly drains. Fill it before the timer runs out.
// Put ONE of these on a "GameSystems" object in the scene.
public class KillMinigame : MonoBehaviour
{
    public static KillMinigame Instance { get; private set; }

    [SerializeField] private float pressPower = 0.1f;      // how much one press fills (0-1)
    [SerializeField] private float drainPerSecond = 0.25f; // the victim fighting back
    [SerializeField] private float startFill = 0.2f;
    [SerializeField] private float timeLimit = 6f;

    public bool IsRunning => victim != null;

    Victim victim;
    PlayerInteractor interactor;
    float fill;
    float timeLeft;
    float pulse; // makes the bar flash on each press

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    public void Begin(Victim target, PlayerInteractor player)
    {
        if (IsRunning) return;
        victim = target;
        interactor = player;
        fill = startFill;
        timeLeft = timeLimit;
        GameState.InputLocked = true; // freeze walking and looking during the struggle
    }

    void Update()
    {
        if (!IsRunning) return;

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.spaceKey.wasPressedThisFrame)
        {
            fill += pressPower;
            pulse = 1f;
        }

        fill = Mathf.Clamp01(fill - drainPerSecond * Time.deltaTime);
        timeLeft -= Time.deltaTime;
        pulse = Mathf.MoveTowards(pulse, 0, Time.deltaTime * 6f);

        if (fill >= 1f) Finish(true);
        else if (timeLeft <= 0f) Finish(false);
    }

    void Finish(bool success)
    {
        Victim v = victim;
        victim = null;
        GameState.InputLocked = false;

        if (success) v.OnKilled();
        else v.OnEscaped(interactor);
    }

    void OnDisable()
    {
        if (IsRunning) { victim = null; GameState.InputLocked = false; }
    }

    // PLACEHOLDER: simple OnGUI pop-up, replace with proper UI art for the final version
    void OnGUI()
    {
        if (!IsRunning) return;
        float s = GameUI.Scale;

        GameUI.Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0.2f, 0, 0, 0.35f)); // red tint

        Rect panel = new Rect(Screen.width / 2f - 260 * s, Screen.height / 2f - 100 * s, 520 * s, 200 * s);
        GameUI.Fill(panel, new Color(0, 0, 0, 0.85f));

        GUI.Label(new Rect(panel.x, panel.y + 15 * s, panel.width, 50 * s), "MASH  [SPACE]!",
                  GameUI.Text(40, TextAnchor.MiddleCenter, FontStyle.Bold));

        Rect bar = new Rect(panel.x + 30 * s, panel.y + 85 * s, panel.width - 60 * s, 36 * s);
        GameUI.Fill(bar, new Color(0.25f, 0.25f, 0.25f));
        GameUI.Fill(new Rect(bar.x, bar.y, bar.width * fill, bar.height),
                    Color.Lerp(new Color(0.7f, 0.05f, 0.05f), new Color(1f, 0.5f, 0.5f), pulse));

        GUI.Label(new Rect(panel.x, panel.y + 135 * s, panel.width, 40 * s), $"{Mathf.Max(0, timeLeft):0.0}s",
                  GameUI.Text(24));
    }
}
