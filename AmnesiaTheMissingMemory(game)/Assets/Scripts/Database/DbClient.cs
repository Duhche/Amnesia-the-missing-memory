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
    public string unlockedAt; // empty when locked
}

// JsonUtility can't read a bare [ ... ] list, so the response gets wrapped in this
[System.Serializable]
class AchievementList
{
    public AchievementData[] items;
}

// Talks to AmnesiaAPI, which talks to the MySQL database.
// Put ONE of these on the "GameSystems" object. Other scripts use DbClient.Instance.
public class DbClient : MonoBehaviour
{
    public static DbClient Instance { get; private set; }

    // the "http" address AmnesiaAPI prints when it starts (see AmnesiaAPI/Properties/launchSettings.json)
    [SerializeField] private string baseUrl = "http://localhost:5112";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        baseUrl = baseUrl.TrimEnd('/');
    }

    // quick connection test so you can see in the Console if the database is linked
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

    public void CollectItem(int id, Action<bool> done = null)
    {
        StartCoroutine(Send("POST", $"/api/collectibles/{id}/collect", null, req => done?.Invoke(req != null)));
    }

    // done gets every achievement, or null if the API couldn't be reached
    public void GetAchievements(Action<AchievementData[]> done)
    {
        StartCoroutine(Send("GET", "/api/achievements", null, req =>
            done?.Invoke(req == null ? null
                : JsonUtility.FromJson<AchievementList>("{\"items\":" + req.downloadHandler.text + "}").items)));
    }

    public void UnlockAchievement(string achievementName, Action<bool> done = null)
    {
        string body = JsonUtility.ToJson(new AchievementUnlockRequest { name = achievementName });
        StartCoroutine(Send("POST", "/api/achievements/unlock", body, req => done?.Invoke(req != null)));
    }

    // sends one request; done gets the finished request, or null if it failed
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
