using System.IO;
using UnityEditor.Android;
using UnityEngine;

/// <summary>Asks the Quest runtime for high-rate (60 Hz) hand tracking, which makes pinches and drags much more responsive.</summary>
public class QuestHandTrackingManifest : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 100;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string manifest = Path.Combine(path, "src/main/AndroidManifest.xml");
        if (!File.Exists(manifest)) { Debug.LogWarning("CHILLHOP: manifest not found at " + manifest); return; }
        string xml = File.ReadAllText(manifest);
        if (xml.Contains("com.oculus.handtracking.frequency")) return;
        const string tag = "</application>";
        int i = xml.LastIndexOf(tag);
        if (i < 0) { Debug.LogWarning("CHILLHOP: no </application> in manifest"); return; }
        string add =
            "    <meta-data android:name=\"com.oculus.handtracking.frequency\" android:value=\"HIGH\" />\n" +
            "    <meta-data android:name=\"com.oculus.handtracking.version\" android:value=\"V2.0\" />\n  ";
        File.WriteAllText(manifest, xml.Insert(i, add));
        Debug.Log("CHILLHOP: added high-frequency hand tracking to the manifest");
    }
}
