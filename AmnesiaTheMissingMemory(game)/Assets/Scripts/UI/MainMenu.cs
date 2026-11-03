using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Simple main menu: title, START and ACHIEVEMENTS (with a trophy).
// Put it on an empty object in the MainMenu scene, together with a Db Client
// (the Db Client is what loads the achievements list from the database).
// PLACEHOLDER: plain OnGUI look, replace with a proper menu design later.
public class MainMenu : MonoBehaviour
{
    [SerializeField] private string gameScene = "Level1"; // must be in File > Build Profiles > Scene List
    [SerializeField] private string title = "AMNESIA";
    [SerializeField] private string subtitle = "The Missing Memory";

    static readonly Color Gold = new Color(0.95f, 0.75f, 0.2f);
    static readonly Color Grey = new Color(0.35f, 0.35f, 0.35f);
    static readonly Color Blood = new Color(0.75f, 0.05f, 0.05f);

    bool showAchievements;
    AchievementData[] achievements;
    string status; // "Loading..." or an error, null when the list is ready
    Vector2 scroll;

    void Start()
    {
        // the game scene locks the cursor, the menu needs it back
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (showAchievements && kb != null && kb.escapeKey.wasPressedThisFrame) showAchievements = false;
    }

    void OpenAchievements()
    {
        showAchievements = true;
        achievements = null;
        scroll = Vector2.zero;

        if (DbClient.Instance == null) { status = "No Db Client in this scene."; return; }

        status = "Loading...";
        DbClient.Instance.GetAchievements(list =>
        {
            achievements = list;
            if (list == null) status = "Couldn't reach the database. Is the API running?";
            else if (list.Length == 0) status = "No achievements yet.";
            else status = null;
        });
    }

    void OnGUI()
    {
        GameUI.Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0.04f, 0.04f, 0.05f));

        if (showAchievements) DrawAchievements();
        else DrawMain();
    }

    void DrawMain()
    {
        float s = GameUI.Scale;
        float cx = Screen.width / 2f;

        // title
        GUI.Label(new Rect(0, Screen.height * 0.16f, Screen.width, 130 * s), title,
                  GameUI.Text(110, TextAnchor.MiddleCenter, FontStyle.Bold));
        GameUI.Fill(new Rect(cx - 170 * s, Screen.height * 0.16f + 135 * s, 340 * s, 4 * s), Blood);
        GUI.Label(new Rect(0, Screen.height * 0.16f + 150 * s, Screen.width, 50 * s), subtitle,
                  GameUI.Text(36, TextAnchor.MiddleCenter, FontStyle.Italic));

        // buttons
        float w = 380 * s, h = 66 * s, y = Screen.height * 0.56f;
        if (Button(new Rect(cx - w / 2, y, w, h), "START", false))
            SceneManager.LoadScene(gameScene);
        if (Button(new Rect(cx - w / 2, y + h + 20 * s, w, h), "ACHIEVEMENTS", true))
            OpenAchievements();
    }

    void DrawAchievements()
    {
        float s = GameUI.Scale;
        float cx = Screen.width / 2f;

        // header: trophy + "ACHIEVEMENTS"
        float top = Screen.height * 0.08f;
        GameUI.Trophy(new Rect(cx - 230 * s, top + 5 * s, 56 * s, 56 * s), Gold);
        GUI.Label(new Rect(cx - 160 * s, top, 400 * s, 66 * s), "ACHIEVEMENTS",
                  GameUI.Text(52, TextAnchor.MiddleLeft, FontStyle.Bold));

        float w = 760 * s, rowH = 96 * s;
        Rect area = new Rect(cx - w / 2, top + 100 * s, w, Screen.height * 0.62f);

        if (status != null)
        {
            GUI.Label(area, status, GameUI.Text(28));
        }
        else
        {
            // scrolls if there are more achievements than fit on screen
            Rect content = new Rect(0, 0, w - 20 * s, achievements.Length * (rowH + 10 * s));
            scroll = GUI.BeginScrollView(area, scroll, content, false, false);
            for (int i = 0; i < achievements.Length; i++)
                DrawRow(achievements[i], new Rect(0, i * (rowH + 10 * s), content.width, rowH), s);
            GUI.EndScrollView();
        }

        float bw = 240 * s, bh = 58 * s;
        if (Button(new Rect(cx - bw / 2, Screen.height - bh - 50 * s, bw, bh), "BACK", false))
            showAchievements = false;
    }

    void DrawRow(AchievementData a, Rect r, float s)
    {
        GameUI.Fill(r, new Color(0.11f, 0.11f, 0.12f));
        GameUI.Fill(new Rect(r.x, r.y, 6 * s, r.height), a.isUnlocked ? Gold : Grey);
        GameUI.Trophy(new Rect(r.x + 25 * s, r.y + 20 * s, 56 * s, 56 * s), a.isUnlocked ? Gold : Grey);

        GUIStyle nameStyle = GameUI.Text(28, TextAnchor.UpperLeft, FontStyle.Bold);
        GUIStyle descStyle = GameUI.Text(18, TextAnchor.UpperLeft);
        if (!a.isUnlocked) nameStyle.normal.textColor = descStyle.normal.textColor = new Color(0.55f, 0.55f, 0.55f);

        GUI.Label(new Rect(r.x + 105 * s, r.y + 14 * s, r.width - 330 * s, 36 * s), a.name, nameStyle);
        GUI.Label(new Rect(r.x + 105 * s, r.y + 52 * s, r.width - 330 * s, 36 * s), a.description, descStyle);

        string when = "Locked";
        if (a.isUnlocked)
            when = DateTime.TryParse(a.unlockedAt, out DateTime d) ? $"Unlocked {d:dd MMM yyyy}" : "Unlocked";
        GUIStyle whenStyle = GameUI.Text(18, TextAnchor.MiddleRight);
        whenStyle.normal.textColor = a.isUnlocked ? Gold : new Color(0.55f, 0.55f, 0.55f);
        GUI.Label(new Rect(r.xMax - 225 * s, r.y, 200 * s, r.height), when, whenStyle);
    }

    // dark button that turns red on hover, optionally with a trophy on the left
    bool Button(Rect r, string label, bool trophy)
    {
        float s = GameUI.Scale;
        bool hover = r.Contains(Event.current.mousePosition);

        GameUI.Fill(r, hover ? new Color(0.25f, 0.03f, 0.03f) : new Color(0.12f, 0.12f, 0.13f));
        GameUI.Fill(new Rect(r.x, r.y, 6 * s, r.height), hover ? Blood : Grey);
        if (trophy)
        {
            float icon = r.height * 0.55f;
            GameUI.Trophy(new Rect(r.x + 26 * s, r.y + (r.height - icon) / 2, icon, icon), Gold);
        }
        GUI.Label(r, label, GameUI.Text(30, TextAnchor.MiddleCenter, FontStyle.Bold));

        return GUI.Button(r, GUIContent.none, GUIStyle.none);
    }
}
