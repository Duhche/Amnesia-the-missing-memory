using UnityEngine;
using UnityEngine.InputSystem;

// the kill pop-up when you attack a victim. two kinds:
// no knife: mash space to fill the bar before time runs out.
// knife: a line moves back and forth, hit space when it's in the red zone.
// put one on the GameSystems object.
public class KillMinigame : MonoBehaviour
{
    public static KillMinigame Instance { get; private set; }

    [Header("Bare hands (mash)")]
    [SerializeField] private float pressPower = 0.1f;      // how much one press fills
    [SerializeField] private float drainPerSecond = 0.25f; // the victim fighting back
    [SerializeField] private float startFill = 0.2f;
    [SerializeField] private float timeLimit = 6f;

    [Header("Knife (timing)")]
    [SerializeField] private float lineSpeed = 1.2f;  // how fast the line moves
    [SerializeField] private float zoneWidth = 0.15f; // how big the red zone is
    [SerializeField] private int allowedMisses = 2;   // misses before they break free

    public bool IsRunning => victim != null;

    Victim victim;
    PlayerInteractor interactor;
    bool withKnife;

    // mash stuff
    float fill;
    float timeLeft;
    float pulse; // flash when you press

    // knife stuff
    float linePos;
    float lineDir = 1;
    float zoneStart;
    int misses;
    float missFlash;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    public void Begin(Victim target, PlayerInteractor player, bool knife)
    {
        if (IsRunning) return;
        victim = target;
        interactor = player;
        withKnife = knife;

        fill = startFill;
        timeLeft = timeLimit;

        linePos = 0;
        lineDir = 1;
        zoneStart = Random.Range(0.35f, 0.85f - zoneWidth); // random spot, but not right at the start
        misses = 0;
        missFlash = 0;

        GameState.InputLocked = true; // player can't move during the fight
    }

    void Update()
    {
        if (!IsRunning || PauseMenu.IsPaused) return; // space presses don't count while paused

        Keyboard kb = Keyboard.current;
        bool space = kb != null && kb.spaceKey.wasPressedThisFrame;

        if (withKnife) UpdateKnife(space);
        else UpdateMash(space);
    }

    void UpdateMash(bool space)
    {
        if (space)
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

    void UpdateKnife(bool space)
    {
        // move the line and bounce at the ends
        linePos += lineDir * lineSpeed * Time.deltaTime;
        if (linePos >= 1f) { linePos = 1f; lineDir = -1; }
        if (linePos <= 0f) { linePos = 0f; lineDir = 1; }
        missFlash = Mathf.MoveTowards(missFlash, 0, Time.deltaTime * 3f);

        if (!space) return;

        // hit
        if (linePos >= zoneStart && linePos <= zoneStart + zoneWidth)
        {
            Finish(true);
            return;
        }

        // miss
        misses++;
        missFlash = 1f;
        if (misses > allowedMisses) Finish(false);
    }

    void Finish(bool success)
    {
        Victim v = victim;
        victim = null;
        GameState.InputLocked = false;

        if (success) v.OnKilled(withKnife);
        else v.OnEscaped(interactor);
    }

    // if this gets turned off mid fight, give the player control back
    void OnDisable()
    {
        if (IsRunning) { victim = null; GameState.InputLocked = false; }
    }

    // placeholder: basic look, make it nicer later
    void OnGUI()
    {
        if (!IsRunning) return;
        float s = GameUI.Scale;

        GameUI.Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0.2f, 0, 0, 0.35f)); // red tint over the screen

        Rect panel = new Rect(Screen.width / 2f - 260 * s, Screen.height / 2f - 100 * s, 520 * s, 200 * s);
        GameUI.Fill(panel, new Color(0, 0, 0, 0.85f));
        Rect bar = new Rect(panel.x + 30 * s, panel.y + 85 * s, panel.width - 60 * s, 36 * s);

        if (withKnife) DrawKnife(panel, bar, s);
        else DrawMash(panel, bar, s);
    }

    void DrawMash(Rect panel, Rect bar, float s)
    {
        GUI.Label(new Rect(panel.x, panel.y + 15 * s, panel.width, 50 * s), "MASH  [SPACE]!",
                  GameUI.Text(40, TextAnchor.MiddleCenter, FontStyle.Bold));

        GameUI.Fill(bar, new Color(0.25f, 0.25f, 0.25f));
        GameUI.Fill(new Rect(bar.x, bar.y, bar.width * fill, bar.height),
                    Color.Lerp(new Color(0.7f, 0.05f, 0.05f), new Color(1f, 0.5f, 0.5f), pulse));

        GUI.Label(new Rect(panel.x, panel.y + 135 * s, panel.width, 40 * s), $"{Mathf.Max(0, timeLeft):0.0}s",
                  GameUI.Text(24));
    }

    void DrawKnife(Rect panel, Rect bar, float s)
    {
        GUI.Label(new Rect(panel.x, panel.y + 15 * s, panel.width, 50 * s), "STRIKE  [SPACE]!",
                  GameUI.Text(40, TextAnchor.MiddleCenter, FontStyle.Bold));

        // bar goes red for a sec when you miss
        GameUI.Fill(bar, Color.Lerp(new Color(0.25f, 0.25f, 0.25f), new Color(0.5f, 0.1f, 0.1f), missFlash));
        GameUI.Fill(new Rect(bar.x + bar.width * zoneStart, bar.y, bar.width * zoneWidth, bar.height),
                    new Color(0.8f, 0.05f, 0.05f));
        GameUI.Fill(new Rect(bar.x + bar.width * linePos - 3 * s, bar.y - 8 * s, 6 * s, bar.height + 16 * s), Color.white);

        int triesLeft = allowedMisses + 1 - misses;
        GUI.Label(new Rect(panel.x, panel.y + 135 * s, panel.width, 40 * s),
                  triesLeft == 1 ? "Last chance" : $"{triesLeft} tries left", GameUI.Text(24));
    }
}
