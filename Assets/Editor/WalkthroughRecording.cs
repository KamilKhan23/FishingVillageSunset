using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders the walkthrough video offline (frame by frame) to Deliverables/Walkthrough_FishingVillage.mp4.
/// The tour shows each viewpoint, walks down through the village onto the pier and rises over the lighthouse.
/// Ambient scripts (boats, gulls, lanterns, lighthouse, water) are evaluated per frame with the same maths
/// as in Play mode, so the motion in the video matches the running scene.
/// Usage from code: Begin(), then Step(n) until CurrentFrame == TotalFrames, then Finish().
/// </summary>
public static class WalkthroughRecording
{
    const int Fps = 30;
    const float Duration = 72f;
    const int Width = 1920, Height = 1080;
    const float DeckY = 11.44f;

    static MediaEncoder encoder;
    static RenderTexture rt;
    static Texture2D frameTex;
    static Camera cam;
    static int frame;
    static GameObject captionCanvas;
    static Text captionText;

    static Vector3 camStartPos; static Quaternion camStartRot;
    static Transform[] vps;
    static Terrain terrain;
    static List<Vector3> path;
    static float pathLength;
    static Vector3 lighthouse;

    class Bob { public Transform t; public Vector3 pos; public Quaternion rot; public float h, hc, r, rc, p, pc, ph; }
    class Spin { public Transform t; public Quaternion rot; public Vector3 dps; }
    class Sway { public Transform t; public Quaternion rot; public Vector3 axis; public float angle, cps, ph; }
    class Flicker { public Light l; public float baseI, amount, speed, seed; }
    class Gull { public Transform t; public Vector3 pos; public Quaternion rot; public float radius, dps, start, hw, hc, bank; public bool cw; }
    static List<Bob> bobs; static List<Spin> spins; static List<Sway> sways; static List<Flicker> flickers; static List<Gull> gulls;
    static ParticleSystem[] particles;
    static Material seaMaterial; static Vector2 seaOffset0, seaSpeed;

    static readonly Vector2[] WalkPoints =
    {
        new Vector2(153f, 58f), new Vector2(150f, 64f), new Vector2(165f, 78f), new Vector2(152f, 96f),
        new Vector2(160f, 112f), new Vector2(158f, 126f), new Vector2(158f, 136f), new Vector2(158f, 152f), new Vector2(158.5f, 176f)
    };

    public static int TotalFrames { get { return Mathf.RoundToInt(Duration * Fps); } }
    public static int CurrentFrame { get { return frame; } }

    public static string Begin()
    {
        terrain = Terrain.activeTerrain;
        cam = SceneCapture.FindMainCamera();
        camStartPos = cam.transform.position; camStartRot = cam.transform.rotation;
        var so = new SerializedObject(cam.GetComponent<ExplorerCamera>());
        var arr = so.FindProperty("viewpoints");
        vps = new Transform[3];
        for (int i = 0; i < 3; i++) vps[i] = (Transform)arr.GetArrayElementAtIndex(i).objectReferenceValue;
        lighthouse = Object.FindObjectOfType<Rotator>().transform.position;
        BuildPath();
        CollectAnimated();
        MakeCaption();

        rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
        frameTex = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
        string file = System.IO.Path.Combine(SceneCapture.DeliverablesFolder, "Walkthrough_FishingVillage.mp4");
        System.IO.Directory.CreateDirectory(SceneCapture.DeliverablesFolder);
        if (System.IO.File.Exists(file)) System.IO.File.Delete(file);
        var attr = new VideoTrackAttributes
        {
            frameRate = new MediaRational(Fps),
            width = Width, height = Height,
            includeAlpha = false,
            bitRateMode = VideoBitrateMode.High
        };
        encoder = new MediaEncoder(file, attr);
        frame = 0;
        foreach (var ps in particles) ps.Simulate(0f, true, true);
        return file;
    }

    public static string Step(int count)
    {
        if (encoder == null) return "not started";
        int end = Mathf.Min(frame + count, TotalFrames);
        float dt = 1f / Fps;
        for (; frame < end; frame++)
        {
            float t = frame * dt;
            ApplyAnimated(t, false);
            foreach (var ps in particles) ps.Simulate(dt, true, false, false);

            Vector3 pos; Quaternion rot; string caption;
            Pose(t, out pos, out rot, out caption);
            cam.transform.SetPositionAndRotation(pos, rot);
            captionText.text = caption;

            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            frameTex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            frameTex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            encoder.AddFrame(frameTex);
        }
        return frame + "/" + TotalFrames;
    }

    public static string Finish()
    {
        if (encoder != null) { encoder.Dispose(); encoder = null; }
        ApplyAnimated(0f, true);
        foreach (var ps in particles) ps.Simulate(0f, true, true);
        cam.transform.SetPositionAndRotation(camStartPos, camStartRot);
        if (captionCanvas != null) Object.DestroyImmediate(captionCanvas);
        if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
        if (frameTex != null) Object.DestroyImmediate(frameTex);
        return "done";
    }

    // ------------------------------------------------------------------ tour
    static Quaternion Pan(Transform vp, float deg, float k) { return vp.rotation * Quaternion.Euler(0f, deg * Mathf.SmoothStep(0f, 1f, k), 0f); }

    static void Pose(float t, out Vector3 pos, out Quaternion rot, out string caption)
    {
        if (t < 6f)
        {
            caption = "Key 1 - Viewpoint: Hillside Arrival (the lighthouse is the landmark)";
            pos = vps[0].position; rot = Pan(vps[0], 8f, t / 6f);
            return;
        }
        if (t < 30f)
        {
            float k = (t - 6f) / 24f;
            caption = k < 0.55f ? "W / A / S / D - walking down through the village" : "W - out along the pier";
            float d = Mathf.SmoothStep(0f, 1f, k) * pathLength;
            pos = PointAt(d);
            Vector3 ahead = PointAt(d + 9f) + Vector3.down * 0.6f;
            Quaternion look = Quaternion.LookRotation(ahead - pos);
            rot = Quaternion.Slerp(Pan(vps[0], 8f, 1f), look, Mathf.SmoothStep(0f, 1f, (t - 6f) / 2.5f));
            return;
        }
        if (t < 38f)
        {
            caption = "Key 2 - Viewpoint: Pier End, looking back at the village";
            pos = vps[1].position; rot = Pan(vps[1], -10f, (t - 30f) / 8f);
            return;
        }
        if (t < 45f)
        {
            caption = "Key 3 - Viewpoint: Lighthouse Headland, facing the setting sun";
            pos = vps[2].position; rot = Pan(vps[2], 12f, (t - 38f) / 7f);
            return;
        }
        if (t < 60f)
        {
            caption = "E - rising up around the lighthouse";
            float k = (t - 45f) / 15f;
            float ease = Mathf.SmoothStep(0f, 1f, k);
            Vector3 s = vps[2].position;
            float a0 = Mathf.Atan2(s.z - lighthouse.z, s.x - lighthouse.x);
            float r0 = new Vector2(s.x - lighthouse.x, s.z - lighthouse.z).magnitude;
            float a = a0 - ease * Mathf.PI * 0.9f;
            float r = Mathf.Lerp(r0, 34f, ease);
            Vector3 p = new Vector3(lighthouse.x + Mathf.Cos(a) * r, 0f, lighthouse.z + Mathf.Sin(a) * r);
            float g = Mathf.Max(terrain.SampleHeight(p), 10f);
            p.y = Mathf.Lerp(s.y, g + 22f, ease);
            pos = p;
            Quaternion look = Quaternion.LookRotation(lighthouse + Vector3.down * 6f - p);
            rot = Quaternion.Slerp(Pan(vps[2], 12f, 1f), look, Mathf.SmoothStep(0f, 1f, (t - 45f) / 2.5f));
            return;
        }
        if (t < 68f)
        {
            caption = "Q - gliding down to the harbour at sunset";
            float k = Mathf.SmoothStep(0f, 1f, (t - 60f) / 8f);
            Vector3 from = new Vector3(200f, 40f, 170f), to = SceneCapture.BestShotPosition;
            pos = Vector3.Lerp(from, to, k);
            Quaternion a = Quaternion.LookRotation(new Vector3(150f, 14f, 170f) - from);
            Quaternion b = Quaternion.LookRotation(SceneCapture.BestShotLookAt - to);
            rot = Quaternion.Slerp(a, b, k);
            return;
        }
        caption = "Key 1 - back to the Hillside Arrival";
        pos = vps[0].position; rot = vps[0].rotation;
    }

    static float WalkHeight(Vector3 w)
    {
        bool onPier = w.z > 133f && Mathf.Abs(w.x - 158f) < 1.7f;
        float ground = Mathf.Max(terrain.SampleHeight(w), 10f);
        if (onPier) ground = Mathf.Max(ground, DeckY);
        return ground + 1.75f;
    }

    static void BuildPath()
    {
        path = new List<Vector3>();
        var pts = WalkPoints;
        for (int i = 0; i < pts.Length - 1; i++)
        {
            Vector2 p0 = pts[Mathf.Max(i - 1, 0)], p1 = pts[i], p2 = pts[i + 1], p3 = pts[Mathf.Min(i + 2, pts.Length - 1)];
            for (int s = 0; s < 30; s++)
            {
                float t = s / 30f, t2 = t * t, t3 = t2 * t;
                Vector2 p = 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
                Vector3 w = new Vector3(p.x, 0f, p.y);
                w.y = WalkHeight(w);
                path.Add(w);
            }
        }
        // start exactly at viewpoint 1's height
        path[0] = new Vector3(path[0].x, vps[0].position.y, path[0].z);
        for (int i = 1; i < path.Count - 1; i++) path[i] = new Vector3(path[i].x, (path[i - 1].y + path[i].y * 2f + path[i + 1].y) * 0.25f, path[i].z);
        pathLength = 0f;
        for (int i = 1; i < path.Count; i++) pathLength += Vector3.Distance(path[i], path[i - 1]);
    }

    static Vector3 PointAt(float distance)
    {
        for (int i = 1; i < path.Count; i++)
        {
            float seg = Vector3.Distance(path[i - 1], path[i]);
            if (distance <= seg) return Vector3.Lerp(path[i - 1], path[i], distance / seg);
            distance -= seg;
        }
        return path[path.Count - 1];
    }

    // ------------------------------------------------------------------ ambient motion
    static SerializedObject S(Object o) { return new SerializedObject(o); }
    static float F(SerializedObject so, string p) { return so.FindProperty(p).floatValue; }

    static void CollectAnimated()
    {
        bobs = new List<Bob>(); spins = new List<Spin>(); sways = new List<Sway>(); flickers = new List<Flicker>(); gulls = new List<Gull>();
        foreach (var b in Object.FindObjectsOfType<BoatBob>())
        {
            var so = S(b);
            bobs.Add(new Bob { t = b.transform, pos = b.transform.localPosition, rot = b.transform.localRotation,
                h = F(so, "bobHeight"), hc = F(so, "bobCyclesPerSecond"), r = F(so, "rollDegrees"), rc = F(so, "rollCyclesPerSecond"),
                p = F(so, "pitchDegrees"), pc = F(so, "pitchCyclesPerSecond"), ph = F(so, "phaseOffset") });
        }
        foreach (var r in Object.FindObjectsOfType<Rotator>())
            spins.Add(new Spin { t = r.transform, rot = r.transform.localRotation, dps = S(r).FindProperty("degreesPerSecond").vector3Value });
        foreach (var s in Object.FindObjectsOfType<SwayMotion>())
        {
            var so = S(s);
            sways.Add(new Sway { t = s.transform, rot = s.transform.localRotation, axis = so.FindProperty("swayAxis").vector3Value.normalized,
                angle = F(so, "swayAngle"), cps = F(so, "swayCyclesPerSecond"), ph = F(so, "phaseOffset") });
        }
        int i = 0;
        foreach (var f in Object.FindObjectsOfType<LightFlicker>())
        {
            var so = S(f); var l = f.GetComponent<Light>();
            flickers.Add(new Flicker { l = l, baseI = l.intensity, amount = F(so, "flickerAmount"), speed = F(so, "flickerSpeed"), seed = 13.7f * (++i) });
        }
        foreach (var g in Object.FindObjectsOfType<GullCircle>())
        {
            var so = S(g);
            gulls.Add(new Gull { t = g.transform, pos = g.transform.localPosition, rot = g.transform.localRotation, radius = F(so, "radius"), dps = F(so, "degreesPerSecond"),
                start = F(so, "startAngle"), hw = F(so, "heightWave"), hc = F(so, "heightCyclesPerSecond"), bank = F(so, "bankDegrees"), cw = so.FindProperty("clockwise").boolValue });
        }
        particles = Object.FindObjectsOfType<ParticleSystem>();
        var water = Object.FindObjectOfType<WaterScroll>();
        seaMaterial = water.GetComponent<Renderer>().sharedMaterial;
        seaOffset0 = seaMaterial.GetTextureOffset("_BaseMap");
        seaSpeed = S(water).FindProperty("scrollSpeed").vector2Value;
    }

    static void ApplyAnimated(float t, bool reset)
    {
        const float TwoPi = Mathf.PI * 2f;
        foreach (var b in bobs)
        {
            if (reset) { b.t.localPosition = b.pos; b.t.localRotation = b.rot; continue; }
            b.t.localPosition = b.pos + Vector3.up * Mathf.Sin(t * b.hc * TwoPi + b.ph) * b.h;
            b.t.localRotation = b.rot * Quaternion.Euler(Mathf.Sin(t * b.pc * TwoPi + b.ph * 0.6f) * b.p, 0f, Mathf.Sin(t * b.rc * TwoPi + b.ph * 1.7f) * b.r);
        }
        foreach (var s in spins) s.t.localRotation = reset ? s.rot : s.rot * Quaternion.Euler(s.dps * t);
        foreach (var s in sways)
            s.t.localRotation = reset ? s.rot : s.rot * Quaternion.AngleAxis(Mathf.Sin(t * s.cps * TwoPi + s.ph) * s.angle, s.axis);
        foreach (var f in flickers)
            f.l.intensity = reset ? f.baseI : f.baseI * (1f + (Mathf.PerlinNoise(f.seed, t * f.speed) - 0.5f) * 2f * f.amount);
        foreach (var g in gulls)
        {
            if (reset) { g.t.localPosition = g.pos; g.t.localRotation = g.rot; continue; }
            float dir = g.cw ? -1f : 1f;
            float a = (g.start + t * g.dps * dir) * Mathf.Deg2Rad;
            float h = Mathf.Sin(t * g.hc * TwoPi + g.start) * g.hw;
            g.t.localPosition = new Vector3(Mathf.Cos(a) * g.radius, h, Mathf.Sin(a) * g.radius);
            Vector3 tangent = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a)) * dir;
            g.t.localRotation = Quaternion.LookRotation(tangent, Vector3.up) * Quaternion.Euler(0f, 0f, g.bank * dir);
        }
        Vector2 off = reset ? seaOffset0 : seaOffset0 + seaSpeed * t;
        seaMaterial.SetTextureOffset("_BaseMap", new Vector2(off.x % 1f, off.y % 1f));
    }

    // ------------------------------------------------------------------ captions
    static void MakeCaption()
    {
        captionCanvas = new GameObject("_WalkthroughCaption");
        captionCanvas.hideFlags = HideFlags.DontSave;
        var canvas = captionCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 1f;
        var scaler = captionCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(Width, Height);

        var go = new GameObject("Caption");
        go.hideFlags = HideFlags.DontSave;
        go.transform.SetParent(captionCanvas.transform, false);
        captionText = go.AddComponent<Text>();
        captionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        captionText.fontSize = 38;
        captionText.alignment = TextAnchor.MiddleCenter;
        captionText.color = new Color(1f, 0.96f, 0.9f);
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);
        var rtf = captionText.rectTransform;
        rtf.anchorMin = new Vector2(0f, 0.05f); rtf.anchorMax = new Vector2(1f, 0.15f);
        rtf.offsetMin = Vector2.zero; rtf.offsetMax = Vector2.zero;
    }
}
