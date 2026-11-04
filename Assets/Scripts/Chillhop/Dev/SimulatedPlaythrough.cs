#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Editor-only. Plays the real scene with a virtual hand (fingertip sphere + fake pinch point) to prove the
/// wiring works end to end. Run: Unity -executeMethod ChillhopTest.Run  (see Assets/Editor/ChillhopTest.cs)
/// </summary>
public class SimulatedPlaythrough : MonoBehaviour
{
    const System.Reflection.BindingFlags BF = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
    int fails;
    float lampBase = 6f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (Environment.GetCommandLineArgs().Contains("-chillhopTest")) new GameObject("Playthrough").AddComponent<SimulatedPlaythrough>();
    }

    void Check(bool ok, string what) { Debug.Log((ok ? "PASS " : "FAIL ") + what); if (!ok) fails++; }
    static void SetField(object o, string n, object v) { o.GetType().GetField(n, BF).SetValue(o, v); }

    // Marketing shots: a wide hero camera from just behind the player, or a closer one aimed at a point.
    void Shot(string name, Vector3? eye = null, Vector3? look = null, float fov = 60f)
    {
        var go = new GameObject("ShotCam");
        var cam = go.AddComponent<Camera>();
        var main = Camera.main;
        if (main != null)
        {
            cam.clearFlags = main.clearFlags; cam.backgroundColor = main.backgroundColor;
            var d = main.GetUniversalAdditionalCameraData();
            var c = cam.GetUniversalAdditionalCameraData();
            c.renderPostProcessing = d.renderPostProcessing; c.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        }
        cam.nearClipPlane = 0.03f; cam.fieldOfView = fov;
        go.transform.position = eye ?? new Vector3(0.1f, 1.86f, -0.85f);
        go.transform.rotation = Quaternion.LookRotation((look ?? new Vector3(0.0f, 1.3f, 0.7f)) - go.transform.position);
        const int W = 1600, H = 900;
        var rt = new RenderTexture(W, H, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        Directory.CreateDirectory("Shots");
        File.WriteAllBytes("Shots/" + name + ".png", tex.EncodeToPNG());
        Destroy(go); Destroy(rt); Destroy(tex);
    }

    IEnumerator Poke(Transform tip, Transform target, Vector3 fromLocal, Vector3 toLocal)
    {
        for (int i = 0; i <= 25; i++)
        {
            tip.position = target.TransformPoint(Vector3.Lerp(fromLocal, toLocal, i / 25f));
            yield return new WaitForFixedUpdate();
            yield return null;
        }
        yield return new WaitForSeconds(0.1f);
        tip.position = target.TransformPoint(fromLocal + new Vector3(0, 0, -0.5f));
        yield return new WaitForSeconds(0.5f);
    }

    // Same as Poke, but photographs the pad with the finger pressed in and the cap flashing.
    IEnumerator PokeShot(Transform tip, Transform target, Vector3 fromLocal, Vector3 toLocal)
    {
        for (int i = 0; i <= 25; i++)
        {
            tip.position = target.TransformPoint(Vector3.Lerp(fromLocal, toLocal, i / 25f));
            yield return new WaitForFixedUpdate();
            yield return null;
            if (i == 25) Shot("04_pad", new Vector3(0.2f, 1.45f, 0.1f), target.position + new Vector3(0.1f, 0.03f, 0.05f), 52f);
        }
        yield return new WaitForSeconds(0.1f);
        tip.position = target.TransformPoint(fromLocal + new Vector3(0, 0, -0.5f));
        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator Start()
    {
        yield return new WaitForSeconds(1.5f);
        var mgr = VinylSimulationManager.Instance;
        Check(mgr != null && mgr.CurrentStep == SimStep.MenuStart, "manager up at MenuStart");
        Shot("01_start");
        lampBase = GameObject.Find("DeskLampLight").GetComponent<Light>().intensity;
        Shot("01_start_close", new Vector3(0, 1.5f, 0.0f), new Vector3(0, 1.45f, 0.8f), 50f);
        Shot("room_front", new Vector3(0, 1.6f, -0.3f), new Vector3(0, 1.4f, 3f), 85f);
        Shot("room_left", new Vector3(0, 1.6f, 0.2f), new Vector3(-3f, 1.1f, 1.2f), 85f);
        Shot("room_right", new Vector3(0, 1.6f, 0.2f), new Vector3(3f, 1.3f, 1.0f), 85f);
        Shot("room_back", new Vector3(0, 1.6f, 0.2f), new Vector3(0.3f, 1.3f, -2f), 85f);
        Shot("room_deck", new Vector3(0.0f, 1.62f, -0.35f), new Vector3(0.0f, 1.0f, 0.7f), 62f);

        var tipGo = new GameObject("TestTip");
        tipGo.transform.position = new Vector3(0, -5, 0);
        tipGo.AddComponent<SphereCollider>().radius = 0.008f;
        tipGo.AddComponent<Rigidbody>();
        tipGo.AddComponent<VRFingertip>();
        Transform tip = tipGo.transform;
        var fingerVis = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(fingerVis.GetComponent<Collider>());
        fingerVis.transform.SetParent(tip, false); fingerVis.transform.localScale = Vector3.one * 0.05f;
        fingerVis.GetComponent<Renderer>().material.color = new Color(0.93f, 0.72f, 0.58f);

        // pretend-hand pieces
        var hand = FindObjectsByType<XRPokeInteractor>(FindObjectsInactive.Include)[0];
        Check(hand != null, "rig has a poke interactor");
        int tips = FindObjectsByType<VRFingertip>(FindObjectsInactive.Include).Length;
        Check(tips >= 3, "fingertips on rig (" + (tips - 1) + ")");
        var attach = new GameObject("TestPinch").transform;

        // 1) START
        Transform start = GameObject.Find("StartButton").transform;
        yield return Poke(tip, start, new Vector3(0, 0, -0.05f), new Vector3(0, 0, 0.02f));
        Check(mgr.CurrentStep == SimStep.Step1_PlaceVinyl, "START poke -> step 1 (" + mgr.CurrentStep + ")");

        // 2) vinyl drop
        var vinyl = GameObject.Find("VinylRecord");
        var platter = GameObject.Find("Platter").transform;
        var vrb = vinyl.GetComponent<Rigidbody>();
        vinyl.transform.position = platter.position + Vector3.up * 0.14f + new Vector3(0.03f, 0, 0.02f);
        vrb.linearVelocity = Vector3.zero;
        yield return new WaitForSeconds(2.5f);
        Check(mgr.CurrentStep == SimStep.Step2_DropNeedle, "vinyl drop -> step 2 (" + mgr.CurrentStep + ")");
        Check(vinyl.transform.parent != null && vinyl.transform.parent.name == "SnapPoint", "vinyl on SnapPoint");
        yield return new WaitForSeconds(2f);
        Check(Quaternion.Angle(platter.localRotation, Quaternion.identity) > 1f, "platter spinning");
        Shot("02_vinyl", new Vector3(-0.2f, 1.62f, 0.05f), new Vector3(-0.3f, 1.02f, 0.68f), 50f);

        // 3) tonearm swing
        var arm = FindAnyObjectByType<VRTonearm>();
        Transform pivot = (Transform)typeof(VRTonearm).GetField("arm", BF).GetValue(arm);
        Vector3 off = arm.transform.position - pivot.position; off.y = 0;
        attach.position = arm.transform.position;
        SetField(arm, "activeInteractor", hand);
        SetField(arm, "attach", attach);
        SetField(arm, "prevDir", off.normalized);
        SetField(arm, "hasPrevDir", true);
        for (int a = 3; a <= 60; a += 3)
        {
            attach.position = pivot.position + Quaternion.AngleAxis(a, Vector3.up) * off + Vector3.up * 0.025f;
            yield return null;
        }
        yield return new WaitForSeconds(0.6f);
        Check(mgr.CurrentStep == SimStep.Step3_TriggerSamplePad, "tonearm -> step 3 (" + mgr.CurrentStep + ")");
        var drum = GameObject.Find("DrumLoop").GetComponent<AudioSource>();
        yield return new WaitForSeconds(3f);
        Check(drum.isPlaying && drum.volume > 0.05f, "drum loop playing after needle drop (vol " + drum.volume + ")");
        Check(GameObject.Find("NeedleCrackle").GetComponent<AudioSource>().isPlaying, "crackle playing");
        Shot("03_tonearm", new Vector3(-0.05f, 1.55f, 0.1f), new Vector3(-0.3f, 1.05f, 0.75f), 45f);

        // 4) pad
        Transform pad = GameObject.Find("PadButton").transform;
        yield return PokeShot(tip, pad, new Vector3(0, 0.06f, 0), new Vector3(0, 0.0f, 0));
        Check(mgr.CurrentStep == SimStep.Step4_FilterKnob, "pad poke -> step 4 (" + mgr.CurrentStep + ")");
        yield return new WaitForSeconds(13f);
        Check(GameObject.Find("KeysLoop").GetComponent<AudioSource>().isPlaying, "keys loop playing, synced");


        // 5) knob
        var lp = FindAnyObjectByType<AudioLowPassFilter>();
        Check(Mathf.Abs(lp.cutoffFrequency - 800f) < 1f, "filter starts at 800 Hz (" + lp.cutoffFrequency + ")");
        var knob = GameObject.Find("FilterKnob").GetComponent<VRFaderKnob>();
        Vector3 kc = knob.transform.position;
        Vector3 koff = new Vector3(0, 0.03f, 0.05f);
        attach.position = kc + koff;
        SetField(knob, "activeInteractor", hand);
        SetField(knob, "attach", attach);
        typeof(VRFaderKnob).GetMethod("BeginDrag", BF).Invoke(knob, null);
        for (int a = 3; a <= 99; a += 3)
        {
            attach.position = kc + Quaternion.AngleAxis(a, Vector3.up) * koff;
            yield return null;
        }
        yield return new WaitForSeconds(1.2f);
        Shot("05_filter", new Vector3(0.28f, 1.5f, 0.1f), new Vector3(0.28f, 1.0f, 0.78f), 50f);
        Check(mgr.CurrentStep == SimStep.Step5_MasterFader, "knob -> step 5 (" + mgr.CurrentStep + ")");
        Check(Mathf.Abs(lp.cutoffFrequency - 20000f) < 5f, "filter open at 20 kHz (" + lp.cutoffFrequency + ")");
        Check(Mathf.Abs(AudioListener.volume - 0.3f) < 0.02f, "master still 0.3 before fader (" + AudioListener.volume + ")");

        // 6) fader
        var fader = GameObject.Find("FaderHandle").GetComponent<VRFaderKnob>();
        attach.position = fader.transform.position;
        SetField(fader, "activeInteractor", hand);
        SetField(fader, "attach", attach);
        typeof(VRFaderKnob).GetMethod("BeginDrag", BF).Invoke(fader, null);
        Vector3 f0 = fader.transform.position;
        for (int i = 1; i <= 20; i++) { attach.position = f0 + Vector3.up * (i * 0.01f); yield return null; }
        yield return new WaitForSeconds(1.5f);
        Check(mgr.CurrentStep == SimStep.Complete, "fader -> Complete (" + mgr.CurrentStep + ")");
        Check(Mathf.Abs(AudioListener.volume - 1f) < 0.02f, "master at 1.0 (" + AudioListener.volume + ")");
        yield return new WaitForSeconds(4f);
        var lamp = GameObject.Find("DeskLampLight").GetComponent<Light>();
        Check(lamp.intensity < lampBase * 0.6f, "lamp dimmed (" + lamp.intensity + ")");
        Check(GameObject.Find("CompletePanel") != null, "complete panel present");
        Shot("06_complete");

        // 7) restart
        Transform restart = GameObject.Find("RestartButton").transform;
        yield return Poke(tip, restart, new Vector3(0, 0, -0.05f), new Vector3(0, 0, 0.02f));
        yield return new WaitForSeconds(0.5f);
        Check(mgr.CurrentStep == SimStep.MenuStart, "restart -> MenuStart (" + mgr.CurrentStep + ")");
        Check(vinyl.transform.parent != null && vinyl.transform.parent.name == "Deck", "vinyl back home");
        Check(Mathf.Abs(vinyl.transform.position.x - 0.78f) < 0.01f, "vinyl at crate");
        Check(!drum.isPlaying, "audio stopped");
        Check(Mathf.Abs(lp.cutoffFrequency - 800f) < 1f && Mathf.Abs(AudioListener.volume - 0.3f) < 0.02f, "audio reset to 800 Hz / 0.3");
        Check(Quaternion.Angle(platter.localRotation, Quaternion.identity) < 0.5f, "platter reset");
        yield return new WaitForSeconds(3.5f);
        Check(Mathf.Abs(lamp.intensity - lampBase) < 0.1f, "lamp restored");

        Debug.Log("PLAYTHROUGH DONE fails=" + fails);
        UnityEditor.EditorApplication.Exit(fails);
    }
}
#endif
