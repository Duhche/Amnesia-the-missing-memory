using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// What the player is carrying. Filled by PickupItem (by item name).
// Emptied every time a new scene loads, so items have to be picked up again in each level.
// PLACEHOLDER: no inventory screen yet.
public static class PlayerInventory
{
    static readonly HashSet<string> items = new HashSet<string>();

    public static void Add(string itemName) => items.Add(itemName);
    public static bool Has(string itemName) => items.Contains(itemName);
    public static void Clear() => items.Clear();

    // runs every time you press Play, even with domain reload turned off
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        items.Clear();
        SceneManager.sceneLoaded -= OnSceneLoaded; // don't subscribe twice
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => items.Clear();
}
