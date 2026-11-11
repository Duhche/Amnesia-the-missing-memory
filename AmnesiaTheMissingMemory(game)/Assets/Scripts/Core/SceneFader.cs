using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// fades to black, loads the next scene, then fades back in.
// just call SceneFader.GoTo("Level1(day)") from any script.
// the scene has to be in the Build Profiles scene list.
public class SceneFader : MonoBehaviour
{
    const float FadeSeconds = 1f;

    static SceneFader instance;
    float alpha;
    bool busy;

    // true while it's fading or loading (the pause menu won't open then)
    public static bool IsBusy => instance != null && instance.busy;

    public static void GoTo(string sceneName, bool waitForAchievements = true)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[SceneFader] Scene '{sceneName}' isn't in File > Build Profiles > Scene List (or is misspelled).");
            return;
        }

        // makes itself the first time it's needed
        if (instance == null)
        {
            instance = new GameObject("SceneFader").AddComponent<SceneFader>();
            DontDestroyOnLoad(instance.gameObject); // stays alive in the next scene so it can fade back in
        }
        if (!instance.busy) instance.StartCoroutine(instance.Run(sceneName, waitForAchievements));
    }

    IEnumerator Run(string sceneName, bool waitForAchievements)
    {
        busy = true;

        // wait for the achievement pop-ups to finish first
        while (waitForAchievements && AchievementManager.Instance != null && AchievementManager.Instance.IsBusy)
            yield return null;

        // fade out
        GameState.InputLocked = true;
        for (alpha = 0; alpha < 1; alpha += Time.unscaledDeltaTime / FadeSeconds) yield return null;
        alpha = 1;

        yield return SceneManager.LoadSceneAsync(sceneName);
        GameState.InputLocked = false;

        // fade in
        for (; alpha > 0; alpha -= Time.unscaledDeltaTime / FadeSeconds) yield return null;
        alpha = 0;
        busy = false;
    }

    void OnGUI()
    {
        if (alpha <= 0) return;
        GUI.depth = -1000; // draw over everything else
        GameUI.Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0, 0, alpha));
    }
}
