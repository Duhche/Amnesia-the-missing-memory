using UnityEngine;

// the volume sliders, used in the main menu settings and in the pause menu
public static class SettingsUI
{
    // how tall the sliders are together, so the menus know where to put things under them
    public static float Height => 130 * GameUI.Scale;

    public static void DrawVolume(float x, float y, float width)
    {
        float s = GameUI.Scale;
        AudioManager.AmbientVolume = Row(new Rect(x, y, width, 50 * s), "Ambient", AudioManager.AmbientVolume);
        AudioManager.EffectsVolume = Row(new Rect(x, y + 75 * s, width, 50 * s), "Effects", AudioManager.EffectsVolume);
    }

    // "Ambient  [=======-----]  70%"
    static float Row(Rect r, string label, float value)
    {
        float s = GameUI.Scale;
        float labelW = 150 * s, percentW = 80 * s;

        GUI.Label(new Rect(r.x, r.y, labelW, r.height), label, GameUI.Text(26, TextAnchor.MiddleLeft, FontStyle.Bold));
        value = GameUI.Slider(new Rect(r.x + labelW, r.y, r.width - labelW - percentW, r.height), value);
        GUI.Label(new Rect(r.xMax - percentW, r.y, percentW, r.height), $"{Mathf.RoundToInt(value * 100)}%",
                  GameUI.Text(22, TextAnchor.MiddleRight));
        return value;
    }
}
