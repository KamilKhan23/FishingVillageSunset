using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Renders Game-view quality screenshots from the scene's Main Camera without entering Play mode.
/// Used to export the deliverable screenshots (Tools > Fishing Village > Export Screenshots).
/// </summary>
public static class SceneCapture
{
    public static string DeliverablesFolder
    {
        get { return Path.GetFullPath(Path.Combine(Application.dataPath, "../Deliverables")); }
    }

    public static Camera FindMainCamera()
    {
        foreach (var c in Object.FindObjectsOfType<Camera>()) if (c.CompareTag("MainCamera")) return c;
        return null;
    }

    public static void Capture(string file, Vector3 position, Quaternion rotation, int width = 1920, int height = 1080)
    {
        var cam = FindMainCamera();
        Vector3 p0 = cam.transform.position; Quaternion r0 = cam.transform.rotation;
        var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        cam.transform.SetPositionAndRotation(position, rotation);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = null;
        Directory.CreateDirectory(Path.GetDirectoryName(file));
        File.WriteAllBytes(file, tex.EncodeToPNG());
        cam.transform.SetPositionAndRotation(p0, r0);
        rt.Release();
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
    }

    public static void Capture(string file, Vector3 position, Vector3 lookAt, int width = 1920, int height = 1080)
    {
        Capture(file, position, Quaternion.LookRotation(lookAt - position, Vector3.up), width, height);
    }

    [MenuItem("Tools/Fishing Village/Export Screenshots")]
    public static void ExportDeliverableScreenshots()
    {
        var cam = FindMainCamera();
        var so = new SerializedObject(cam.GetComponent<ExplorerCamera>());
        var vps = so.FindProperty("viewpoints");
        string[] names = { "Screenshot_1_Viewpoint1_HillsideArrival.png", "Screenshot_2_Viewpoint2_PierEnd.png", "Screenshot_3_Viewpoint3_LighthouseHeadland.png" };
        for (int i = 0; i < 3; i++)
        {
            var t = (Transform)vps.GetArrayElementAtIndex(i).objectReferenceValue;
            Capture(Path.Combine(DeliverablesFolder, names[i]), t.position, t.rotation);
        }
        Capture(Path.Combine(DeliverablesFolder, "Screenshot_4_BestShot_HarbourAtSunset.png"), BestShotPosition, BestShotLookAt);
        Debug.Log("Screenshots exported to " + DeliverablesFolder);
    }

    public static Vector3 BestShotPosition = new Vector3(172f, 12.6f, 148f);
    public static Vector3 BestShotLookAt = new Vector3(122f, 15f, 196f);
}
