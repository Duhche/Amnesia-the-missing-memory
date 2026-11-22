using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// builds level 1, night and day get the same layout:
//  - a pine forest made from the 2 pine models already in the scene (with trunk colliders)
//  - a clear spot where the player spawns
//  - a dirt trail from the spawn to a small camp (2 tents + a campfire)
//  - moves the victim (night) / body (day) to the fire, sticks the knife in a tree by the camp
//    and puts the collectible next to a tent
// run it from Tools > Build Level 1 Forest And Camp. running it again rebuilds it all.
// everything it makes goes inside the "Level1 Generated" object.
public static class Level1Builder
{
    static readonly string[] Scenes = { "Assets/Scenes/Level1(night).unity", "Assets/Scenes/Level1(day).unity" };

    const string GeneratedName = "Level1 Generated";
    const int Seed = 1234;              // same number = same layout every time

    const float TreesPer100m2 = 2f;     // how thick the forest is
    const float TreeSpacing = 2.5f;     // min distance between trees
    const float EdgeMargin = 1f;        // stay this far from the edge of the ground
    const float TrunkRadius = 0.3f;     // size of the invisible collider around each trunk

    static readonly Vector2 CampPosition = new Vector2(1.9f, 9.12f); // where the campfire is (x, z)
    const float SpawnDistance = 40f;    // how far back from the camp the player starts
    const float SpawnClearRadius = 4f;  // no trees this close to the spawn
    const float CampClearRadius = 8f;   // no trees this close to the campfire
    const float TrailWidth = 1.6f;
    const float TrailClearWidth = 3.5f; // no trees this close to the middle of the trail

    static System.Random random;
    static float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);

    [MenuItem("Tools/Build Level 1 Forest And Camp")]
    public static void BuildLevel1()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string wasOpen = EditorSceneManager.GetActiveScene().path;

        foreach (string path in Scenes)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            bool night = path.Contains("(night)");
            Build(night);
            EditorSceneManager.SaveScene(scene);
        }

        if (!string.IsNullOrEmpty(wasOpen)) EditorSceneManager.OpenScene(wasOpen, OpenSceneMode.Single);
    }

    static void Build(bool night)
    {
        random = new System.Random(Seed); // reset so both scenes come out the same

        GameObject ground = GameObject.Find("ground");
        GameObject player = GameObject.Find("Player");
        if (ground == null || player == null) { Debug.LogError("[Level1Builder] Need objects called 'ground' and 'Player'"); return; }
        Bounds area = ground.GetComponent<Collider>().bounds;
        float top = area.max.y;

        // throw away what was made last time (and the old tree scatter)
        foreach (string old in new[] { GeneratedName, "Trees" })
        {
            GameObject o = GameObject.Find(old);
            if (o != null) Object.DestroyImmediate(o);
        }
        Transform root = new GameObject(GeneratedName).transform;
        Transform treeGroup = Group("Trees", root);
        Transform colliderGroup = Group("Tree Colliders", root);

        // the 2 pine models already in the scene are what the forest gets made from
        var pineModels = new List<GameObject>();
        var originals = new List<Transform>();
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t.parent != null || !t.name.StartsWith("Tree_")) continue;
            GameObject model = PrefabUtility.GetCorrespondingObjectFromOriginalSource(t.gameObject);
            if (model != null && !pineModels.Contains(model)) pineModels.Add(model);
            originals.Add(t);
        }
        pineModels.Sort((a, b) => string.CompareOrdinal(a.name, b.name)); // same order every time
        if (pineModels.Count == 0) { Debug.LogError("[Level1Builder] No Tree_ models in the scene to copy"); return; }

        // where things go: the camp has a fixed spot, the player starts SpawnDistance behind it,
        // facing the camp (the way the player already faces)
        Vector3 camp = new Vector3(CampPosition.x, top, CampPosition.y);
        Vector3 fwd = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
        if (fwd == Vector3.zero) fwd = Vector3.forward;

        Vector3 spawn = camp - fwd * SpawnDistance;
        spawn.x = Mathf.Clamp(spawn.x, area.min.x + SpawnClearRadius, area.max.x - SpawnClearRadius);
        spawn.z = Mathf.Clamp(spawn.z, area.min.z + SpawnClearRadius, area.max.z - SpawnClearRadius);
        player.transform.position = new Vector3(spawn.x, player.transform.position.y, spawn.z);

        fwd = (camp - spawn).normalized; // in case the spawn got pushed in from the edge
        Vector3 right = Vector3.Cross(Vector3.up, fwd);

        // your own pines stay, but get nudged off the trail / out of the clearings if they're in the way
        var trees = new List<Vector3>();
        foreach (Transform t in originals)
        {
            Vector3 p = t.position;
            p = PushOutOfCircle(p, spawn, SpawnClearRadius);
            p = PushOutOfCircle(p, camp, CampClearRadius);
            float side = Vector3.Dot(p - spawn, right) >= 0 ? 1 : -1;
            while (DistanceToSegment(p, spawn, camp) < TrailClearWidth) p += right * side * 0.25f;
            while (TooClose(p, trees, TreeSpacing)) p -= fwd * 0.25f; // don't end up inside another pine
            t.position = p;

            trees.Add(p);
            AddTrunkCollider(t.gameObject, colliderGroup);
        }

        BuildTrail(root, spawn, camp, fwd, right, top);
        Vector3[] tents = BuildCamp(root, camp, fwd, right, top, night);

        // a tree right by the camp with the knife stuck in it
        Vector3 knifeTree = camp + right * 6f - fwd * 1.5f;
        PlaceTree(pineModels[0], knifeTree, 1.8f, 0, treeGroup, colliderGroup);
        trees.Add(knifeTree);

        // move the story objects to the camp (keeping their height)
        MoveTo("Victim", camp - right * 1.6f - fwd * 0.4f, top);
        MoveTo("Body", camp - right * 1.6f - fwd * 0.4f, top);
        StickKnife("Weapon", knifeTree, camp, top);
        MoveTo("Collectible", tents[1] - right * 1.5f, top);

        // fill the rest with pines, but keep the spawn, the trail and the camp clear
        int treeCount = Mathf.RoundToInt(area.size.x * area.size.z / 100f * TreesPer100m2);
        int placed = 0;
        for (int attempt = 0; attempt < treeCount * 20 && placed < treeCount; attempt++)
        {
            Vector3 pos = new Vector3(Range(area.min.x + EdgeMargin, area.max.x - EdgeMargin), top,
                                      Range(area.min.z + EdgeMargin, area.max.z - EdgeMargin));

            if (FlatDistance(pos, spawn) < SpawnClearRadius) continue;
            if (FlatDistance(pos, camp) < CampClearRadius) continue;
            if (DistanceToSegment(pos, spawn, camp) < TrailClearWidth) continue;
            if (TooClose(pos, trees, TreeSpacing)) continue;

            GameObject model = pineModels[random.Next(pineModels.Count)];
            PlaceTree(model, pos, Range(1.3f, 2.1f), Range(0, 360), treeGroup, colliderGroup);
            trees.Add(pos);
            placed++;
        }

        Debug.Log($"[Level1Builder] {(night ? "night" : "day")}: {placed} pines, camp at {camp}, spawn at {spawn}");
    }

    // ---- trees ----

    static void PlaceTree(GameObject model, Vector3 pos, float size, float turn, Transform parent, Transform colliderGroup)
    {
        GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
        tree.transform.position = pos;
        tree.transform.rotation = Quaternion.Euler(0, turn, 0) * model.transform.rotation;
        tree.transform.localScale = Vector3.Scale(model.transform.localScale, new Vector3(size, size * Range(1.2f, 1.5f), size));
        AddTrunkCollider(tree, colliderGroup);
    }

    // the pine models have no collider, so each one gets an invisible pole you can't walk through
    static void AddTrunkCollider(GameObject tree, Transform colliderGroup)
    {
        Bounds b = WorldBounds(tree);
        GameObject pole = new GameObject("Trunk " + tree.name);
        pole.transform.SetParent(colliderGroup, false);
        pole.transform.position = new Vector3(tree.transform.position.x, b.center.y, tree.transform.position.z);
        CapsuleCollider c = pole.AddComponent<CapsuleCollider>();
        c.radius = TrunkRadius;
        c.height = Mathf.Max(b.size.y, TrunkRadius * 2);
    }

    // ---- trail ----

    static void BuildTrail(Transform root, Vector3 spawn, Vector3 camp, Vector3 fwd, Vector3 right, float top)
    {
        Transform trail = Group("Trail", root);
        Material dirt = GetMaterial("trail_dirt", new Color(0.42f, 0.33f, 0.22f));

        // one long flat strip of dirt from the spawn to the edge of the camp
        Vector3 start = spawn + fwd * 1f;
        Vector3 end = camp - fwd * 2.5f;
        GameObject path = Cube("Dirt Path", trail, dirt, collider: false);
        path.transform.position = (start + end) / 2f + Vector3.up * 0.01f;
        path.transform.rotation = Quaternion.LookRotation(fwd);
        path.transform.localScale = new Vector3(TrailWidth, 0.02f, Vector3.Distance(start, end));

    }

    // ---- camp ----

    // returns where the 2 tents are
    static Vector3[] BuildCamp(Transform root, Vector3 camp, Vector3 fwd, Vector3 right, float top, bool night)
    {
        Transform group = Group("Camp", root);

        // campfire: logs, and at night flames + a flickering orange light
        Transform fire = Group("Campfire", group);
        fire.position = new Vector3(camp.x, top, camp.z);

        Material wood = GetMaterial(night ? "campfire_wood" : "campfire_ash", night ? new Color(0.35f, 0.2f, 0.1f) : new Color(0.15f, 0.13f, 0.12f));
        for (int i = 0; i < 3; i++)
        {
            GameObject log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            log.name = "Log";
            Object.DestroyImmediate(log.GetComponent<Collider>());
            log.GetComponent<Renderer>().sharedMaterial = wood;
            log.transform.SetParent(fire, false);
            log.transform.position = new Vector3(camp.x, top + 0.08f, camp.z);
            log.transform.rotation = Quaternion.Euler(0, i * 60f, 90f); // lying down, crossed
            log.transform.localScale = new Vector3(0.12f, 0.45f, 0.12f);
        }

        if (night)
        {
            Material flame = GetMaterial("campfire_flame", new Color(1f, 0.55f, 0.1f), unlit: true);
            for (int i = 0; i < 3; i++)
            {
                GameObject f = Cube("Flame", fire, flame, collider: false);
                f.transform.position = new Vector3(camp.x, top + 0.35f, camp.z);
                f.transform.rotation = Quaternion.Euler(0, i * 30f, 45f);
                f.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f) * (1f - i * 0.2f);
            }

            GameObject lightObj = new GameObject("Fire Light");
            lightObj.transform.SetParent(fire, false);
            lightObj.transform.position = new Vector3(camp.x, top + 0.8f, camp.z);
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.55f, 0.2f);
            l.range = 12f;
            l.intensity = 3f;
            l.shadows = LightShadows.Soft;
            lightObj.AddComponent<CampfireLight>();
        }

        // 2 tents behind the fire, opening towards it
        Material green = GetMaterial("tent_green", new Color(0.2f, 0.35f, 0.2f));
        Material orange = GetMaterial("tent_orange", new Color(0.75f, 0.35f, 0.1f));
        Vector3 tentA = camp + right * 3f + fwd * 2.5f;
        Vector3 tentB = camp - right * 3f + fwd * 2.5f;
        Tent("Tent A", group, tentA, camp, top, green);
        Tent("Tent B", group, tentB, camp, top, orange);
        return new[] { tentA, tentB };
    }

    // an A-frame tent: a cube turned 45 degrees and half sunk into the ground
    static void Tent(string name, Transform parent, Vector3 pos, Vector3 facing, float top, Material mat)
    {
        Transform tent = Group(name, parent);
        tent.position = new Vector3(pos.x, top, pos.z);
        Vector3 dir = facing - pos; dir.y = 0;
        tent.rotation = Quaternion.LookRotation(dir.normalized);

        GameObject body = Cube("Canvas", tent, mat, collider: true);
        body.transform.localPosition = Vector3.zero;
        body.transform.localRotation = Quaternion.Euler(0, 0, 45);
        body.transform.localScale = new Vector3(1.6f, 1.6f, 2.4f);
    }

    // ---- moving the story objects ----

    static void MoveTo(string name, Vector3 pos, float top)
    {
        GameObject go = GameObject.Find(name);
        if (go == null) return;
        go.transform.position = new Vector3(pos.x, go.transform.position.y, pos.z);
        Ground(go, top); // make sure it sits on the floor
    }

    // knife pointing into the trunk at chest height, on the side facing the camp
    static void StickKnife(string name, Vector3 tree, Vector3 camp, float top)
    {
        GameObject knife = GameObject.Find(name);
        if (knife == null) return;
        Vector3 toCamp = camp - tree; toCamp.y = 0; toCamp.Normalize();

        knife.transform.position = new Vector3(tree.x, top + 1.3f, tree.z) + toCamp * (TrunkRadius + 0.15f);
        knife.transform.rotation = Quaternion.LookRotation(-toCamp) * Quaternion.Euler(12f, 0, 0); // a bit tilted down
    }

    // puts an object's lowest point on the ground
    static void Ground(GameObject go, float top)
    {
        Renderer r = go.GetComponentInChildren<Renderer>();
        if (r == null) return;
        go.transform.position += Vector3.up * (top - r.bounds.min.y);
    }

    // ---- little helpers ----

    static Transform Group(string name, Transform parent)
    {
        Transform t = new GameObject(name).transform;
        t.SetParent(parent, false);
        return t;
    }

    static GameObject Cube(string name, Transform parent, Material mat, bool collider)
    {
        GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.name = name;
        c.transform.SetParent(parent, false);
        c.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider) Object.DestroyImmediate(c.GetComponent<Collider>());
        return c;
    }

    // makes the material the first time, reuses it after that
    static Material GetMaterial(string name, Color color, bool unlit = false)
    {
        string path = $"Assets/materials/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        Shader shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
        mat = new Material(shader);
        mat.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static Bounds WorldBounds(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    static float FlatDistance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

    // if p is inside the circle, move it straight out to the edge
    static Vector3 PushOutOfCircle(Vector3 p, Vector3 center, float radius)
    {
        Vector3 away = p - center; away.y = 0;
        if (away.magnitude >= radius) return p;
        if (away == Vector3.zero) away = Vector3.right;
        Vector3 edge = center + away.normalized * radius;
        return new Vector3(edge.x, p.y, edge.z);
    }

    static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector2 P = new Vector2(p.x, p.z), A = new Vector2(a.x, a.z), B = new Vector2(b.x, b.z);
        Vector2 ab = B - A;
        float t = Mathf.Clamp01(Vector2.Dot(P - A, ab) / ab.sqrMagnitude);
        return Vector2.Distance(P, A + ab * t);
    }

    static bool TooClose(Vector3 pos, List<Vector3> others, float distance)
    {
        foreach (Vector3 o in others)
            if (FlatDistance(pos, o) < distance) return true;
        return false;
    }
}
