using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.Events;

/// <summary>Builds the whole Night Shift Vinyl scene from code. Run via Chillhop/Build Scene.</summary>
public static class ChillhopBuilder
{
    const string ScenePath = "Assets/Scenes/ChillhopCrate.unity";
    const string MatDir = "Assets/Materials/Chillhop";
    static readonly Vector3 RigOffset = new Vector3(0.2f, 0f, 0.1f);
    const float RigScale = 1.25f;      // scales the player up relative to the room: hands reach 25% further, the room reads a little smaller
    const string AudioDir = "Assets/Audio/Chillhop/";
    const string RigPrefab = "Assets/Samples/XR Interaction Toolkit/3.6.0/Hands Interaction Demo/Prefabs/XR Origin Hands (XR Rig).prefab";

    static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

    // Layout constants (metres). Player head sits at (0, 1.5, 0) looking +Z.
    static readonly Vector3 Head = new Vector3(0f, 1.5f, 0f);
    static readonly Vector3 PlatterC = new Vector3(-0.42f, 1.04f, 0.62f);
    static readonly Vector3 ArmPivotPos = new Vector3(-0.12f, 1.075f, 0.82f);
    const float ArmLength = 0.22f;
    const float PlayAngle = 40f;

    [MenuItem("Chillhop/Build Scene")]
    public static void BuildScene()
    {
        InternalEditorUtilityAddTag("Vinyl");
        SetFixedTimestep();
        EnableHandFeatures();
        ConfigurePlayer();
        Directory.CreateDirectory(MatDir);
        Directory.CreateDirectory("Assets/Scenes");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.30f, 0.23f, 0.18f);
        RenderSettings.fog = false;
        ChillhopDecor.Environment();

        GameObject rig = BuildRig();
        BuildRoom();
        var lights = BuildLights();
        GameObject vinyl = BuildDeck(out VRSnapZone snapZone, out PlatterSpinner spinner, out VRTonearm tonearm);
        BuildMixer(out VRPokeButton pad, out VRFaderKnob knob, out VRFaderKnob fader);
        var audio = BuildAudio(rig);
        var ui = BuildUi(out VRPokeButton startBtn, out VRPokeButton restartBtn);
        foreach (var panelRoot in new[] { ui.intro, ui.checklist, ui.complete })
            if (panelRoot != null) panelRoot.transform.position += RigOffset + new Vector3(0f, 0.2f, 0.05f);   // panels were laid out for a head at the origin
        var mgr = BuildManager(audio, ui, lights);

        Wire(mgr, audio, snapZone, spinner, tonearm, pad, knob, fader, startBtn, restartBtn);
        ChillhopDecor.Apply();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("CHILLHOP: scene built at " + ScenePath);
    }

    // ------------------------------------------------------------------ project settings

    static void InternalEditorUtilityAddTag(string tag)
    {
        UnityEditorInternal.InternalEditorUtility.AddTag(tag);
    }

    static void SetFixedTimestep()
    {
        var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TimeManager.asset")[0]);
        tm.FindProperty("Fixed Timestep").floatValue = 1f / 72f;
        tm.ApplyModifiedProperties();
    }

    static void EnableHandFeatures()
    {
        var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        if (settings == null)
        {
            Debug.LogWarning("CHILLHOP: no OpenXR settings for Android");
            return;
        }
        foreach (OpenXRFeature f in settings.GetFeatures<OpenXRFeature>())
        {
            string n = f.name + " " + f.GetType().Name;
            bool wanted = (n.Contains("Hand") && !n.Contains("Microsoft")) || (n.Contains("Quest") && n.Contains("Support"));
            Debug.Log("CHILLHOP feature: " + n + " enabled=" + f.enabled + (wanted ? " (wanted)" : ""));
            if (n.Contains("Microsoft") && f.enabled) { f.enabled = false; EditorUtility.SetDirty(f); }
            if (wanted && !f.enabled)
            {
                f.enabled = true;
                EditorUtility.SetDirty(f);
            }
        }
        EditorUtility.SetDirty(settings);
    }

    static void ConfigurePlayer()
    {
        PlayerSettings.productName = "Night Shift Vinyl";
        PlayerSettings.companyName = "nmoffett";
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.nmoffett.nightshiftvinyl");
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.bundleVersion = "1.0.0";
    }

    // ------------------------------------------------------------------ rig

    static GameObject BuildRig()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RigPrefab);
        var rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        rig.name = "XR Origin Hands (XR Rig)";
        rig.transform.localScale = Vector3.one * RigScale;
        rig.transform.position = RigOffset;   // stand a little forward and right so the whole deck is within arm's reach

        // The hand interactors read pinch/select from the XRI Default Input Actions; nothing enables them unless an
        // InputActionManager does (the sample scenes include one, the rig prefab does not).
        var actionsAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
            "Assets/Samples/XR Interaction Toolkit/3.6.0/Starter Assets/XRI Default Input Actions.inputactions");
        var iamGo = new GameObject("Input Action Manager");
        var iam = iamGo.AddComponent<UnityEngine.XR.Interaction.Toolkit.Inputs.InputActionManager>();
        iam.actionAssets = new List<UnityEngine.InputSystem.InputActionAsset> { actionsAsset };

        // Optical tracking rarely reports a perfect 1.0 pinch, and the sample demands one (press 1.0 / release 0.9).
        // A pinch now registers at 80% and holds until it relaxes below 60%.
        foreach (MonoBehaviour mb in rig.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb != null && mb.GetType().Name == "ReleaseThresholdButtonReader")
            {
                Set(mb, "m_PressThreshold", 0.8f);
                Set(mb, "m_ReleaseThreshold", 0.6f);
            }
        }

        DumpHierarchy(rig.transform, "");

        // Stationary scene: strip locomotion and teleport.
        var kill = new List<GameObject>();
        foreach (Transform t in rig.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name.ToLowerInvariant();
            if (t == rig.transform) continue;
            if (n.Contains("teleport") || n == "locomotion" || n.Contains("hand menu") || n == "hand menu rig") kill.Add(t.gameObject);
        }
        foreach (GameObject g in kill) if (g != null) Object.DestroyImmediate(g);

        var origin = rig.GetComponentInChildren<XROrigin>(true);
        origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;
        origin.CameraYOffset = Head.y;

        Camera cam = origin.Camera;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.03f, 0.03f);
        cam.nearClipPlane = 0.05f;
        if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
        cam.gameObject.AddComponent<AudioLowPassFilter>().cutoffFrequency = 800f;

        // Fingertip triggers for the physical poke buttons.
        foreach (XRPokeInteractor poke in rig.GetComponentsInChildren<XRPokeInteractor>(true))
        {
            Transform attach = poke.attachTransform != null ? poke.attachTransform : poke.transform;
            var tip = new GameObject("Fingertip");
            tip.transform.SetParent(attach, false);
            tip.AddComponent<SphereCollider>().radius = 0.008f;
            tip.AddComponent<Rigidbody>();
            tip.AddComponent<VRFingertip>();
            Debug.Log("CHILLHOP: fingertip added under " + attach.name + " of " + poke.name);
        }
        return rig;
    }

    static void DumpHierarchy(Transform t, string indent)
    {
        Debug.Log("CHILLHOP rig: " + indent + t.name);
        foreach (Transform c in t) DumpHierarchy(c, indent + "  ");
    }

    // ------------------------------------------------------------------ materials + primitives

    static Material Mat(string name, Color color, float metallic = 0f, float smooth = 0.3f, Color? emission = null)
    {
        if (mats.TryGetValue(name, out Material cached)) return cached;
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Metallic", metallic);
        m.SetFloat("_Smoothness", smooth);
        if (emission.HasValue)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission.Value);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        AssetDatabase.CreateAsset(m, MatDir + "/" + name + ".mat");
        mats[name] = m;
        return m;
    }

    static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool collider = false)
    {
        GameObject g = GameObject.CreatePrimitive(type);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider) Object.DestroyImmediate(g.GetComponent<Collider>());
        return g;
    }

    static GameObject Empty(string name, Transform parent, Vector3 pos)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        return g;
    }

    // ------------------------------------------------------------------ room, desk, lights

    static void BuildRoom()
    {
        Material floor = Mat("Floor", new Color(0.13f, 0.095f, 0.07f), 0f, 0.35f);
        Material wall = Mat("Wall", new Color(0.19f, 0.13f, 0.10f), 0f, 0.1f);
        Material wood = Mat("DeskWood", new Color(0.36f, 0.22f, 0.12f), 0f, 0.45f);
        Material dark = Mat("DeskDark", new Color(0.12f, 0.08f, 0.06f), 0f, 0.3f);

        var room = Empty("Room", null, Vector3.zero).transform;
        Prim(PrimitiveType.Cube, "Floor", room, new Vector3(0, -0.1f, 1f), new Vector3(6, 0.2f, 6), floor, true);
        Prim(PrimitiveType.Cube, "BackWall", room, new Vector3(0, 1.5f, 3.1f), new Vector3(6, 3.2f, 0.2f), wall, true);
        Prim(PrimitiveType.Cube, "LeftWall", room, new Vector3(-3.1f, 1.5f, 1f), new Vector3(0.2f, 3.2f, 6), wall, true);
        Prim(PrimitiveType.Cube, "RightWall", room, new Vector3(3.1f, 1.5f, 1f), new Vector3(0.2f, 3.2f, 6), wall, true);
        Prim(PrimitiveType.Cube, "FrontWall", room, new Vector3(0, 1.5f, -2.1f), new Vector3(6, 3.2f, 0.2f), wall, true);
        Prim(PrimitiveType.Cube, "Ceiling", room, new Vector3(0, 3.2f, 1f), new Vector3(6, 0.2f, 6), wall, true);

        var desk = Empty("Desk", null, new Vector3(0, 0, 0.65f)).transform;
        Prim(PrimitiveType.Cube, "DeskBody", desk, new Vector3(0, 0.45f, 0), new Vector3(1.9f, 0.9f, 0.8f), dark, true);
        Prim(PrimitiveType.Cube, "DeskTop", desk, new Vector3(0, 0.925f, 0), new Vector3(2.0f, 0.05f, 0.9f), wood, true);

    }

    class LightRefs
    {
        public Light lamp;
        public Light[] neon;
    }

    static LightRefs BuildLights()
    {
        var refs = new LightRefs();
        var root = Empty("Lighting", null, Vector3.zero).transform;

        var lampObj = Empty("DeskLampLight", root, new Vector3(-0.98f, 1.45f, 0.30f));
        lampObj.transform.rotation = Quaternion.LookRotation(new Vector3(0.45f, 1.0f, 0.65f) - lampObj.transform.position);
        var lamp = lampObj.AddComponent<Light>();
        lamp.type = LightType.Spot;
        lamp.color = new Color(1f, 0.72f, 0.38f);
        lamp.intensity = 6f;
        lamp.range = 3.5f;
        lamp.spotAngle = 95f;
        lamp.shadows = LightShadows.None;
        refs.lamp = lamp;

        // Lamp visual
        Material brass = Mat("Brass", new Color(0.7f, 0.5f, 0.2f), 0.8f, 0.5f);
        Material bulb = Mat("Bulb", new Color(1f, 0.8f, 0.5f), 0f, 0.5f, new Color(3f, 1.8f, 0.7f));
        var lampVis = Empty("LampVisual", root, new Vector3(-0.98f, 0.95f, 0.30f)).transform;
        Prim(PrimitiveType.Cylinder, "Base", lampVis, new Vector3(0, 0.01f, 0), new Vector3(0.14f, 0.01f, 0.14f), brass);
        Prim(PrimitiveType.Cylinder, "Stem", lampVis, new Vector3(0, 0.25f, 0), new Vector3(0.015f, 0.25f, 0.015f), brass);
        Prim(PrimitiveType.Sphere, "Bulb", lampVis, new Vector3(0, 0.5f, 0), new Vector3(0.11f, 0.11f, 0.11f), bulb);

        // Neon accents on the back wall
        Color[] colors = { new Color(1f, 0.2f, 0.7f), new Color(0.2f, 0.9f, 1f), new Color(0.7f, 0.3f, 1f) };
        float[] xs = { -1.7f, 0f, 1.7f };
        refs.neon = new Light[3];
        for (int i = 0; i < 3; i++)
        {
            Material neonMat = Mat("Neon" + i, colors[i] * 0.4f, 0f, 0.5f, colors[i] * 2.2f);
            Prim(PrimitiveType.Cube, "NeonBar" + i, root, new Vector3(xs[i], 2.4f, 2.97f), new Vector3(0.9f, 0.05f, 0.05f), neonMat);
            var l = Empty("NeonLight" + i, root, new Vector3(xs[i], 2.3f, 2.6f)).AddComponent<Light>();
            l.type = LightType.Point;
            l.color = colors[i];
            l.intensity = 0.4f;
            l.range = 4.5f;
            l.shadows = LightShadows.None;
            refs.neon[i] = l;
        }

        // Soft fill so the room is never pitch black.
        var fill = Empty("FillLight", root, new Vector3(0, 2.8f, 0.5f)).AddComponent<Light>();
        fill.type = LightType.Point;
        fill.color = new Color(1f, 0.85f, 0.7f);
        fill.intensity = 1.0f;
        fill.range = 6f;
        fill.shadows = LightShadows.None;
        return refs;
    }

    // ------------------------------------------------------------------ deck: platter, vinyl, tonearm, crate

    static GameObject BuildDeck(out VRSnapZone zone, out PlatterSpinner spinner, out VRTonearm tonearm)
    {
        Material body = Mat("TurntableBody", new Color(0.09f, 0.07f, 0.06f), 0.2f, 0.5f);
        Material platterMat = Mat("Platter", new Color(0.55f, 0.55f, 0.58f), 0.9f, 0.6f);
        Material vinylMat = Mat("Vinyl", new Color(0.03f, 0.03f, 0.035f), 0.3f, 0.85f);
        Material labelMat = Mat("VinylLabel", new Color(0.92f, 0.42f, 0.34f), 0f, 0.3f);
        Material metal = Mat("ArmMetal", new Color(0.78f, 0.78f, 0.8f), 0.9f, 0.7f);
        Material knobMat = Mat("Rubber", new Color(0.06f, 0.06f, 0.06f), 0f, 0.3f);
        Material crateMat = Mat("CrateWood", new Color(0.30f, 0.18f, 0.10f), 0f, 0.4f);

        var deck = Empty("Deck", null, Vector3.zero).transform;

        // Turntable
        Prim(PrimitiveType.Cube, "TurntableBase", deck, new Vector3(PlatterC.x + 0.05f, 0.98f, PlatterC.z + 0.05f), new Vector3(0.62f, 0.06f, 0.56f), body, true);

        var platter = Empty("Platter", deck, PlatterC);
        Prim(PrimitiveType.Cylinder, "PlatterMesh", platter.transform, new Vector3(0, -0.015f, 0), new Vector3(0.4f, 0.015f, 0.4f), platterMat);
        var snapPoint = Empty("SnapPoint", platter.transform, new Vector3(0, 0.006f, 0));   // record centre height
        Prim(PrimitiveType.Cylinder, "Spindle", platter.transform, new Vector3(0, 0.01f, 0), new Vector3(0.006f, 0.012f, 0.006f), metal);
        spinner = platter.AddComponent<PlatterSpinner>();

        var zoneObj = Empty("PlatterSnapZone", deck, new Vector3(PlatterC.x, PlatterC.y + 0.1f, PlatterC.z));
        var zoneCol = zoneObj.AddComponent<BoxCollider>();
        zoneCol.isTrigger = true;
        zoneCol.size = new Vector3(0.4f, 0.24f, 0.4f);
        zone = zoneObj.AddComponent<VRSnapZone>();

        // Record crate (stand) on the right, with the record resting on top
        var crate = Empty("RecordSleeve", deck, new Vector3(0.78f, 0.95f, 0.62f)).transform;
        Prim(PrimitiveType.Cube, "CrateBase", crate, new Vector3(0, 0.05f, 0), new Vector3(0.36f, 0.1f, 0.36f), crateMat, true);
        Prim(PrimitiveType.Cube, "CrateBack", crate, new Vector3(0, 0.17f, 0.17f), new Vector3(0.36f, 0.14f, 0.02f), crateMat);
        Prim(PrimitiveType.Cube, "CrateSideL", crate, new Vector3(-0.17f, 0.15f, 0), new Vector3(0.02f, 0.1f, 0.36f), crateMat);
        Prim(PrimitiveType.Cube, "CrateSideR", crate, new Vector3(0.17f, 0.15f, 0), new Vector3(0.02f, 0.1f, 0.36f), crateMat);

        // Vinyl record
        var vinyl = new GameObject("VinylRecord") { tag = "Vinyl" };
        vinyl.transform.SetParent(deck, false);
        vinyl.transform.position = new Vector3(0.78f, 1.05f + 0.008f, 0.62f);
        var col = vinyl.AddComponent<BoxCollider>();
        col.size = new Vector3(0.30f, 0.012f, 0.30f);
        var pinchZone = new GameObject("PinchZone");     // thick trigger so a pinch near the record counts
        pinchZone.transform.SetParent(vinyl.transform, false);
        var pz = pinchZone.AddComponent<BoxCollider>();
        pz.center = new Vector3(0, 0.04f, 0);       // extends upward only, so the record still rests at its old height
        pz.size = new Vector3(0.30f, 0.08f, 0.30f);
        var rb = vinyl.AddComponent<Rigidbody>();
        rb.mass = 0.2f;
        rb.linearDamping = 1f;
        rb.angularDamping = 2f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        Prim(PrimitiveType.Cylinder, "Disc", vinyl.transform, Vector3.zero, new Vector3(0.30f, 0.0015f, 0.30f), vinylMat);
        Prim(PrimitiveType.Cylinder, "Label", vinyl.transform, new Vector3(0, 0.0016f, 0), new Vector3(0.1f, 0.0005f, 0.1f), labelMat);
        var grab = vinyl.AddComponent<XRGrabInteractable>();
        vinyl.AddComponent<HeldSurfaceClamp>();
        grab.movementType = XRBaseInteractable.MovementType.Kinematic;
        grab.smoothPosition = true;
        grab.smoothPositionAmount = 5f;
        grab.smoothRotation = true;
        grab.smoothRotationAmount = 5f;
        grab.throwOnDetach = false;
        grab.forceGravityOnDetach = true;
        grab.useDynamicAttach = true;
        grab.attachEaseInTime = 0.15f;
        grab.selectMode = InteractableSelectMode.Single;
        grab.trackScale = false;
        var respawn = vinyl.AddComponent<RespawnIfFallen>();
        Set(respawn, "minY", -0.5f);

        SetRef(zone, "snapPoint", snapPoint.transform);
        SetRef(zone, "targetObject", vinyl);
        Set(zone, "targetTag", "Vinyl");
        SetEnum(zone, "expectedStep", (int)SimStep.Step1_PlaceVinyl);

        // Tonearm
        Prim(PrimitiveType.Cylinder, "ArmBase", deck, new Vector3(ArmPivotPos.x, 1.04f, ArmPivotPos.z), new Vector3(0.07f, 0.035f, 0.07f), metal);
        Vector3 playDir = (new Vector3(PlatterC.x, 0, PlatterC.z) - new Vector3(ArmPivotPos.x, 0, ArmPivotPos.z)).normalized;
        Vector3 restDir = Quaternion.AngleAxis(-PlayAngle, Vector3.up) * playDir;

        var pivot = Empty("ArmPivot", deck, ArmPivotPos);
        pivot.transform.rotation = Quaternion.LookRotation(restDir, Vector3.up);
        Prim(PrimitiveType.Cube, "ArmTube", pivot.transform, new Vector3(0, 0, ArmLength / 2f), new Vector3(0.012f, 0.012f, ArmLength), metal);
        Prim(PrimitiveType.Cube, "Headshell", pivot.transform, new Vector3(0, -0.006f, ArmLength), new Vector3(0.025f, 0.012f, 0.04f), Mat("Headshell", new Color(0.15f, 0.15f, 0.16f)));
        Prim(PrimitiveType.Cube, "Needle", pivot.transform, new Vector3(0, -0.016f, ArmLength + 0.012f), new Vector3(0.004f, 0.012f, 0.004f), metal);
        Prim(PrimitiveType.Cylinder, "Counterweight", pivot.transform, new Vector3(0, 0, -0.05f), new Vector3(0.03f, 0.02f, 0.03f) , knobMat).transform.localRotation = Quaternion.Euler(90, 0, 0);

        var handle = Empty("Handle", pivot.transform, new Vector3(0, 0.025f, ArmLength * 0.78f));
        var hc = handle.AddComponent<SphereCollider>();
        hc.radius = 0.07f;          // generous: a pinch only has to land near the knob
        var hrb = handle.AddComponent<Rigidbody>();
        hrb.isKinematic = true;
        hrb.useGravity = false;
        handle.AddComponent<XRSimpleInteractable>();
        Prim(PrimitiveType.Sphere, "HandleKnob", handle.transform, Vector3.zero, new Vector3(0.035f, 0.035f, 0.035f), Mat("HandleAmber", new Color(1f, 0.7f, 0.25f), 0f, 0.5f, new Color(0.8f, 0.4f, 0.05f)));
        Prim(PrimitiveType.Cylinder, "HandleStem", pivot.transform, new Vector3(0, 0.012f, ArmLength * 0.78f), new Vector3(0.006f, 0.012f, 0.006f), metal);
        tonearm = handle.AddComponent<VRTonearm>();
        SetRef(tonearm, "arm", pivot.transform);
        Set(tonearm, "restAngle", 0f);
        Set(tonearm, "playAngle", PlayAngle);
        Set(tonearm, "snapTolerance", 5f);

        return vinyl;
    }

    // ------------------------------------------------------------------ mixer: pad, knob, fader

    static void BuildMixer(out VRPokeButton pad, out VRFaderKnob knob, out VRFaderKnob fader)
    {
        Material body = Mat("MixerBody", new Color(0.12f, 0.12f, 0.14f), 0.3f, 0.45f);
        Material padBase = Mat("PadBase", new Color(0.05f, 0.05f, 0.05f), 0f, 0.3f);
        Material padCap = Mat("PadCap", new Color(0.95f, 0.55f, 0.1f), 0f, 0.55f, new Color(0.35f, 0.14f, 0f));
        Material rubber = Mat("Rubber", new Color(0.06f, 0.06f, 0.06f), 0f, 0.3f);
        Material white = Mat("Pointer", new Color(0.95f, 0.95f, 0.95f), 0f, 0.5f);
        Material track = Mat("FaderTrack", new Color(0.02f, 0.02f, 0.02f), 0f, 0.3f);
        Material fadeCap = Mat("FaderCap", new Color(0.9f, 0.9f, 0.92f), 0.3f, 0.5f);

        var mixer = Empty("Mixer", null, new Vector3(0.28f, 0.95f, 0.66f)).transform;
        Prim(PrimitiveType.Cube, "MixerBody", mixer, new Vector3(0, 0.03f, 0), new Vector3(0.5f, 0.06f, 0.46f), body, true);
        float top = 0.06f;

        // MPC pad
        var padObj = Empty("PadButton", mixer, new Vector3(-0.1f, top, -0.02f));
        var padTrig = padObj.AddComponent<BoxCollider>();
        padTrig.isTrigger = true;
        padTrig.center = new Vector3(0, 0.02f, 0);
        padTrig.size = new Vector3(0.14f, 0.08f, 0.14f);
        Prim(PrimitiveType.Cube, "PadBase", padObj.transform, new Vector3(0, 0.004f, 0), new Vector3(0.13f, 0.008f, 0.13f), padBase);
        var cap = Prim(PrimitiveType.Cube, "PadCap", padObj.transform, new Vector3(0, 0.02f, 0), new Vector3(0.1f, 0.024f, 0.1f), padCap);
        pad = padObj.AddComponent<VRPokeButton>();
        SetRef(pad, "cap", cap.transform);
        SetVec3(pad, "pressDirection", Vector3.down);
        Set(pad, "travel", 0.012f);
        Set(pad, "surfaceHeight", 0.012f);
        SetBool(pad, "gateByStep", true);
        SetEnum(pad, "requiredStep", (int)SimStep.Step3_TriggerSamplePad);
        SetRef(pad, "flashRenderer", cap.GetComponent<Renderer>());
        SetColor(pad, "idleGlow", new Color(0.35f, 0.14f, 0f));
        SetColor(pad, "flashGlow", new Color(1.5f, 0.6f, 0f));
        SetRef(pad, "pressClip", AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDir + "pad_hit.wav"));

        // Filter knob
        var knobObj = Empty("FilterKnob", mixer, new Vector3(0.1f, top + 0.03f, -0.02f));
        var kc = knobObj.AddComponent<CapsuleCollider>();
        kc.radius = 0.075f;
        kc.height = 0.12f;
        var krb = knobObj.AddComponent<Rigidbody>();
        krb.isKinematic = true;
        krb.useGravity = false;
        knobObj.AddComponent<XRSimpleInteractable>();
        Prim(PrimitiveType.Cylinder, "KnobBody", knobObj.transform, Vector3.zero, new Vector3(0.08f, 0.03f, 0.08f), rubber);
        Prim(PrimitiveType.Cube, "KnobPointer", knobObj.transform, new Vector3(0, 0.031f, 0.028f), new Vector3(0.008f, 0.004f, 0.03f), white);
        // tick marks on the mixer around the knob (0 to 90 degrees, clockwise from +Z toward +X)
        for (int i = 0; i <= 3; i++)
        {
            float a = i * 30f * Mathf.Deg2Rad;
            Prim(PrimitiveType.Cube, "Tick" + i, mixer, new Vector3(0.1f + Mathf.Sin(a) * 0.065f, top + 0.002f, -0.02f + Mathf.Cos(a) * 0.065f), new Vector3(0.006f, 0.002f, 0.006f), white);
        }
        knob = knobObj.AddComponent<VRFaderKnob>();
        SetEnum(knob, "mode", (int)VRFaderKnob.ControlMode.Rotary);
        SetVec3(knob, "axis", Vector3.up);
        Set(knob, "rotaryRange", 90f);
        SetEnum(knob, "output", (int)VRFaderKnob.OutputTarget.FilterCutoff);
        SetEnum(knob, "requiredStep", (int)SimStep.Step4_FilterKnob);

        // Master fader: a flat DJ-style channel fader lying on the board, sliding left (quiet) to right (loud)
        var panel = Empty("FaderTrack", mixer, new Vector3(0.0f, top + 0.0045f, -0.185f));
        Prim(PrimitiveType.Cube, "FaderSlot", panel.transform, new Vector3(0, 0.0006f, 0), new Vector3(0.23f, 0.002f, 0.009f), track);
        for (int i = 0; i <= 8; i++)
            Prim(PrimitiveType.Cube, "FaderTick" + i, panel.transform, new Vector3(-0.1f + i * 0.025f, 0.0008f, 0.03f), new Vector3(0.002f, 0.0015f, i % 2 == 0 ? 0.016f : 0.009f), white);
        var fh = Empty("FaderHandle", panel.transform, new Vector3(-0.1f, 0.009f, 0));
        var fc = fh.AddComponent<BoxCollider>();
        fc.size = new Vector3(0.09f, 0.08f, 0.11f);
        var frb = fh.AddComponent<Rigidbody>();
        frb.isKinematic = true;
        frb.useGravity = false;
        fh.AddComponent<XRSimpleInteractable>();
        Prim(PrimitiveType.Cube, "FaderCapMesh", fh.transform, Vector3.zero, new Vector3(0.028f, 0.016f, 0.05f), fadeCap);
        Prim(PrimitiveType.Cube, "FaderCapStripe", fh.transform, new Vector3(0, 0.0085f, 0), new Vector3(0.004f, 0.001f, 0.046f), Mat("Pointer", new Color(0.95f, 0.95f, 0.95f), 0f, 0.5f));
        fader = fh.AddComponent<VRFaderKnob>();
        SetEnum(fader, "mode", (int)VRFaderKnob.ControlMode.Slider);
        SetVec3(fader, "axis", Vector3.right);
        Set(fader, "sliderTravel", 0.2f);
        SetEnum(fader, "output", (int)VRFaderKnob.OutputTarget.MasterVolume);
        Set(fader, "volumeAtMin", 0.3f);
        Set(fader, "volumeAtMax", 1f);
        SetEnum(fader, "requiredStep", (int)SimStep.Step5_MasterFader);
    }

    // ------------------------------------------------------------------ audio

    class AudioRefs
    {
        public VinylAudioController controller;
        public AudioSource chime;
    }

    static AudioSource MakeSource(string name, Transform parent, string clip, bool loop)
    {
        var go = Empty(name, parent, Vector3.zero);
        var s = go.AddComponent<AudioSource>();
        s.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDir + clip);
        s.loop = loop;
        s.playOnAwake = false;
        s.spatialBlend = 0f;
        return s;
    }

    static AudioRefs BuildAudio(GameObject rig)
    {
        var refs = new AudioRefs();
        var root = Empty("Audio", null, Vector3.zero);
        var ctl = root.AddComponent<VinylAudioController>();
        var hum = MakeSource("PlatterHum", root.transform, "platter_hum_loop.wav", true);
        var crackle = MakeSource("NeedleCrackle", root.transform, "vinyl_crackle_loop.wav", true);
        var drums = MakeSource("DrumLoop", root.transform, "drums_loop.wav", true);
        var keys = MakeSource("KeysLoop", root.transform, "keys_loop.wav", true);
        var chime = MakeSource("UIChime", root.transform, "step_chime.wav", false);
        chime.bypassListenerEffects = true;

        SetRef(ctl, "humSource", hum);
        SetRef(ctl, "needleSource", crackle);
        SetRef(ctl, "drumSource", drums);
        SetRef(ctl, "keysSource", keys);
        SetRef(ctl, "lowPass", rig.GetComponentInChildren<AudioLowPassFilter>(true));
        refs.controller = ctl;
        refs.chime = chime;
        return refs;
    }

    // ------------------------------------------------------------------ world-space UI

    class UiRefs
    {
        public GameObject intro, checklist, complete;
        public TMP_Text checklistText, timeText;
    }

    static TMP_Text MakeText(string name, Transform parent, string text, float size, Color color, Vector2 anchorMin, Vector2 anchorMax, TextAlignmentOptions align, FontStyles style = FontStyles.Normal)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(30, 20);
        rt.offsetMax = new Vector2(-30, -20);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.fontStyle = style;
        t.textWrappingMode = TextWrappingModes.Normal;
        return t;
    }

    static GameObject MakePanel(string name, Vector3 pos, Vector2 size, Color bg, out Transform content)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas));
        go.transform.position = pos;
        go.transform.rotation = Quaternion.LookRotation(pos - Head, Vector3.up);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = null;
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one * 0.001f;

        var frame = new GameObject("Frame", typeof(RectTransform), typeof(Image));
        frame.transform.SetParent(go.transform, false);
        var fr = (RectTransform)frame.transform;
        fr.anchorMin = Vector2.zero;
        fr.anchorMax = Vector2.one;
        fr.offsetMin = fr.offsetMax = Vector2.zero;
        frame.GetComponent<Image>().color = new Color(0.50f, 0.32f, 0.17f);

        var bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(go.transform, false);
        var br = (RectTransform)bgObj.transform;
        br.anchorMin = Vector2.zero;
        br.anchorMax = Vector2.one;
        br.offsetMin = new Vector2(14, 14);
        br.offsetMax = new Vector2(-14, -14);
        bgObj.GetComponent<Image>().color = bg;
        content = go.transform;
        return go;
    }

    static VRPokeButton MakeBigButton(string name, Vector3 pos, string label, Color color, SimStep step)
    {
        var root = new GameObject(name);
        root.transform.position = pos;
        root.transform.rotation = Quaternion.LookRotation(pos - Head, Vector3.up);
        Vector3 faceFwd = Vector3.forward;                                    // away from the player, in local space
        var trig = root.AddComponent<BoxCollider>();
        trig.isTrigger = true;
        trig.center = new Vector3(0, 0, -0.02f);
        trig.size = new Vector3(0.26f, 0.12f, 0.08f);
        Prim(PrimitiveType.Cube, "ButtonBase", root.transform, new Vector3(0, 0, 0.022f), new Vector3(0.28f, 0.14f, 0.012f), Mat("ButtonBase", new Color(0.12f, 0.08f, 0.05f)));
        Material m = Mat("Button_" + name, color, 0f, 0.5f, color * 0.5f);
        var cap = Prim(PrimitiveType.Cube, "ButtonCap", root.transform, new Vector3(0, 0, 0.006f), new Vector3(0.24f, 0.10f, 0.024f), m);
        var tg = new GameObject("Label", typeof(RectTransform), typeof(Canvas));
        tg.transform.SetParent(cap.transform, false);
        tg.transform.localPosition = new Vector3(0, 0, -0.52f);
        tg.transform.localScale = new Vector3(1f / 0.24f * 0.001f, 1f / 0.10f * 0.001f, 1f / 0.024f * 0.001f);
        var c = tg.GetComponent<Canvas>();
        c.renderMode = RenderMode.WorldSpace;
        ((RectTransform)tg.transform).sizeDelta = new Vector2(200, 90);
        var txt = MakeText("Text", tg.transform, label, 40, new Color(0.1f, 0.05f, 0f), Vector2.zero, Vector2.one, TextAlignmentOptions.Center, FontStyles.Bold);
        ((RectTransform)txt.transform).offsetMin = ((RectTransform)txt.transform).offsetMax = Vector2.zero;
        txt.textWrappingMode = TextWrappingModes.NoWrap;
        txt.enableAutoSizing = true;
        txt.fontSizeMin = 20;
        txt.fontSizeMax = 40;

        var btn = root.AddComponent<VRPokeButton>();
        SetRef(btn, "cap", cap.transform);
        SetVec3(btn, "pressDirection", faceFwd);
        Set(btn, "travel", 0.012f);
        Set(btn, "surfaceHeight", 0.012f);
        SetBool(btn, "gateByStep", true);
        SetEnum(btn, "requiredStep", (int)step);
        SetRef(btn, "flashRenderer", cap.GetComponent<Renderer>());
        SetColor(btn, "idleGlow", color * 0.5f);
        SetColor(btn, "flashGlow", color * 2.2f);
        SetRef(btn, "pressClip", AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDir + "button_click.wav"));
        return btn;
    }

    static UiRefs BuildUi(out VRPokeButton startBtn, out VRPokeButton restartBtn)
    {
        var ui = new UiRefs();
        Color bg = new Color(0.10f, 0.065f, 0.045f, 0.96f);
        Color cream = new Color(0.97f, 0.92f, 0.84f);
        Color amber = new Color(1f, 0.72f, 0.28f);
        Transform c;

        // Intro
        ui.intro = MakePanel("IntroPanel", new Vector3(0, 1.62f, 0.85f), new Vector2(800, 450), bg, out c);
        MakeText("Title", c, "NIGHT SHIFT VINYL", 56, amber, new Vector2(0, 0.62f), new Vector2(1, 1), TextAlignmentOptions.Center, FontStyles.Bold);
        MakeText("Body", c, "Welcome to Night Shift Vinyl.\nPOKE buttons with one fingertip.\nPINCH (thumb + index) to grab and turn.", 36, cream, new Vector2(0, 0.08f), new Vector2(1, 0.64f), TextAlignmentOptions.Center);
        MakeText("Hint", c, "Poke START SESSION with one finger", 30, amber * 0.85f, new Vector2(0, 0), new Vector2(1, 0.16f), TextAlignmentOptions.Center, FontStyles.Italic);
        startBtn = MakeBigButton("StartButton", new Vector3(0, 1.30f, 0.55f), "START SESSION", new Color(0.30f, 0.80f, 0.55f), SimStep.MenuStart);
        startBtn.transform.SetParent(ui.intro.transform, true);

        // Checklist (always visible)
        ui.checklist = MakePanel("ChecklistPanel", new Vector3(-0.62f, 1.50f, 0.75f), new Vector2(620, 420), bg, out c);
        MakeText("Heading", c, "AUDIO SESSION", 44, amber, new Vector2(0, 0.76f), new Vector2(1, 1), TextAlignmentOptions.Center, FontStyles.Bold);
        ui.checklistText = MakeText("Checklist", c, "", 34, cream, new Vector2(0, 0), new Vector2(1, 0.78f), TextAlignmentOptions.TopLeft);
        ui.checklistText.richText = true;
        ui.checklistText.lineSpacing = 25f;

        // Completion
        ui.complete = MakePanel("CompletePanel", new Vector3(0, 1.62f, 0.85f), new Vector2(800, 450), bg, out c);
        MakeText("Title", c, "SESSION MASTERED\n- 100% GROOVE", 62, amber, new Vector2(0, 0.38f), new Vector2(1, 1), TextAlignmentOptions.Center, FontStyles.Bold);
        ui.timeText = MakeText("Time", c, "Session time 00:00.0", 40, cream, new Vector2(0, 0.0f), new Vector2(1, 0.4f), TextAlignmentOptions.Center);
        restartBtn = MakeBigButton("RestartButton", new Vector3(0, 1.30f, 0.55f), "RESTART SESSION", new Color(0.95f, 0.45f, 0.35f), SimStep.Complete);
        restartBtn.transform.SetParent(ui.complete.transform, true);
        ui.complete.SetActive(false);
        return ui;
    }

    // ------------------------------------------------------------------ manager + wiring

    static VinylSimulationManager BuildManager(AudioRefs audio, UiRefs ui, LightRefs lights)
    {
        var go = new GameObject("SimulationManager");
        var mgr = go.AddComponent<VinylSimulationManager>();
        SetRef(mgr, "introRoot", ui.intro);
        SetRef(mgr, "checklistRoot", ui.checklist);
        SetRef(mgr, "checklistText", ui.checklistText);
        SetRef(mgr, "completeRoot", ui.complete);
        SetRef(mgr, "completionTimeText", ui.timeText);
        SetRef(mgr, "chimeSource", audio.chime);
        SetRef(mgr, "stepChime", AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDir + "step_chime.wav"));
        SetRef(mgr, "completeChime", AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDir + "complete_chime.wav"));
        SetRef(mgr, "audioController", audio.controller);
        SetRef(mgr, "deskLamp", lights.lamp);
        Set(mgr, "lampDimIntensity", 0.6f);
        var so = new SerializedObject(mgr);
        var arr = so.FindProperty("neonLights");
        arr.arraySize = lights.neon.Length;
        for (int i = 0; i < lights.neon.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = lights.neon[i];
        so.ApplyModifiedProperties();
        return mgr;
    }

    static void Wire(VinylSimulationManager mgr, AudioRefs audio, VRSnapZone zone, PlatterSpinner spinner, VRTonearm tonearm,
                     VRPokeButton pad, VRFaderKnob knob, VRFaderKnob fader, VRPokeButton start, VRPokeButton restart)
    {
        UnityEventTools.AddVoidPersistentListener(zone.onSnapped, spinner.StartSpin);
        UnityEventTools.AddVoidPersistentListener(zone.onSnapped, audio.controller.PlayPlatterHum);
        UnityEventTools.AddVoidPersistentListener(tonearm.onNeedleDropped, audio.controller.PlayNeedleDrop);
        UnityEventTools.AddVoidPersistentListener(pad.onPressed, audio.controller.TriggerSynthPad);
        UnityEventTools.AddVoidPersistentListener(pad.onPressed, mgr.OnSamplePadPressed);
        UnityEventTools.AddVoidPersistentListener(start.onPressed, mgr.StartSession);
        UnityEventTools.AddVoidPersistentListener(restart.onPressed, mgr.ResetSimulation);
        SetRef(knob, "audioController", audio.controller);
        SetRef(fader, "audioController", audio.controller);
    }

    // ------------------------------------------------------------------ serialized-field helpers

    static SerializedProperty Prop(Object o, string field, out SerializedObject so)
    {
        so = new SerializedObject(o);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) Debug.LogError("CHILLHOP: missing field " + field + " on " + o.GetType().Name);
        return p;
    }

    static void SetRef(Object o, string f, Object v) { var p = Prop(o, f, out var so); if (p != null) { p.objectReferenceValue = v; so.ApplyModifiedProperties(); } }
    static void Set(Object o, string f, float v) { var p = Prop(o, f, out var so); if (p != null) { p.floatValue = v; so.ApplyModifiedProperties(); } }
    static void Set(Object o, string f, string v) { var p = Prop(o, f, out var so); if (p != null) { p.stringValue = v; so.ApplyModifiedProperties(); } }
    static void SetBool(Object o, string f, bool v) { var p = Prop(o, f, out var so); if (p != null) { p.boolValue = v; so.ApplyModifiedProperties(); } }
    static void SetEnum(Object o, string f, int v) { var p = Prop(o, f, out var so); if (p != null) { p.enumValueIndex = v; so.ApplyModifiedProperties(); } }
    static void SetVec3(Object o, string f, Vector3 v) { var p = Prop(o, f, out var so); if (p != null) { p.vector3Value = v; so.ApplyModifiedProperties(); } }
    static void SetColor(Object o, string f, Color v) { var p = Prop(o, f, out var so); if (p != null) { p.colorValue = v; so.ApplyModifiedProperties(); } }

    // ------------------------------------------------------------------ APK

    [MenuItem("Chillhop/Build APK")]
    public static void BuildApk()
    {
        Directory.CreateDirectory("Builds");
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        // Hand-tracking components live only in serialized prefabs; keep stripping minimal (see Assets/link.xml).
        PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.Android, ManagedStrippingLevel.Minimal);
        var opts = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Builds/nightshiftvinyl.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log("CHILLHOP BUILD: " + report.summary.result + " size=" + report.summary.totalSize + " errors=" + report.summary.totalErrors);
        EditorApplication.Exit(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
    }
}
