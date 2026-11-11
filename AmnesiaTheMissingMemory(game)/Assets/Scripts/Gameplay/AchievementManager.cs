using System.Collections.Generic;
using UnityEngine;

// one achievement typed in the Inspector (the name has to match the database)
[System.Serializable]
public struct AchievementInfo
{
    public string name;
    public string description;
}

// shows the "Achievement Unlocked" pop-up and saves it to the database.
// put one on the GameSystems object. use it from anywhere like this:
//     AchievementManager.Instance.Unlock("Night 1 Complete", "You finished the first night.");
public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    [SerializeField] private float showSeconds = 4f;
    [SerializeField] private float slideSeconds = 0.35f;

    struct Popup { public string name; public string description; }

    readonly HashSet<string> unlockedThisSession = new HashSet<string>();
    readonly Queue<Popup> waiting = new Queue<Popup>(); // pop-ups waiting their turn
    Popup? showing;
    float shownFor;

    // true while a pop-up is showing or waiting. SceneFader waits for this before changing scene
    public bool IsBusy => showing != null || waiting.Count > 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    // unlocks a whole list (PickupItem and Inspectable use this)
    public static void UnlockAll(AchievementInfo[] list)
    {
        if (list == null || list.Length == 0) return;
        if (Instance == null) { Debug.LogWarning("No AchievementManager in the scene, achievements not shown."); return; }
        foreach (AchievementInfo a in list) Instance.Unlock(a.name, a.description);
    }

    public void Unlock(string achievementName, string description = "")
    {
        if (!unlockedThisSession.Add(achievementName)) return; // already got it this run

        waiting.Enqueue(new Popup { name = achievementName, description = description });

        // save it in the database for the save you're playing
        if (SaveSession.Slot == 0)
            Debug.Log($"Achievement '{achievementName}' not saved, you're not playing on a save slot.");
        else if (DbClient.Instance != null)
            DbClient.Instance.UnlockAchievement(achievementName, ok =>
            {
                if (!ok) Debug.LogWarning($"Achievement '{achievementName}' was shown but NOT saved (is the API running?)");
            });
        else
            Debug.LogWarning("No DbClient in the scene, achievement not saved.");
    }

    // shows the pop-ups one at a time
    void Update()
    {
        if (showing == null)
        {
            if (waiting.Count == 0) return;
            showing = waiting.Dequeue();
            shownFor = 0;
            // placeholder: play a sound here later
        }

        shownFor += Time.unscaledDeltaTime;
        if (shownFor > showSeconds) showing = null;
    }

    // placeholder: basic pop-up look, make it nicer later
    void OnGUI()
    {
        if (showing == null) return;
        float s = GameUI.Scale;

        // slides in from the right and back out at the end
        float t = Mathf.Min(shownFor, showSeconds - shownFor) / slideSeconds;
        float slide = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t));

        float w = 420 * s, h = 110 * s;
        Rect box = new Rect(Screen.width - (w + 30 * s) * slide, 30 * s, w, h);

        GameUI.Fill(box, new Color(0.05f, 0.05f, 0.05f, 0.92f));
        GameUI.Fill(new Rect(box.x, box.y, 8 * s, h), new Color(0.75f, 0.05f, 0.05f)); // red line on the side

        GUI.Label(new Rect(box.x + 25 * s, box.y + 10 * s, w - 35 * s, 24 * s), "ACHIEVEMENT UNLOCKED",
                  GameUI.Text(16, TextAnchor.UpperLeft, FontStyle.Bold));
        GUI.Label(new Rect(box.x + 25 * s, box.y + 34 * s, w - 35 * s, 36 * s), showing.Value.name,
                  GameUI.Text(28, TextAnchor.UpperLeft, FontStyle.Bold));
        GUI.Label(new Rect(box.x + 25 * s, box.y + 72 * s, w - 35 * s, 30 * s), showing.Value.description,
                  GameUI.Text(16, TextAnchor.UpperLeft));
    }
}
