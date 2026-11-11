using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// keeps track of what the player picked up (by item name).
// gets emptied every new level, so you have to pick things up again.
// placeholder: no inventory screen yet.
public static class PlayerInventory
{
    static readonly HashSet<string> items = new HashSet<string>();

    public static void Add(string itemName) => items.Add(itemName);
    public static bool Has(string itemName) => items.Contains(itemName);
    public static void Clear() => items.Clear();

    // runs every time you hit Play
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        items.Clear();
        SceneManager.sceneLoaded -= OnSceneLoaded; // so it doesn't get added twice
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // new scene = empty pockets
    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => items.Clear();
}
