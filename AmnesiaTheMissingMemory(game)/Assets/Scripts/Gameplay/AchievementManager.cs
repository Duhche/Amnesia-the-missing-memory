using System.Collections.Generic;
using UnityEngine;

// Shows the "Achievement Unlocked" pop-up and saves it to the database through DbClient.
// Put ONE of these on the "GameSystems" object. Call it from anywhere with:
//     AchievementManager.Instance.Unlock("Night 1 Complete", "You finished the first night.");
// The name must match a row in the Achievements table.
public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    [SerializeField] private float showSeconds = 4f;
    [SerializeField] private float slideSeconds = 0.35f;

    struct Popup { public string name; public string description; }

    readonly HashSet<string> unlockedThisSession = new HashSet<string>();
    readonly Queue<Popup> waiting = new Queue<Popup>();
    Popup? showing;
    float shownFor;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    public void Unlock(string achievementName, string description = "")
    {
        if (!unlockedThisSession.Add(achievementName)) return; // don't show the same one twice

        waiting.Enqueue(new Popup { name = achievementName, description = description });

        if (DbClient.Instance != null)
            DbClient.Instance.UnlockAchievement(achievementName, ok =>
            {
                if (!ok) Debug.LogWarning($"Achievement '{achievementName}' was shown but NOT saved (is the API running?)");
            });
        else
            Debug.LogWarning("No DbClient in the scene, achievement not saved.");
    }

    void Update()
    {
        if (showing == null)
        {
            if (waiting.Count == 0) return;
            showing = waiting.Dequeue();
            shownFor = 0;
            // PLACEHOLDER: play an achievement sound here
        }

        shownFor += Time.unscaledDeltaTime;
        if (shownFor > showSeconds) showing = null;
    }

    // PLACEHOLDER: simple OnGUI pop-up, replace with proper UI art for the final version
    void OnGUI()
    {
        if (showing == null) return;
        float s = GameUI.Scale;

        // slide in from the right, slide out at the end
        float t = Mathf.Min(shownFor, showSeconds - shownFor) / slideSeconds;
        float slide = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t));

        float w = 420 * s, h = 110 * s;
        Rect box = new Rect(Screen.width - (w + 30 * s) * slide, 30 * s, w, h);

        GameUI.Fill(box, new Color(0.05f, 0.05f, 0.05f, 0.92f));
        GameUI.Fill(new Rect(box.x, box.y, 8 * s, h), new Color(0.75f, 0.05f, 0.05f)); // red stripe

        GUI.Label(new Rect(box.x + 25 * s, box.y + 10 * s, w - 35 * s, 24 * s), "ACHIEVEMENT UNLOCKED",
                  GameUI.Text(16, TextAnchor.UpperLeft, FontStyle.Bold));
        GUI.Label(new Rect(box.x + 25 * s, box.y + 34 * s, w - 35 * s, 36 * s), showing.Value.name,
                  GameUI.Text(28, TextAnchor.UpperLeft, FontStyle.Bold));
        GUI.Label(new Rect(box.x + 25 * s, box.y + 72 * s, w - 35 * s, 30 * s), showing.Value.description,
                  GameUI.Text(16, TextAnchor.UpperLeft));
    }
}
