using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Fades to black, loads another scene, then fades back in.
// Nothing to set up: call SceneFader.GoTo("Level1(day)") from any script.
// The scene must be in File > Build Profiles > Scene List.
public class SceneFader : MonoBehaviour
{
    const float FadeSeconds = 1f;

    static SceneFader instance;
    float alpha;
    bool busy;

    public static void GoTo(string sceneName, bool waitForAchievements = true)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[SceneFader] Scene '{sceneName}' isn't in File > Build Profiles > Scene List (or is misspelled).");
            return;
        }

        if (instance == null)
        {
            instance = new GameObject("SceneFader").AddComponent<SceneFader>();
            DontDestroyOnLoad(instance.gameObject); // survives the scene change so it can fade back in
        }
        if (!instance.busy) instance.StartCoroutine(instance.Run(sceneName, waitForAchievements));
    }

    IEnumerator Run(string sceneName, bool waitForAchievements)
    {
        busy = true;

        // let "Achievement Unlocked" pop-ups finish before leaving the scene
        while (waitForAchievements && AchievementManager.Instance != null && AchievementManager.Instance.IsBusy)
            yield return null;

        GameState.InputLocked = true;
        for (alpha = 0; alpha < 1; alpha += Time.unscaledDeltaTime / FadeSeconds) yield return null;
        alpha = 1;

        yield return SceneManager.LoadSceneAsync(sceneName);
        GameState.InputLocked = false;

        for (; alpha > 0; alpha -= Time.unscaledDeltaTime / FadeSeconds) yield return null;
        alpha = 0;
        busy = false;
    }

    void OnGUI()
    {
        if (alpha <= 0) return;
        GUI.depth = -1000; // draw on top of every other pop-up
        GameUI.Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0, 0, alpha));
    }
}
