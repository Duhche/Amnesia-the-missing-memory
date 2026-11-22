using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// plays the music, the ambience and the sound effects.
// it makes itself when the game starts, nothing to set up.
// the sounds are in Assets/Resources/Audio and get loaded by name.
//
// where the sounds are from:
//   menu_music       "horror suspense background" by Universfield (Pixabay)
//   footsteps_grass  "walking on dried grass" by freesound_community (Pixabay)
//   weapon_pickup    "item equip" by freesound_community (Pixabay)
//   item_pickup      "item pickup 3" by yodguard (Pixabay)
//   inspect_body     "inspect item" by niamhd00145229 (freesound.org #422709, check the license)
//   night_ambience   "night cricket ambience" by freesound_community (Pixabay)
//   day_ambience     "nature ambience" by freesound_community (Pixabay)
//   kill             "head or body rips apart" by ragecore29 (Pixabay)
//   scene_change     "a thrilling scene change" by wings_of_freedom (Pixabay)
public class AudioManager : MonoBehaviour
{
    public enum Sfx { WeaponPickup, ItemPickup, Inspect, Kill, SceneChange }

    const string MenuScene = "MainMenu";
    const float FadeSeconds = 0.75f;

    public static AudioManager Instance { get; private set; }

    // ---- settings (saved on this PC) ----

    static float ambientVolume = 0.8f;
    static float effectsVolume = 1f;
    static bool menuMusicOn = true;

    // music in the menu + the night/day ambience
    public static float AmbientVolume
    {
        get => ambientVolume;
        set { ambientVolume = Mathf.Clamp01(value); PlayerPrefs.SetFloat("AmbientVolume", ambientVolume); }
    }

    // footsteps, pickups, kill, inspect
    public static float EffectsVolume
    {
        get => effectsVolume;
        set { effectsVolume = Mathf.Clamp01(value); PlayerPrefs.SetFloat("EffectsVolume", effectsVolume); }
    }

    // the music note button in the main menu
    public static bool MenuMusicOn
    {
        get => menuMusicOn;
        set
        {
            menuMusicOn = value;
            PlayerPrefs.SetInt("MenuMusicOn", value ? 1 : 0);
            if (Instance != null) Instance.PlayForScene(SceneManager.GetActiveScene().name);
        }
    }

    // ---- the sounds ----

    AudioClip menuMusic, nightAmbience, dayAmbience, footsteps;
    AudioClip weaponPickup, itemPickup, inspect, kill, sceneChange;

    AudioSource ambientSource;  // music / ambience, loops
    AudioSource footstepSource; // walking, loops while you move
    AudioSource effectSource;   // one-off sounds

    float fade = 1;   // 0-1, used when switching between music and ambience
    bool walking;
    Coroutine switching;

    // runs once when you hit Play, then stays alive in every scene
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        ambientVolume = PlayerPrefs.GetFloat("AmbientVolume", 0.8f);
        effectsVolume = PlayerPrefs.GetFloat("EffectsVolume", 1f);
        menuMusicOn = PlayerPrefs.GetInt("MenuMusicOn", 1) == 1;

        GameObject go = new GameObject("AudioManager");
        Instance = go.AddComponent<AudioManager>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        menuMusic = Load("menu_music");
        nightAmbience = Load("night_ambience");
        dayAmbience = Load("day_ambience");
        footsteps = Load("footsteps_grass");
        weaponPickup = Load("weapon_pickup");
        itemPickup = Load("item_pickup");
        inspect = Load("inspect_body");
        kill = Load("kill");
        sceneChange = Load("scene_change");

        ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.loop = true;
        footstepSource = gameObject.AddComponent<AudioSource>();
        footstepSource.loop = true;
        footstepSource.clip = footsteps;
        effectSource = gameObject.AddComponent<AudioSource>();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // the first scene was already loaded before this existed, so start its sound here
    void Start() => PlayForScene(SceneManager.GetActiveScene().name);

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        PlayerPrefs.Save();
    }

    void OnApplicationQuit() => PlayerPrefs.Save();

    static AudioClip Load(string name)
    {
        AudioClip clip = Resources.Load<AudioClip>("Audio/" + name);
        if (clip == null) Debug.LogWarning($"[AudioManager] Couldn't find Resources/Audio/{name}");
        return clip;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetWalking(false);
        PlayForScene(scene.name);
    }

    // menu = music (if the note is on), night levels = crickets, day levels = nature
    void PlayForScene(string scene)
    {
        AudioClip clip = null;
        if (scene == MenuScene) clip = menuMusicOn ? menuMusic : null;
        else if (scene.Contains("(night)")) clip = nightAmbience;
        else if (scene.Contains("(day)")) clip = dayAmbience;

        if (clip == ambientSource.clip && (clip == null || ambientSource.isPlaying)) return; // already playing it

        if (switching != null) StopCoroutine(switching);
        switching = StartCoroutine(SwitchTo(clip));
    }

    // fade out the old sound, swap, fade in the new one
    IEnumerator SwitchTo(AudioClip clip)
    {
        if (ambientSource.isPlaying)
            for (; fade > 0; fade -= Time.unscaledDeltaTime / FadeSeconds) yield return null;
        fade = 0;

        ambientSource.Stop();
        ambientSource.clip = clip;
        if (clip == null) yield break;
        ambientSource.Play();

        for (; fade < 1; fade += Time.unscaledDeltaTime / FadeSeconds) yield return null;
        fade = 1;
    }

    void Update()
    {
        // so the sliders change the sound right away
        ambientSource.volume = ambientVolume * fade;
        footstepSource.volume = effectsVolume * 0.7f;
    }

    // ---- what other scripts call ----

    public static void Play(Sfx sfx)
    {
        if (Instance == null) return;
        AudioClip clip = sfx switch
        {
            Sfx.WeaponPickup => Instance.weaponPickup,
            Sfx.ItemPickup => Instance.itemPickup,
            Sfx.Inspect => Instance.inspect,
            Sfx.Kill => Instance.kill,
            Sfx.SceneChange => Instance.sceneChange,
            _ => null
        };
        if (clip != null) Instance.effectSource.PlayOneShot(clip, effectsVolume);
    }

    // PlayerHandler calls this every frame with whether you're moving
    public static void SetWalking(bool isWalking)
    {
        if (Instance == null || Instance.walking == isWalking) return;
        Instance.walking = isWalking;

        AudioSource src = Instance.footstepSource;
        if (!isWalking) src.Pause(); // pause instead of stop so it carries on from the same spot
        else if (Instance.footsteps == null) return;
        else if (src.time > 0) src.UnPause();
        else src.Play();
    }
}
