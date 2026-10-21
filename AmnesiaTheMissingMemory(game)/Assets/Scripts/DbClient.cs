using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

[System.Serializable]
public class ProgressData
{
    public int currentChapter;
    public string lastCheckpoint;
    public int playtimeMinutes;
}

public class DbClient : MonoBehaviour
{
    const string BaseUrl = "https://localhost:7232/"; // use the port your API prints on startup

    IEnumerator Start()
    {
        using UnityWebRequest req = UnityWebRequest.Get(BaseUrl + "/api/progress");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            ProgressData p = JsonUtility.FromJson<ProgressData>(req.downloadHandler.text);
            Debug.Log($"Chapter {p.currentChapter}, checkpoint: {p.lastCheckpoint}");
        }
        else Debug.LogError(req.error);
    }

    public IEnumerator SaveProgress(ProgressData data)
    {
        byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data));
        using UnityWebRequest req = new UnityWebRequest(BaseUrl + "/api/progress", "PUT");
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();
    }

    public IEnumerator CollectItem(int id)
    {
        using UnityWebRequest req = UnityWebRequest.PostWwwForm(BaseUrl + $"/api/collectibles/{id}/collect", "");
        yield return req.SendWebRequest();
    }
}