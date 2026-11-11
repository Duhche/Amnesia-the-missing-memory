using UnityEngine;

// helpers for drawing the on-screen stuff (boxes, text, the trophy).
// placeholder: will be swapped for proper UI later.
public static class GameUI
{
    // everything was made for 1080p, this makes it fit other screen sizes
    public static float Scale => Screen.height / 1080f;

    // draws a plain colored box
    public static void Fill(Rect rect, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;
    }

    // trophy icon made out of rectangles
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

    // dark button that goes red when you hover it, can have a trophy on the left.
    // returns true when it gets clicked
    public static bool Button(Rect r, string label, int fontSize = 30, bool trophy = false)
    {
        float s = Scale;
        bool hover = r.Contains(Event.current.mousePosition);

        Fill(r, hover ? new Color(0.25f, 0.03f, 0.03f) : new Color(0.12f, 0.12f, 0.13f));
        Fill(new Rect(r.x, r.y, 6 * s, r.height), hover ? new Color(0.75f, 0.05f, 0.05f) : new Color(0.35f, 0.35f, 0.35f));
        if (trophy)
        {
            float icon = r.height * 0.55f;
            Trophy(new Rect(r.x + 26 * s, r.y + (r.height - icon) / 2, icon, icon), new Color(0.95f, 0.75f, 0.2f));
        }
        GUI.Label(r, label, Text(fontSize, TextAnchor.MiddleCenter, FontStyle.Bold));

        return GUI.Button(r, GUIContent.none, GUIStyle.none);
    }

    // white text style in the size you want
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
