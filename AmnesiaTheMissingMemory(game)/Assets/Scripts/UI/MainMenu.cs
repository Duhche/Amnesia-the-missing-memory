using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;

// main menu: title, START (opens the 3 save slots) and ACHIEVEMENTS (per save)
public class MainMenu : MonoBehaviour
{
    [SerializeField] private string gameScene = "Level1(night)"; // where a new save starts
    [SerializeField] private string title = "AMNESIA";
    [SerializeField] private string subtitle = "The Missing Memory";

    const int SlotCount = 3;

    static readonly Color Gold = new Color(0.95f, 0.75f, 0.2f);
    static readonly Color Grey = new Color(0.35f, 0.35f, 0.35f);
    static readonly Color Blood = new Color(0.75f, 0.05f, 0.05f);
    static readonly Color Faded = new Color(0.55f, 0.55f, 0.55f);

    enum Page { Main, Saves, Achievements }
    Page page = Page.Main;

    SaveData[] saves;      // the saves that exist, null while loading or if the API is off
    string status;         // "Loading..." or an error, null when everything loaded
    bool busy;             // waiting on the API, so buttons don't get double clicked
    int confirmDelete;     // which slot's DELETE was clicked once (0 = none)

    int achievementsSlot = 1;
    AchievementData[] achievements;
    string achievementsStatus;
    Vector2 scroll;

    // Awake runs before the scene counts as loaded, so SaveSession won't save "MainMenu" as a level
    void Awake() => SaveSession.Slot = 0;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (page != Page.Main && kb != null && kb.escapeKey.wasPressedThisFrame) page = Page.Main;
    }

    // ---- loading stuff from the API ----

    void LoadSaves(Action then = null)
    {
        saves = null;
        confirmDelete = 0;

        if (DbClient.Instance == null) { status = "No Db Client in this scene."; then?.Invoke(); return; }

        status = "Loading...";
        DbClient.Instance.GetSaves(list =>
        {
            saves = list;
            status = list == null ? "Couldn't reach the database. Is the API running?" : null;
            then?.Invoke();
        });
    }

    SaveData FindSave(int slot)
    {
        if (saves == null) return null;
        foreach (SaveData s in saves) if (s.slot == slot) return s;
        return null;
    }

    void OpenSaves()
    {
        page = Page.Saves;
        LoadSaves();
    }

    void OpenAchievements()
    {
        page = Page.Achievements;
        achievements = null;
        achievementsStatus = "Loading...";

        // start on the first save that exists
        LoadSaves(() =>
        {
            achievementsSlot = 1;
            if (saves != null && saves.Length > 0) achievementsSlot = saves[0].slot;
            LoadAchievements(achievementsSlot);
        });
    }

    void LoadAchievements(int slot)
    {
        achievementsSlot = slot;
        achievements = null;
        scroll = Vector2.zero;

        if (saves == null) { achievementsStatus = status; return; }
        if (FindSave(slot) == null) { achievementsStatus = "Nothing saved in this slot yet."; return; }

        achievementsStatus = "Loading...";
        DbClient.Instance.GetAchievements(slot, list =>
        {
            achievements = list;
            if (list == null) achievementsStatus = "Couldn't reach the database. Is the API running?";
            else if (list.Length == 0) achievementsStatus = "No achievements yet.";
            else achievementsStatus = null;
        });
    }

    // ---- what the save buttons do ----

    void NewGame(int slot)
    {
        busy = true;
        status = "Creating save...";
        DbClient.Instance.CreateSave(slot, ok =>
        {
            busy = false;
            if (!ok) { status = "Couldn't create the save."; return; }
            SaveSession.Slot = slot;
            SceneFader.GoTo(gameScene);
        });
    }

    void Continue(SaveData save)
    {
        SaveSession.Slot = save.slot;
        SceneFader.GoTo(save.currentLevel);
    }

    void Delete(int slot)
    {
        busy = true;
        DbClient.Instance.DeleteSave(slot, ok =>
        {
            busy = false;
            if (!ok) { status = "Couldn't delete the save."; return; }
            LoadSaves();
        });
    }

    // turns "Level2(night)" into "Night 2"
    static string LevelName(string scene)
    {
        Match m = Regex.Match(scene ?? "", @"^Level(\d+)\((night|day)\)$", RegexOptions.IgnoreCase);
        if (!m.Success) return scene;
        return (m.Groups[2].Value.ToLower() == "night" ? "Night " : "Day ") + m.Groups[1].Value;
    }

    static string ShortDate(string date) =>
        DateTime.TryParse(date, out DateTime d) ? d.ToString("dd MMM yyyy") : "";

    // ---- drawing ----

    void OnGUI()
    {
        GameUI.Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0.04f, 0.04f, 0.05f));

        if (page == Page.Saves) DrawSaves();
        else if (page == Page.Achievements) DrawAchievements();
        else DrawMain();
    }

    void DrawMain()
    {
        float s = GameUI.Scale;
        float sw = Screen.width, sh = Screen.height;
        float cx = sw / 2f;

        // title
        GUI.Label(new Rect(0, sh * 0.16f, sw, 130 * s), title,
                  GameUI.Text(110, TextAnchor.MiddleCenter, FontStyle.Bold));
        GameUI.Fill(new Rect(cx - 170 * s, sh * 0.16f + 135 * s, 340 * s, 4 * s), Blood);
        GUI.Label(new Rect(0, sh * 0.16f + 150 * s, sw, 50 * s), subtitle,
                  GameUI.Text(36, TextAnchor.MiddleCenter, FontStyle.Italic));

        // buttons
        float w = 380 * s, h = 66 * s, y = sh * 0.56f;
        if (Button(new Rect(cx - w / 2, y, w, h), "START", false))
            OpenSaves();
        if (Button(new Rect(cx - w / 2, y + h + 20 * s, w, h), "ACHIEVEMENTS", true))
            OpenAchievements();
    }

    void DrawSaves()
    {
        float s = GameUI.Scale;
        float sw = Screen.width, sh = Screen.height;
        float cx = sw / 2f;
        float top = sh * 0.08f;

        GUI.Label(new Rect(0, top, sw, 66 * s), "SAVE SLOTS", GameUI.Text(52, TextAnchor.MiddleCenter, FontStyle.Bold));

        float w = 760 * s, h = 130 * s, gap = 20 * s;
        float y = top + 100 * s;

        if (saves == null)
        {
            GUI.Label(new Rect(cx - w / 2, y, w, 200 * s), status, GameUI.Text(28));

            // API is off, you can still play, it just won't save anything
            if (status != null && status != "Loading..." &&
                Button(new Rect(cx - 220 * s, y + 220 * s, 440 * s, 58 * s), "PLAY WITHOUT SAVING", false, 24))
            {
                SaveSession.Slot = 0;
                SceneFader.GoTo(gameScene);
            }
        }
        else
        {
            for (int slot = 1; slot <= SlotCount; slot++)
                DrawSlot(slot, new Rect(cx - w / 2, y + (slot - 1) * (h + gap), w, h), s);

            if (status != null)
                GUI.Label(new Rect(cx - w / 2, y + SlotCount * (h + gap), w, 40 * s), status, GameUI.Text(22));
        }

        float bw = 240 * s, bh = 58 * s;
        if (Button(new Rect(cx - bw / 2, sh - bh - 50 * s, bw, bh), "BACK", false))
            page = Page.Main;
    }

    void DrawSlot(int slot, Rect r, float s)
    {
        SaveData save = FindSave(slot);

        GameUI.Fill(r, new Color(0.11f, 0.11f, 0.12f));
        GameUI.Fill(new Rect(r.x, r.y, 6 * s, r.height), save != null ? Blood : Grey);

        GUI.Label(new Rect(r.x + 30 * s, r.y + 14 * s, 300 * s, 30 * s), $"SAVE {slot}",
                  GameUI.Text(20, TextAnchor.UpperLeft, FontStyle.Bold));

        float bw = 170 * s, bh = 48 * s;
        Rect right = new Rect(r.xMax - bw - 25 * s, r.y + (r.height - bh) / 2, bw, bh);

        if (save == null)
        {
            GUIStyle empty = GameUI.Text(32, TextAnchor.UpperLeft, FontStyle.Bold);
            empty.normal.textColor = Faded;
            GUI.Label(new Rect(r.x + 30 * s, r.y + 45 * s, 400 * s, 44 * s), "Empty", empty);

            if (Button(right, "NEW", false, 24) && !busy) NewGame(slot);
            return;
        }

        // which level it's on, plus when you last played and what you've got
        GUI.Label(new Rect(r.x + 30 * s, r.y + 45 * s, 400 * s, 44 * s), LevelName(save.currentLevel),
                  GameUI.Text(32, TextAnchor.UpperLeft, FontStyle.Bold));
        GUIStyle info = GameUI.Text(16, TextAnchor.UpperLeft);
        info.normal.textColor = Faded;
        GUI.Label(new Rect(r.x + 30 * s, r.y + 90 * s, 380 * s, 28 * s),
                  $"{ShortDate(save.lastPlayed)}  ·  {save.achievementsUnlocked} achievements  ·  {save.collectiblesFound} collectibles", info);

        // DELETE has to be clicked twice so you don't lose a save by accident
        Rect left = new Rect(right.x - bw - 15 * s, right.y, bw, bh);
        if (Button(left, "CONTINUE", false, 24) && !busy) Continue(save);
        if (Button(right, confirmDelete == slot ? "SURE?" : "DELETE", false, 24) && !busy)
        {
            if (confirmDelete == slot) Delete(slot);
            else confirmDelete = slot;
        }
    }

    void DrawAchievements()
    {
        float s = GameUI.Scale;
        float sw = Screen.width, sh = Screen.height;
        float cx = sw / 2f;

        // trophy and the "ACHIEVEMENTS" title at the top
        float top = sh * 0.06f;
        GameUI.Trophy(new Rect(cx - 230 * s, top + 5 * s, 56 * s, 56 * s), Gold);
        GUI.Label(new Rect(cx - 160 * s, top, 400 * s, 66 * s), "ACHIEVEMENTS",
                  GameUI.Text(52, TextAnchor.MiddleLeft, FontStyle.Bold));

        // pick which save to look at
        float tw = 200 * s, th = 48 * s, tgap = 15 * s;
        float tx = cx - (SlotCount * tw + (SlotCount - 1) * tgap) / 2;
        for (int slot = 1; slot <= SlotCount; slot++)
        {
            Rect tab = new Rect(tx + (slot - 1) * (tw + tgap), top + 85 * s, tw, th);
            if (slot == achievementsSlot) GameUI.Fill(new Rect(tab.x, tab.yMax, tab.width, 4 * s), Gold);
            if (Button(tab, $"SAVE {slot}", false, 22) && saves != null) LoadAchievements(slot);
        }

        float w = 760 * s, rowH = 96 * s;
        Rect area = new Rect(cx - w / 2, top + 160 * s, w, sh * 0.55f);

        if (achievementsStatus != null)
        {
            GUI.Label(area, achievementsStatus, GameUI.Text(28));
        }
        else
        {
            // lets you scroll if there are too many to fit
            Rect content = new Rect(0, 0, w - 20 * s, achievements.Length * (rowH + 10 * s));
            scroll = GUI.BeginScrollView(area, scroll, content, false, false);
            for (int i = 0; i < achievements.Length; i++)
                DrawRow(achievements[i], new Rect(0, i * (rowH + 10 * s), content.width, rowH), s);
            GUI.EndScrollView();
        }

        float bw = 240 * s, bh = 58 * s;
        if (Button(new Rect(cx - bw / 2, sh - bh - 50 * s, bw, bh), "BACK", false))
            page = Page.Main;
    }

    void DrawRow(AchievementData a, Rect r, float s)
    {
        GameUI.Fill(r, new Color(0.11f, 0.11f, 0.12f));
        GameUI.Fill(new Rect(r.x, r.y, 6 * s, r.height), a.isUnlocked ? Gold : Grey);
        GameUI.Trophy(new Rect(r.x + 25 * s, r.y + 20 * s, 56 * s, 56 * s), a.isUnlocked ? Gold : Grey);

        GUIStyle nameStyle = GameUI.Text(28, TextAnchor.UpperLeft, FontStyle.Bold);
        GUIStyle descStyle = GameUI.Text(18, TextAnchor.UpperLeft);
        if (!a.isUnlocked) nameStyle.normal.textColor = descStyle.normal.textColor = Faded;

        GUI.Label(new Rect(r.x + 105 * s, r.y + 14 * s, r.width - 330 * s, 36 * s), a.name, nameStyle);
        GUI.Label(new Rect(r.x + 105 * s, r.y + 52 * s, r.width - 330 * s, 36 * s), a.description, descStyle);

        GUIStyle stateStyle = GameUI.Text(18, TextAnchor.MiddleRight);
        stateStyle.normal.textColor = a.isUnlocked ? Gold : Faded;
        GUI.Label(new Rect(r.xMax - 225 * s, r.y, 200 * s, r.height), a.isUnlocked ? "Unlocked" : "Locked", stateStyle);
    }

    bool Button(Rect r, string label, bool trophy, int fontSize = 30) => GameUI.Button(r, label, fontSize, trophy);
}
