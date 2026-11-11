using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;

[System.Serializable]
public class ProgressData
{
    public int currentChapter;
    public string lastCheckpoint;
    public int playtimeMinutes;
}

[System.Serializable]
class AchievementUnlockRequest
{
    public string name;
}

[System.Serializable]
public class AchievementData
{
    public int id;
    public string name;
    public string description;
    public bool isUnlocked;
    public string unlockedAt; // empty if it's still locked
}

[System.Serializable]
class AchievementList
{
    public AchievementData[] items;
}

[System.Serializable]
public class SaveData
{
    public int slot;
    public string currentLevel; // scene name, like "Level1(day)"
    public string createdAt;
    public string lastPlayed;
    public int achievementsUnlocked;
    public int collectiblesFound;
}

[System.Serializable]
class SaveList
{
    public SaveData[] items;
}

[System.Serializable]
class SaveUpdateRequest
{
    public string currentLevel;
}

// connects to AmnesiaAPI which connects to the MySQL database.
public class DbClient : MonoBehaviour
{
    public static DbClient Instance { get; private set; }

    [SerializeField] private string baseUrl = "http://localhost:5112";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        baseUrl = baseUrl.TrimEnd('/');
    }

    // test to see if the Console is linked with the database
    void Start()
    {
        GetProgress(p =>
        {
            if (p != null) Debug.Log($"[DbClient] Connected. Chapter {p.currentChapter}, checkpoint: {p.lastCheckpoint}");
        });
    }

    public void GetProgress(Action<ProgressData> done)
    {
        StartCoroutine(Send("GET", "/api/progress", null, req =>
            done?.Invoke(req == null ? null : JsonUtility.FromJson<ProgressData>(req.downloadHandler.text))));
    }

    public void SaveProgress(ProgressData data, Action<bool> done = null)
    {
        StartCoroutine(Send("PUT", "/api/progress", JsonUtility.ToJson(data), req => done?.Invoke(req != null)));
    }

    // marks a collectible as found for the save you're playing right now
    public void CollectItem(int id, Action<bool> done = null)
    {
        StartCoroutine(Send("POST", $"/api/saves/{SaveSession.Slot}/collectibles/{id}/collect", null, req => done?.Invoke(req != null)));
    }

    // ---- save slots ----

    // gets the saves that exist, or null if the API isn't running
    public void GetSaves(Action<SaveData[]> done)
    {
        StartCoroutine(Send("GET", "/api/saves", null, req =>
            done?.Invoke(req == null ? null
                : JsonUtility.FromJson<SaveList>("{\"items\":" + req.downloadHandler.text + "}").items)));
    }

    public void CreateSave(int slot, Action<bool> done = null)
    {
        StartCoroutine(Send("POST", $"/api/saves/{slot}", null, req => done?.Invoke(req != null)));
    }

    public void UpdateSaveLevel(int slot, string level, Action<bool> done = null)
    {
        string body = JsonUtility.ToJson(new SaveUpdateRequest { currentLevel = level });
        StartCoroutine(Send("PUT", $"/api/saves/{slot}", body, req => done?.Invoke(req != null)));
    }

    public void DeleteSave(int slot, Action<bool> done = null)
    {
        StartCoroutine(Send("DELETE", $"/api/saves/{slot}", null, req => done?.Invoke(req != null)));
    }

    // ---- achievements (each save has its own) ----

    // gets all the achievements for one save, or null if the API isn't running
    public void GetAchievements(int slot, Action<AchievementData[]> done)
    {
        StartCoroutine(Send("GET", $"/api/saves/{slot}/achievements", null, req =>
            done?.Invoke(req == null ? null
                : JsonUtility.FromJson<AchievementList>("{\"items\":" + req.downloadHandler.text + "}").items)));
    }

    // unlocks it for the save you're playing right now
    public void UnlockAchievement(string achievementName, Action<bool> done = null)
    {
        string body = JsonUtility.ToJson(new AchievementUnlockRequest { name = achievementName });
        StartCoroutine(Send("POST", $"/api/saves/{SaveSession.Slot}/achievements/unlock", body, req => done?.Invoke(req != null)));
    }

    // sends a request to the API, gives back null if it didn't work
    IEnumerator Send(string method, string path, string json, Action<UnityWebRequest> done)
    {
        using UnityWebRequest req = new UnityWebRequest(baseUrl + path, method);
        req.downloadHandler = new DownloadHandlerBuffer();
        if (json != null)
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.SetRequestHeader("Content-Type", "application/json");
        }
        req.timeout = 5;

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success) done?.Invoke(req);
        else
        {
            Debug.LogError($"[DbClient] {method} {path} failed: {req.error} {req.downloadHandler.text}");
            done?.Invoke(null);
        }
    }
}
