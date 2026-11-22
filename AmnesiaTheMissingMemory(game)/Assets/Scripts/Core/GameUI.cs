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

    // music note icon (two notes joined at the top) made out of rectangles
    public static void MusicNote(Rect r, Color color)
    {
        void Part(float x, float y, float w, float h) =>
            Fill(new Rect(r.x + x * r.width, r.y + y * r.height, w * r.width, h * r.height), color);

        Part(0.36f, 0.04f, 0.54f, 0.14f); // the bar on top
        Part(0.36f, 0.04f, 0.09f, 0.74f); // left stem
        Part(0.82f, 0.04f, 0.09f, 0.66f); // right stem
        Part(0.10f, 0.66f, 0.35f, 0.26f); // left note head
        Part(0.56f, 0.58f, 0.35f, 0.26f); // right note head
    }

    // a line across an icon, for "off"
    public static void Crossed(Rect r, Color color)
    {
        Matrix4x4 old = GUI.matrix;
        GUIUtility.RotateAroundPivot(45, r.center);
        Fill(new Rect(r.center.x - r.width * 0.65f, r.center.y - r.height * 0.05f, r.width * 1.3f, r.height * 0.1f), color);
        GUI.matrix = old;
    }

    // a volume bar you can click or drag, gives back the new value (0-1)
    public static float Slider(Rect r, float value)
    {
        int id = GUIUtility.GetControlID(FocusType.Passive);
        Event e = Event.current;

        switch (e.GetTypeForControl(id))
        {
            case EventType.MouseDown:
                if (r.Contains(e.mousePosition)) { GUIUtility.hotControl = id; value = ValueAt(r, e); e.Use(); }
                break;
            case EventType.MouseDrag:
                if (GUIUtility.hotControl == id) { value = ValueAt(r, e); e.Use(); }
                break;
            case EventType.MouseUp:
                if (GUIUtility.hotControl == id) { GUIUtility.hotControl = 0; e.Use(); }
                break;
        }

        // grey track, red filled part, white handle
        float s = Scale;
        Rect track = new Rect(r.x, r.center.y - 4 * s, r.width, 8 * s);
        Fill(track, new Color(0.25f, 0.25f, 0.25f));
        Fill(new Rect(track.x, track.y, track.width * value, track.height), new Color(0.75f, 0.05f, 0.05f));
        Fill(new Rect(track.x + track.width * value - 7 * s, r.center.y - 14 * s, 14 * s, 28 * s), Color.white);
        return value;
    }

    static float ValueAt(Rect r, Event e) => Mathf.Clamp01((e.mousePosition.x - r.x) / r.width);

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
