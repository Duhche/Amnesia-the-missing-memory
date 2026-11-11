using UnityEngine;

// stuff every script can check.
public static class GameState
{
    // when this is true the player can't walk, look around or use things (like during the kill minigame)
    public static bool InputLocked;

    // turns it back off every time you hit Play
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay() => InputLocked = false;
}
