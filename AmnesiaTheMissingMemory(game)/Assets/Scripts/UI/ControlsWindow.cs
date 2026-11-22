using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// shows the controls when the first night starts. press E to close it and start playing.
// it makes itself when the game starts, nothing to set up.
public class ControlsWindow : MonoBehaviour
{
    const string ShowInScene = "Level1(night)";

    static readonly (string key, string action)[] Controls =
    {
        ("W A S D", "Walk"),
        ("MOUSE", "Look around"),
        ("E", "Interact / pick up / kill"),
        ("SPACE", "Kill minigame"),
        ("ESC", "Pause"),
    };

    bool showing;
    bool unlockNextFrame; // so the E that closes this doesn't also use something in front of you

    // runs once when you hit Play, the object then stays alive through every scene
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        GameObject go = new GameObject("ControlsWindow");
        ControlsWindow window = go.AddComponent<ControlsWindow>();
        DontDestroyOnLoad(go);
        window.showing = SceneManager.GetActiveScene().name == ShowInScene; // if you hit Play right in Night 1
    }

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => showing = scene.name == ShowInScene;

    void Update()
    {
        if (unlockNextFrame) { unlockNextFrame = false; GameState.InputLocked = false; }
        if (!showing || PauseMenu.IsPaused || SceneFader.IsBusy) return;

        GameState.InputLocked = true; // can't walk around while reading

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.eKey.wasPressedThisFrame)
        {
            showing = false;
            unlockNextFrame = true;
        }
    }

    void OnGUI()
    {
        if (!showing) return;
        GUI.depth = -100; // over the game, under the pause menu and the fade
        float s = GameUI.Scale;

        float w = 560 * s, h = 450 * s;
        Rect panel = new Rect((Screen.width - w) / 2, (Screen.height - h) / 2, w, h);
        GameUI.Fill(panel, new Color(0, 0, 0, 0.88f));
        GameUI.Fill(new Rect(panel.x, panel.y, panel.width, 6 * s), new Color(0.75f, 0.05f, 0.05f)); // red line on top

        GUI.Label(new Rect(panel.x, panel.y + 20 * s, w, 60 * s), "CONTROLS",
                  GameUI.Text(44, TextAnchor.MiddleCenter, FontStyle.Bold));

        // one row per control: key in a grey box, what it does next to it
        float rowY = panel.y + 100 * s;
        foreach (var (key, action) in Controls)
        {
            Rect keyBox = new Rect(panel.x + 50 * s, rowY, 160 * s, 44 * s);
            GameUI.Fill(keyBox, new Color(0.2f, 0.2f, 0.22f));
            GUI.Label(keyBox, key, GameUI.Text(22, TextAnchor.MiddleCenter, FontStyle.Bold));
            GUI.Label(new Rect(keyBox.xMax + 25 * s, rowY, w - 260 * s, 44 * s), action,
                      GameUI.Text(24, TextAnchor.MiddleLeft));
            rowY += 56 * s;
        }

        GUIStyle hint = GameUI.Text(22, TextAnchor.MiddleCenter, FontStyle.Italic);
        hint.normal.textColor = new Color(0.75f, 0.75f, 0.75f);
        GUI.Label(new Rect(panel.x, panel.yMax - 55 * s, w, 40 * s), "Press [E] to start", hint);
    }
}
