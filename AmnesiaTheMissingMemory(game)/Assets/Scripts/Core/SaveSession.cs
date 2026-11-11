using UnityEngine;
using UnityEngine.SceneManagement;

// which save slot you're playing right now (0 = none, like when the API is off).
// every time a level loads it saves that level into the slot.
public static class SaveSession
{
    public static int Slot;

    // runs every time you hit Play
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        Slot = 0;
        SceneManager.sceneLoaded -= OnSceneLoaded; // so it doesn't get added twice
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // the main menu sets Slot back to 0 before this runs, so the menu never gets saved as a level
    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Slot > 0 && DbClient.Instance != null) DbClient.Instance.UpdateSaveLevel(Slot, scene.name);
    }
}
