using UnityEngine;
using UnityEngine.InputSystem;

// pause menu in the levels: press Esc for resume, main menu or quit.
// it makes itself when the game starts, nothing to set up.
public class PauseMenu : MonoBehaviour
{
    const string MenuScene = "MainMenu";

    public static bool IsPaused { get; private set; }

    bool lockedBefore; // what InputLocked was before pausing, so a minigame stays locked after resume

    // runs once when you hit Play, the object then stays alive through every scene
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        IsPaused = false;
        GameObject go = new GameObject("PauseMenu");
        go.AddComponent<PauseMenu>();
        DontDestroyOnLoad(go);
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;

        if (IsPaused) Resume();
        else if (CanPause()) Pause();
    }

    // not in the main menu and not in the middle of a fade
    static bool CanPause() => !SceneFader.IsBusy && FindAnyObjectByType<MainMenu>() == null;

    void Pause()
    {
        IsPaused = true;
        lockedBefore = GameState.InputLocked;
        GameState.InputLocked = true;
        Time.timeScale = 0; // freezes everything that moves
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Resume()
    {
        IsPaused = false;
        GameState.InputLocked = lockedBefore;
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // your save already has this level, so CONTINUE starts you back here
    void GoToMainMenu()
    {
        IsPaused = false;
        Time.timeScale = 1;
        SceneFader.GoTo(MenuScene, false);
    }

    void Quit()
    {
        Time.timeScale = 1;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // stops Play mode in the editor
#else
        Application.Quit();
#endif
    }

    // so time isn't stuck frozen if the game stops while paused
    void OnDestroy() => Time.timeScale = 1;

    void OnGUI()
    {
        if (!IsPaused) return;
        GUI.depth = -500; // over the game, under the scene fade
        float s = GameUI.Scale;
        float cx = Screen.width / 2f;

        GameUI.Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0, 0, 0.75f));
        GUI.Label(new Rect(0, Screen.height * 0.22f, Screen.width, 90 * s), "PAUSED",
                  GameUI.Text(72, TextAnchor.MiddleCenter, FontStyle.Bold));

        float w = 380 * s, h = 66 * s, gap = 20 * s, y = Screen.height * 0.42f;
        if (GameUI.Button(new Rect(cx - w / 2, y, w, h), "RESUME")) Resume();
        if (GameUI.Button(new Rect(cx - w / 2, y + (h + gap), w, h), "MAIN MENU")) GoToMainMenu();
        if (GameUI.Button(new Rect(cx - w / 2, y + 2 * (h + gap), w, h), "QUIT GAME")) Quit();
    }
}
