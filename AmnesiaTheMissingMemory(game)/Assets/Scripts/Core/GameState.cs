using UnityEngine;

// Shared flags that any script can read.
public static class GameState
{
    // true while a minigame (or later a cutscene/menu) is open: blocks walking, looking and interacting
    public static bool InputLocked;

    // resets the flag every time you press Play, even with domain reload turned off
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay() => InputLocked = false;
}
