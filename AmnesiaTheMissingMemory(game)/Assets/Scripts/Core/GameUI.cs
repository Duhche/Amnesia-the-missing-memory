using UnityEngine;

// Small helpers for drawing on-screen UI with OnGUI, so no Canvas setup is needed.
// PLACEHOLDER: swap for a real Canvas / UI Toolkit design for the final version.
public static class GameUI
{
    // UI was designed for 1080p, this scales it to the current screen height
    public static float Scale => Screen.height / 1080f;

    public static void Fill(Rect rect, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;
    }

    // simple trophy icon built from rectangles
    public static void Trophy(Rect r, Color color)
    {
        void Part(float x, float y, float w, float h) =>
            Fill(new Rect(r.x + x * r.width, r.y + y * r.height, w * r.width, h * r.height), color);

        Part(0.22f, 0.00f, 0.56f, 0.42f); // cup
        Part(0.04f, 0.06f, 0.18f, 0.07f); // left handle
        Part(0.04f, 0.06f, 0.07f, 0.28f);
        Part(0.04f, 0.27f, 0.18f, 0.07f);
        Part(0.78f, 0.06f, 0.18f, 0.07f); // right handle
        Part(0.89f, 0.06f, 0.07f, 0.28f);
        Part(0.78f, 0.27f, 0.18f, 0.07f);
        Part(0.44f, 0.42f, 0.12f, 0.26f); // stem
        Part(0.30f, 0.68f, 0.40f, 0.10f); // neck
        Part(0.20f, 0.80f, 0.60f, 0.20f); // base
    }

    public static GUIStyle Text(int size, TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
    {
        GUIStyle s = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(size * Scale),
            alignment = anchor,
            fontStyle = style,
            wordWrap = true
        };
        s.normal.textColor = Color.white;
        return s;
    }
}
