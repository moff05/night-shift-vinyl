using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Dresses the lounge: Blender-modelled furniture and props (Assets/Models/Chillhop), textured materials,
/// moonlight through the window, glow, particles and the audio-reactive bits.
/// Called from ChillhopBuilder.BuildScene (Environment first, Apply last).
/// </summary>
public static class ChillhopDecor
{
    const string Models = "Assets/Models/Chillhop/";
    const string Tex = "Assets/Textures/Chillhop/";
    const string MatDir = "Assets/Materials/Chillhop";

    static readonly Dictionary<string, Material> pal = new Dictionary<string, Material>();
    static Transform root;

    // ------------------------------------------------------------------ textures + materials

    static Texture2D T(string file, bool clamp = false, int aniso = 4, bool alpha = false, int maxSize = 1024)
    {
        string path = Tex + file;
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { Debug.LogWarning("CHILLHOP: missing texture " + file); return null; }
        bool dirty = false;
        var wm = clamp ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
        if (ti.wrapMode != wm) { ti.wrapMode = wm; dirty = true; }
        if (ti.anisoLevel != aniso) { ti.anisoLevel = aniso; dirty = true; }
        if (ti.alphaIsTransparency != alpha) { ti.alphaIsTransparency = alpha; dirty = true; }
        if (ti.maxTextureSize != maxSize) { ti.maxTextureSize = maxSize; dirty = true; }
        if (dirty) ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static Material Def(string name, Color c, float metal = 0f, float smooth = 0.3f, string tex = null, Vector2? tile = null,
                         Color? emit = null, bool cullOff = false, bool clamp = false)
    {
        string path = MatDir + "/" + name + ".mat";
        AssetDatabase.DeleteAsset(path);
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Metallic", metal);
        m.SetFloat("_Smoothness", smooth);
        if (tex != null)
        {
            m.SetTexture("_BaseMap", T(tex, clamp));
            m.SetTextureScale("_BaseMap", tile ?? Vector2.one);
        }
        if (emit.HasValue)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emit.Value);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        if (cullOff) m.SetFloat("_Cull", 0f);
        AssetDatabase.CreateAsset(m, path);
        pal[name] = m;
        return m;
    }

    // mode: 1 cutout, 2 alpha blend, 3 additive
    static Material Unlit(string name, string tex, Color color, int mode, bool particle = false, Vector2? tile = null)
    {
        string path = MatDir + "/" + name + ".mat";
        AssetDatabase.DeleteAsset(path);
        var m = new Material(Shader.Find(particle ? "Universal Render Pipeline/Particles/Unlit" : "Universal Render Pipeline/Unlit")) { name = name };
        if (tex != null)
        {
            m.SetTexture("_BaseMap", T(tex, true, 2, mode >= 2, 2048));
            m.SetTextureScale("_BaseMap", tile ?? Vector2.one);
        }
        m.SetColor("_BaseColor", color);
        if (mode == 1)
        {
            m.SetFloat("_AlphaClip", 1f);
            m.SetFloat("_Cutoff", 0.5f);
            m.EnableKeyword("_ALPHATEST_ON");
            m.renderQueue = 2450;
        }
        else if (mode >= 2)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_SrcBlend", 5f);
            m.SetFloat("_DstBlend", mode == 3 ? 1f : 10f);
            m.SetFloat("_SrcBlendAlpha", 1f);
            m.SetFloat("_DstBlendAlpha", mode == 3 ? 1f : 10f);
            m.SetFloat("_Blend", mode == 3 ? 2f : 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
            m.SetShaderPassEnabled("ShadowCaster", false);
        }
        AssetDatabase.CreateAsset(m, path);
        pal[name] = m;
        return m;
    }

    static Material P(string n)
    {
        if (pal.TryGetValue(n, out var m)) return m;
        Debug.LogWarning("CHILLHOP: no palette entry for " + n);
        return pal["M_Neutral"];
    }

    static Color C(float r, float g, float b, float a = 1f) { return new Color(r, g, b, a); }

    static void BuildPalette()
    {
        Def("M_Neutral", C(0.5f, 0.5f, 0.5f));
        Def("M_Art", Color.white);
        // floor + shell
        for (int i = 0; i < 4; i++) Def("M_Floor" + i, C(1.05f, 1.0f, 0.95f), 0f, 0.42f, "floor" + i + ".png", null, null, false, false);
        Def("M_FloorGap", C(0.02f, 0.014f, 0.01f));
        Def("M_Wall", C(0.33f, 0.40f, 0.58f), 0f, 0.08f, "plaster.png");
        Def("M_WallPanel", C(0.34f, 0.19f, 0.11f), 0f, 0.3f, "wood.png");
        Def("M_Trim", C(0.55f, 0.33f, 0.18f), 0f, 0.35f, "wood.png");
        Def("M_Ceiling", C(0.13f, 0.14f, 0.22f), 0f, 0.05f, "plaster.png");
        Def("M_Beam", C(0.30f, 0.17f, 0.095f), 0f, 0.3f, "wood.png");
        Def("M_WindowFrame", C(0.82f, 0.76f, 0.66f), 0f, 0.3f);
        // woods and metals
        Def("M_WoodLight", C(0.72f, 0.54f, 0.38f), 0f, 0.4f, "wood.png");
        Def("M_Wood", C(0.52f, 0.35f, 0.22f), 0f, 0.4f, "wood.png");
        Def("M_WoodDark", C(0.30f, 0.17f, 0.10f), 0f, 0.35f, "wood.png");
        Def("M_DeskTop", C(0.66f, 0.46f, 0.30f), 0f, 0.55f, "wood.png");
        Def("M_CrateWood", C(0.56f, 0.40f, 0.26f), 0f, 0.3f, "wood.png");
        Def("M_Door", C(0.45f, 0.25f, 0.14f), 0f, 0.4f, "wood.png");
        Def("M_Speaker", C(0.48f, 0.30f, 0.18f), 0f, 0.45f, "wood.png");
        Def("M_Brass", C(0.92f, 0.68f, 0.28f), 0.9f, 0.7f);
        Def("M_Steel", C(0.82f, 0.82f, 0.88f), 0.9f, 0.7f);
        Def("M_Black", C(0.035f, 0.035f, 0.04f), 0f, 0.4f);
        // soft furnishings
        Def("M_Curtain", C(0.50f, 0.15f, 0.40f), 0f, 0.1f, "fabric.png", new Vector2(2, 2));
        Def("M_Mustard", C(0.95f, 0.62f, 0.16f), 0f, 0.08f, "fabric.png", null);
        Def("M_Teal", C(0.10f, 0.55f, 0.58f), 0f, 0.08f, "fabric.png");
        Def("M_Cream", C(0.92f, 0.86f, 0.72f), 0f, 0.08f, "fabric.png");
        Def("M_Blanket", C(0.80f, 0.33f, 0.27f), 0f, 0.05f, "fabric.png", new Vector2(2, 2));
        Def("M_Fur", C(0.95f, 0.50f, 0.15f), 0f, 0.1f, "fabric.png");
        Def("M_FurWhite", C(0.95f, 0.93f, 0.88f), 0f, 0.1f, "fabric.png");
        Def("M_Pink", C(0.98f, 0.58f, 0.64f), 0f, 0.3f);
        Def("M_Rug", Color.white, 0f, 0.04f, "rug.png", null, null, false, true);
        Def("M_RugEdge", C(0.62f, 0.36f, 0.26f), 0f, 0.03f, "fabric.png");
        Def("M_DeskMat", C(0.18f, 0.24f, 0.42f), 0f, 0.2f, "felt.png", new Vector2(1.5f, 1.5f));
        Def("M_Felt", C(0.30f, 0.12f, 0.17f), 0f, 0.05f, "felt.png");
        Def("M_Grille", C(0.10f, 0.10f, 0.11f), 0f, 0.1f, "fabric.png");
        Def("M_Cone", C(0.17f, 0.17f, 0.18f), 0f, 0.25f);
        // plants
        Def("M_Leaf", C(0.16f, 0.58f, 0.24f), 0f, 0.5f);
        Def("M_LeafDark", C(0.08f, 0.34f, 0.16f), 0f, 0.4f);
        Def("M_Snake", C(0.12f, 0.40f, 0.24f), 0f, 0.5f);
        Def("M_Cactus", C(0.25f, 0.58f, 0.40f), 0f, 0.3f);
        Def("M_Pot", C(0.85f, 0.43f, 0.25f), 0f, 0.2f);
        Def("M_PotWhite", C(0.92f, 0.92f, 0.88f), 0f, 0.5f);
        Def("M_Soil", C(0.07f, 0.045f, 0.03f), 0f, 0.05f);
        // glass, ceramics, paper
        Def("M_Ceramic", C(0.95f, 0.90f, 0.80f), 0f, 0.75f);
        Def("M_Coffee", C(0.12f, 0.06f, 0.03f), 0f, 0.9f);
        Def("M_Paper", C(0.94f, 0.92f, 0.85f), 0f, 0.1f);
        Def("M_Notebook", C(0.78f, 0.36f, 0.25f), 0f, 0.25f);
        Def("M_Label", C(0.88f, 0.84f, 0.72f), 0f, 0.2f);
        Def("M_ClockFace", C(0.95f, 0.92f, 0.84f), 0f, 0.3f);
        Def("M_FrameBlack", C(0.03f, 0.03f, 0.035f), 0f, 0.4f);
        Def("M_FrameWood", C(0.62f, 0.37f, 0.18f), 0f, 0.4f, "wood.png");
        Def("M_FrameMat", C(0.94f, 0.92f, 0.86f), 0f, 0.05f);
        Def("M_Pendant", C(0.10f, 0.46f, 0.44f), 0.3f, 0.65f);
        // books, sleeves, cassettes
        Color[] book = { C(0.55f, 0.17f, 0.17f), C(0.17f, 0.34f, 0.52f), C(0.78f, 0.62f, 0.18f), C(0.22f, 0.45f, 0.28f), C(0.50f, 0.28f, 0.45f), C(0.88f, 0.46f, 0.22f), C(0.28f, 0.28f, 0.34f), C(0.66f, 0.64f, 0.54f) };
        for (int i = 0; i < 8; i++) Def("M_Book" + i, book[i], 0f, 0.2f, "fabric.png");
        Color[] sleeve = { C(0.95f, 0.42f, 0.55f), C(0.35f, 0.78f, 0.90f), C(0.98f, 0.84f, 0.34f), C(0.56f, 0.45f, 0.92f), C(0.35f, 0.84f, 0.56f), C(0.96f, 0.58f, 0.28f) };
        for (int i = 0; i < 6; i++) Def("M_Sleeve" + i, sleeve[i], 0f, 0.15f);
        Color[] cass = { C(0.85f, 0.32f, 0.32f), C(0.32f, 0.55f, 0.88f), C(0.95f, 0.80f, 0.25f) };
        for (int i = 0; i < 3; i++) Def("M_Cassette" + i, cass[i], 0f, 0.4f);
        // deck + mixer
        Def("M_Plinth", C(0.07f, 0.065f, 0.065f), 0f, 0.35f);
        Def("M_PlinthTop", C(0.12f, 0.11f, 0.11f), 0f, 0.5f);
        Def("M_Platter", C(0.70f, 0.70f, 0.74f), 0.9f, 0.7f);
        Def("M_Mat", C(0.05f, 0.05f, 0.055f), 0f, 0.25f);
        Def("M_Dot", C(0.95f, 0.95f, 0.90f), 0f, 0.3f, null, null, C(0.5f, 0.5f, 0.45f));
        Def("M_MixerBody", C(0.15f, 0.15f, 0.17f), 0.4f, 0.55f);
        Def("M_MixerTop", C(0.23f, 0.23f, 0.27f), 0.3f, 0.5f);
        Def("M_Vinyl", Color.white, 0.25f, 0.82f, "vinyl.png", null, null, false, true);
        Def("M_VinylLabel", Color.white, 0f, 0.35f, "vinyl_label.png", null, null, false, true);
        // light emitters
        Def("M_Glow", C(1f, 0.82f, 0.5f), 0f, 0.5f, null, null, C(3.2f, 2.0f, 0.8f));
        Def("M_LEDRed", C(1f, 0.1f, 0.05f), 0f, 0.5f, null, null, C(3f, 0.2f, 0.1f));
        Def("M_LEDGreen", C(0.1f, 1f, 0.3f), 0f, 0.5f, null, null, C(0.2f, 2.6f, 0.7f));
        Def("M_LEDAmber", C(1f, 0.7f, 0.1f), 0f, 0.5f, null, null, C(3f, 1.8f, 0.2f));
        Def("M_NeonPink", C(1f, 0.25f, 0.65f), 0f, 0.5f, null, null, C(3.0f, 0.5f, 1.6f));
        Def("M_NeonCyan", C(0.25f, 0.9f, 1f), 0f, 0.5f, null, null, C(0.5f, 2.4f, 3f));
        Def("M_NeonYellow", C(1f, 0.85f, 0.3f), 0f, 0.5f, null, null, C(3f, 2.3f, 0.6f));
        Def("M_Wire", C(0.03f, 0.03f, 0.03f), 0f, 0.3f);
        Def("M_Bulb0", C(1f, 0.8f, 0.45f), 0f, 0.6f, null, null, C(3f, 1.9f, 0.8f));
        Def("M_Bulb1", C(1f, 0.55f, 0.7f), 0f, 0.6f, null, null, C(3f, 0.9f, 1.4f));
    }

    // ------------------------------------------------------------------ placement helpers

    static GameObject Place(string model, Vector3 pos, float yaw = 0f, Transform parent = null, bool isStatic = true, bool shadows = true, string name = null)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Models + model + ".fbx");
        if (prefab == null) { Debug.LogError("CHILLHOP: missing model " + model); return null; }
        var go = Object.Instantiate(prefab);
        go.name = name ?? "Deco_" + model;
        go.transform.SetParent(parent != null ? parent : root, false);
        if (parent == null) go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        else { go.transform.localPosition = pos; go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f); }
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = P(mats[i] != null ? mats[i].name : "M_Neutral");
            r.sharedMaterials = mats;
            r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        }
        if (isStatic)
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
        return go;
    }

    static readonly Dictionary<string, Material> artMats = new Dictionary<string, Material>();

    static Material ArtMat(string name, string tex)
    {
        if (artMats.TryGetValue(name, out var cached) && cached != null) return cached;
        string path = MatDir + "/Art_" + name + ".mat";
        AssetDatabase.DeleteAsset(path);
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Art_" + name };
        m.SetTexture("_BaseMap", T(tex, true));
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Smoothness", 0.35f);
        AssetDatabase.CreateAsset(m, path);
        artMats[name] = m;
        return m;
    }

    static GameObject Frame(string model, string art, Vector3 pos, float yaw)
    {
        var g = Place(model, pos, yaw, null, true, false, "Poster_" + art.Replace(".png", ""));
        var mat = ArtMat(art.Replace(".png", ""), art);
        foreach (Renderer r in g.GetComponentsInChildren<Renderer>())
        {
            var ms = r.sharedMaterials;
            ms[ms.Length - 1] = mat;               // last slot of every frame model is the art quad
            r.sharedMaterials = ms;
        }
        return g;
    }

    static void Hide(string name)
    {
        var g = GameObject.Find(name);
        if (g == null) { Debug.LogWarning("CHILLHOP: nothing named " + name + " to hide"); return; }
        foreach (Renderer r in g.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
    }

    static GameObject Find(string name)
    {
        var g = GameObject.Find(name);
        if (g == null) Debug.LogWarning("CHILLHOP: cannot find " + name);
        return g;
    }

    static GameObject Halo(string name, Vector3 pos, float size, Color color, Transform parent = null)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = name;
        Object.DestroyImmediate(q.GetComponent<Collider>());
        q.transform.SetParent(parent != null ? parent : root, false);
        q.transform.position = pos;
        q.transform.localScale = Vector3.one * size;
        var m = Unlit("Halo_" + name, "halo.png", color, 3);
        var r = q.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        q.AddComponent<FaceCamera>();
        return q;
    }

    static Light PointLight(string name, Vector3 pos, Color color, float intensity, float range)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.position = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None;
        return l;
    }

    static void SetSerialized(Object o, string field, float v)
    {
        var so = new SerializedObject(o);
        var p = so.FindProperty(field);
        if (p != null) { p.floatValue = v; so.ApplyModifiedProperties(); }
    }

    // ------------------------------------------------------------------ environment

    public static void Environment()
    {
        AssetDatabase.Refresh();
        artMats.Clear();
        pal.Clear();
        BuildPalette();

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = C(0.26f, 0.30f, 0.50f);
        RenderSettings.ambientEquatorColor = C(0.30f, 0.22f, 0.25f);
        RenderSettings.ambientGroundColor = C(0.13f, 0.09f, 0.08f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = C(0.06f, 0.05f, 0.11f);
        RenderSettings.fogDensity = 0.018f;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        RenderSettings.customReflectionTexture = MakeEnvCube();
        RenderSettings.reflectionIntensity = 1f;

        // cheaper shadows: one hard cascade, short distance
        foreach (string p in new[] { "Assets/Settings/Mobile_RPAsset.asset", "Assets/Settings/PC_RPAsset.asset" })
        {
            var a = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(p);
            if (a == null) continue;
            a.shadowDistance = 9f;
            a.shadowCascadeCount = 1;
            EditorUtility.SetDirty(a);
        }

        var moon = new GameObject("Moonlight").AddComponent<Light>();
        moon.type = LightType.Directional;
        moon.color = C(0.55f, 0.66f, 1f);
        moon.intensity = 0.8f;
        moon.shadows = LightShadows.Hard;
        moon.shadowStrength = 0.85f;
        moon.shadowBias = 0.03f;
        moon.shadowNormalBias = 0.3f;
        moon.transform.rotation = Quaternion.Euler(33f, 196f, 0f);
    }

    static Cubemap MakeEnvCube()
    {
        const int S = 32;
        string path = Tex + "RoomEnv.cubemap";
        AssetDatabase.DeleteAsset(path);
        var cube = new Cubemap(S, TextureFormat.RGBA32, false);
        System.Func<Vector3, Color> env = d =>
        {
            float y = d.y;
            Color c = y > 0 ? Color.Lerp(C(0.10f, 0.08f, 0.10f), C(0.05f, 0.05f, 0.12f), y) : Color.Lerp(C(0.10f, 0.08f, 0.07f), C(0.14f, 0.08f, 0.04f), -y);
            System.Action<Vector3, Color, float> blob = (dir, col, k) => c += col * Mathf.Pow(Mathf.Max(0f, Vector3.Dot(d, dir.normalized)), k);
            blob(new Vector3(0f, 0.15f, 1f), C(0.45f, 0.58f, 0.95f), 9f);    // window
            blob(new Vector3(-0.8f, 0.35f, 0.5f), C(1f, 0.62f, 0.25f), 14f);  // desk lamp side
            blob(new Vector3(1f, 0.2f, 0.4f), C(0.95f, 0.25f, 0.65f), 12f);   // neon
            blob(new Vector3(0f, 1f, 0.3f), C(0.9f, 0.7f, 0.4f), 20f);         // pendants
            return c;
        };
        CubemapFace[] faces = { CubemapFace.PositiveX, CubemapFace.NegativeX, CubemapFace.PositiveY, CubemapFace.NegativeY, CubemapFace.PositiveZ, CubemapFace.NegativeZ };
        foreach (CubemapFace f in faces)
        {
            var px = new Color[S * S];
            for (int j = 0; j < S; j++)
                for (int i = 0; i < S; i++)
                {
                    float u = (i + 0.5f) / S * 2f - 1f, v = (j + 0.5f) / S * 2f - 1f;
                    Vector3 d = Vector3.forward;
                    switch (f)
                    {
                        case CubemapFace.PositiveX: d = new Vector3(1, -v, -u); break;
                        case CubemapFace.NegativeX: d = new Vector3(-1, -v, u); break;
                        case CubemapFace.PositiveY: d = new Vector3(u, 1, v); break;
                        case CubemapFace.NegativeY: d = new Vector3(u, -1, -v); break;
                        case CubemapFace.PositiveZ: d = new Vector3(u, -v, 1); break;
                        case CubemapFace.NegativeZ: d = new Vector3(-u, -v, -1); break;
                    }
                    px[j * S + i] = env(d.normalized);
                }
            cube.SetPixels(px, f);
        }
        cube.Apply();
        AssetDatabase.CreateAsset(cube, path);
        return cube;
    }

    // ------------------------------------------------------------------ the dressing

    public static void Apply()
    {
        root = new GameObject("Decor").transform;

        // ---- retire the placeholder room visuals but keep their colliders / use them as light blockers
        foreach (string w in new[] { "FrontWall", "LeftWall", "RightWall", "Ceiling", "Floor" })
        {
            var g = Find(w);
            if (g == null) continue;
            var r = g.GetComponent<Renderer>();
            if (w == "Floor") r.enabled = false; else r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }
        var back = Find("BackWall");
        if (back != null) Object.DestroyImmediate(back);
        BackBlocker("BackLeft", -2.2f, 1.8f, 1.6f, 3.4f);          // x centre, width, y centre, height (window: x +-1.3, y 1.0..2.5)
        BackBlocker("BackRight", 2.2f, 1.8f, 1.6f, 3.4f);
        BackBlocker("BackTop", 0f, 2.6f, 2.85f, 0.7f);
        BackBlocker("BackBottom", 0f, 2.6f, 0.45f, 1.1f);
        foreach (string n in new[] { "DeskBody", "DeskTop" }) Hide(n);

        // ---- shell, window, rug
        Place("RoomShell", Vector3.zero);
        Place("Window", Vector3.zero);
        Place("Rug", new Vector3(0f, 0.001f, 0.35f));
        Place("Desk", new Vector3(0f, 0f, 0.65f));
        Place("DeskMat", new Vector3(0.14f, 0.95f, 0.67f));

        // ---- the deck: swap the primitive visuals for the modelled ones
        Hide("TurntableBase");
        Place("Turntable", new Vector3(-0.37f, 0.95f, 0.67f));
        var platter = Find("Platter");
        if (platter != null)
        {
            Hide("PlatterMesh");
            Place("PlatterModel", Vector3.zero, 0f, platter.transform, false, false, "Deco_PlatterModel");
        }
        Hide("ArmBase");
        Place("ArmBase", new Vector3(-0.12f, 1.01f, 0.82f));
        var pivot = Find("ArmPivot");
        if (pivot != null)
        {
            foreach (string n in new[] { "ArmTube", "Headshell", "Needle", "Counterweight" }) Hide(n);
            Place("TonearmModel", Vector3.zero, 0f, pivot.transform, false, false, "Deco_Tonearm");
        }
        var mixer = Find("Mixer");
        if (mixer != null)
        {
            Hide("MixerBody"); Hide("FaderBack");
            Place("MixerModel", Vector3.zero, 0f, mixer.transform, true, false, "Deco_Mixer");
            BuildVu(mixer.transform);
        }
        foreach (string n in new[] { "CrateBase", "CrateBack", "CrateSideL", "CrateSideR" }) Hide(n);
        Place("CrateModel", new Vector3(0.78f, 0.95f, 0.62f));
        var vinyl = Find("VinylRecord");
        if (vinyl != null)
        {
            Hide("Disc"); Hide("Label");
            Place("RecordModel", Vector3.zero, 0f, vinyl.transform, false, false, "Deco_Record");
        }

        // ---- lamp
        var lampRig = GameObject.Find("LampVisual");
        if (lampRig != null) Object.DestroyImmediate(lampRig);
        var lamp = Place("DeskLamp", new Vector3(-0.98f, 0.95f, 0.30f), 0f, null, false);
        var lampLight = Find("DeskLampLight");      // the Light object made by BuildLights (name differs from "Deco_DeskLamp")
        var bulb = lamp.transform.Find("Bulb");
        if (lampLight != null && bulb != null)
        {
            lampLight.transform.position = bulb.position;
            lampLight.transform.rotation = Quaternion.LookRotation(new Vector3(-0.3f, 0.98f, 0.72f) - bulb.position);
            var l = lampLight.GetComponent<Light>();
            l.intensity = 5.5f; l.range = 3.6f; l.spotAngle = 100f; l.innerSpotAngle = 45f;
            l.color = C(1f, 0.70f, 0.36f);
            Halo("LampHalo", bulb.position + new Vector3(0.0f, 0.0f, -0.02f), 0.28f, C(1f, 0.7f, 0.35f, 1f) * 0.55f);
        }

        // ---- ceiling pendants + string lights
        foreach (float sx in new[] { -1f, 1f })
        {
            var pend = Place("Pendant", new Vector3(sx * 1.25f, 3.1f, 1.45f), 0f, null, false, false, "Deco_Pendant" + (sx < 0 ? "L" : "R"));
            var pb = pend.transform.Find("Bulb");
            Vector3 bp = pb != null ? pb.position : pend.transform.position + Vector3.down * 1.28f;
            PointLight("PendantLight" + (sx < 0 ? "L" : "R"), bp + Vector3.down * 0.1f, C(1f, 0.74f, 0.42f), 1.7f, 4.2f);
            Halo("PendantHalo" + (sx < 0 ? "L" : "R"), bp, 0.6f, C(1f, 0.72f, 0.4f) * 0.55f);
        }
        var strings = Place("StringLights", Vector3.zero, 0f, null, false, false);
        var tw = strings.AddComponent<Twinkle>();
        tw.slots = new[] { 1, 2 };
        tw.colors = new[] { C(3f, 1.9f, 0.8f), C(3f, 0.9f, 1.4f) };

        // ---- furniture
        var couch = Place("Couch", new Vector3(-2.5f, 0f, 1.1f), -90f);
        // Centre cushion, curled along the seat between the two throw pillows (they sit at x -0.55 and +0.62), clear of the back cushions.
        Vector3 seat = couch.transform.TransformPoint(new Vector3(0.04f, 0.492f, -0.14f));
        var cat = Place("Cat", seat, 0f, null, false);
        cat.transform.localScale = Vector3.one * 0.8f;
        var br = cat.AddComponent<Breathe>(); br.amount = 0.035f; br.rate = 1.7f;
        Place("SideTable", new Vector3(-2.55f, 0f, 2.42f));
        Place("Cactus", new Vector3(-2.55f, 0.535f, 2.42f), 30f);
        Place("Bookshelf", new Vector3(-2.38f, 0f, 2.83f));
        Place("VinylRack", new Vector3(2.35f, 0f, 2.80f));
        foreach (float sx in new[] { -1f, 1f })
        {
            var sp = Place("Speaker", new Vector3(sx * 1.55f, 0f, 1.85f), sx * 40f, null, true, true, "Deco_Speaker" + (sx < 0 ? "L" : "R"));
            var wf = sp.transform.Find("Woofer");
            if (wf != null) { wf.gameObject.AddComponent<WooferPump>(); GameObjectUtility.SetStaticEditorFlags(wf.gameObject, 0); }
        }

        // ---- plants
        Place("Monstera", new Vector3(2.55f, 0f, 1.35f), 20f);
        Place("Monstera", new Vector3(-2.7f, 0f, -1.65f), 140f);
        Place("SnakePlant", new Vector3(0.8f, 0f, 2.55f));
        Place("Pothos", new Vector3(-2.45f, 3.1f, 0.35f), 0f, null, false);
        Place("Succulent", new Vector3(0.74f, 1.05f, 2.88f));
        Place("Cactus", new Vector3(-0.95f, 1.05f, 2.88f), 200f, null, true, true, "Deco_CactusSill");

        // ---- walls: posters, clock, neon, shelves, door
        Frame("Frame_P_Black", "poster0.png", new Vector3(-2.985f, 1.75f, 0.45f), -90f);
        Frame("Frame_L_Wood", "poster1.png", new Vector3(-2.985f, 1.7f, 1.2f), -90f);
        Frame("Frame_S_Black", "poster2.png", new Vector3(-2.985f, 1.72f, 1.85f), -90f);
        Frame("Frame_P_Wood", "poster3.png", new Vector3(-2.38f, 2.55f, 2.985f), 0f);
        Frame("Frame_W_Black", "poster4.png", new Vector3(2.35f, 2.45f, 2.985f), 0f);
        Frame("Frame_W_Black", "poster4.png", new Vector3(-1.2f, 1.7f, -1.985f), 180f);
        Frame("Frame_P_Black", "poster3.png", new Vector3(-0.2f, 1.7f, -1.985f), 180f);
        Frame("Frame_S_Black", "poster0.png", new Vector3(0.55f, 1.65f, -1.985f), 180f);
        Place("Door", new Vector3(1.9f, 0f, -1.97f), 180f);
        Place("Clock", new Vector3(2.97f, 2.45f, 2.0f), 90f);
        Place("WallShelf", new Vector3(2.35f, 1.3f, 2.88f));
        Place("WallShelf", new Vector3(2.35f, 1.75f, 2.88f));

        var neon = Place("NeonSign", new Vector3(2.94f, 1.95f, 0.85f), 90f, null, false, false);
        var np = neon.AddComponent<NeonPulse>();
        np.slots = new[] { 0, 1, 2 };
        np.colors = new[] { C(3.0f, 0.5f, 1.6f), C(3f, 2.3f, 0.6f), C(0.5f, 2.4f, 3f) };
        var hPink = Halo("NeonHaloPink", new Vector3(2.9f, 1.95f, 0.78f), 1.1f, C(1f, 0.25f, 0.6f) * 0.5f);
        var hCyan = Halo("NeonHaloCyan", new Vector3(2.9f, 1.83f, 1.55f), 0.7f, C(0.2f, 0.85f, 1f) * 0.45f);
        np.halos = new[] { hPink.transform, hCyan.transform };

        // ---- desk props
        Place("Mug", new Vector3(0.66f, 0.95f, 1.0f), 25f);
        var steam = new GameObject("Steam");
        steam.transform.SetParent(root, false);
        steam.transform.position = new Vector3(0.66f, 1.05f, 1.0f);
        MakeSteam(steam);
        Place("Headphones", new Vector3(-0.86f, 0.95f, 0.98f), 15f);
        Place("Cassettes", new Vector3(-0.24f, 0.95f, 1.03f), 8f);
        Place("Notebook", new Vector3(-0.86f, 0.95f, 0.62f), 20f);
        Place("Succulent", new Vector3(0.12f, 0.95f, 1.03f), 0f, null, true, true, "Deco_SucculentDesk");

        // ---- window: skyline, parallax layers, rain, moon glow
        var farM = Unlit("Sky_Far", "skyline_far.png", Color.white, 0);
        var nearM = Unlit("Sky_Near", "skyline_near.png", Color.white, 1);
        SkyQuad("SkylineFar", new Vector3(0f, 3.0f, 9f), new Vector2(14f, 7f), farM);
        SkyQuad("SkylineNear", new Vector3(0f, 1.95f, 5.2f), new Vector2(9f, 4.5f), nearM);
        var rainA = Unlit("Rain_A", "rain.png", C(0.7f, 0.82f, 1f, 0.5f), 3, false, new Vector2(1f, 1.6f));
        var rainB = Unlit("Rain_B", "rain.png", C(0.7f, 0.82f, 1f, 0.35f), 3, false, new Vector2(1.7f, 2.6f));
        var ra = SkyQuad("RainA", new Vector3(0f, 1.75f, 3.165f), new Vector2(2.55f, 1.48f), rainA);
        ra.AddComponent<ScrollUV>().speed = new Vector2(0f, -0.75f);
        var rb = SkyQuad("RainB", new Vector3(0f, 1.75f, 3.15f), new Vector2(2.55f, 1.48f), rainB);
        rb.AddComponent<ScrollUV>().speed = new Vector2(0.02f, -1.1f);

        // ---- neon colour lights (driven by the manager at the finale): park them where the colours should spill
        Reposition("NeonLight0", new Vector3(2.35f, 1.95f, 0.95f));
        Reposition("NeonLight1", new Vector3(-2.2f, 2.3f, 1.4f));
        Reposition("NeonLight2", new Vector3(0f, 2.7f, 2.4f));
        var fill = GameObject.Find("FillLight");
        if (fill != null) Object.DestroyImmediate(fill);
        foreach (string n in new[] { "NeonBar0", "NeonBar1", "NeonBar2" }) { var g = GameObject.Find(n); if (g != null) Object.DestroyImmediate(g); }

        // ---- particles
        MakeDust();

        // ---- runtime glue
        new GameObject("AudioLevels").AddComponent<AudioLevels>();
        var mgr = Object.FindFirstObjectByType<VinylSimulationManager>();
        if (mgr != null)
        {
            SetSerialized(mgr, "neonIdleIntensity", 0.7f);
            SetSerialized(mgr, "neonPulseIntensity", 2.4f);
            SetSerialized(mgr, "lampDimIntensity", 2.2f);
            var cp = GameObject.Find("CompletePanel");
            if (cp != null) { var sd = mgr.gameObject.AddComponent<SlateDock>(); sd.slate = cp.transform; }
        }
        Guides();
    }

    // ---- "touch this next" outline: a pulsing rim around whichever control the current step needs
    static Material outlineMat;

    static void Guides()
    {
        outlineMat = Unlit("Outline_Guide", null, Color.white, 0);
        outlineMat.SetFloat("_Cull", 1f);                      // cull front faces: only the grown back shell shows, as a rim
        EditorUtility.SetDirty(outlineMat);
        Color amber = new Color(1f, 0.72f, 0.28f), green = new Color(0.35f, 1f, 0.6f), coral = new Color(1f, 0.5f, 0.4f), cyan = new Color(0.4f, 0.9f, 1f);
        foreach (var b in Object.FindObjectsByType<VRPokeButton>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (b.name == "StartButton") Guide(b.transform, SimStep.MenuStart, green, 0.007f, "ButtonBase");
            else if (b.name == "RestartButton") Guide(b.transform, SimStep.Complete, coral, 0.007f, "ButtonBase");
            else if (b.name == "PadButton") Guide(b.transform, SimStep.Step3_TriggerSamplePad, amber, 0.005f, "PadCap");
        }
        var vinyl = Find("VinylRecord");
        if (vinyl != null) Guide(vinyl.transform, SimStep.Step1_PlaceVinyl, amber, 0.006f, null);
        var snap = Find("SnapPoint");
        if (snap != null)
        {
            var g = Guide(snap.transform, SimStep.Step1_PlaceVinyl, cyan, 0.005f, "none");
            g.ringRadius = 0.165f; g.ringWidth = 0.006f;
        }
        var pivot = Find("ArmPivot");
        if (pivot != null && pivot.transform.Find("Handle") != null) Guide(pivot.transform.Find("Handle"), SimStep.Step2_DropNeedle, amber, 0.006f, "HandleKnob");
        var knob = Find("FilterKnob");
        if (knob != null) Guide(knob.transform, SimStep.Step4_FilterKnob, amber, 0.005f, "KnobBody");
        var fader = Find("FaderHandle");
        if (fader != null) Guide(fader.transform, SimStep.Step5_MasterFader, amber, 0.004f, "FaderCapMesh");
    }

    static StepGlow Guide(Transform target, SimStep step, Color color, float thickness, string childName)
    {
        var list = new System.Collections.Generic.List<Renderer>();
        foreach (var r in target.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (!r.enabled || r.GetComponent<MeshFilter>() == null) continue;
            if (childName != null && r.name != childName) continue;
            list.Add(r);
        }
        var g = target.gameObject.AddComponent<StepGlow>();
        g.step = step;
        g.sources = list.ToArray();
        g.outlineMaterial = outlineMat;
        g.thickness = thickness;
        g.color = color;
        return g;
    }

    static void BackBlocker(string name, float x, float w, float y, float h)
    {
        var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.name = name;
        Object.DestroyImmediate(c.GetComponent<Collider>());
        c.transform.SetParent(root, false);
        c.transform.position = new Vector3(x, y, 3.1f);
        c.transform.localScale = new Vector3(w, h, 0.2f);
        c.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.ShadowsOnly;
    }

    static GameObject SkyQuad(string name, Vector3 pos, Vector2 size, Material m)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = name;
        Object.DestroyImmediate(q.GetComponent<Collider>());
        q.transform.SetParent(root, false);
        q.transform.position = pos;
        q.transform.localScale = new Vector3(size.x, size.y, 1f);
        var r = q.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        return q;
    }

    static void Reposition(string name, Vector3 pos)
    {
        var g = GameObject.Find(name);
        if (g != null) g.transform.position = pos;
    }

    static void BuildVu(Transform mixer)
    {
        var bars = new Transform[8];
        for (int i = 0; i < 8; i++)
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.name = "VuBar" + i;
            Object.DestroyImmediate(c.GetComponent<Collider>());
            c.transform.SetParent(mixer, false);
            c.transform.localPosition = new Vector3(-0.098f + i * 0.028f, 0.066f, 0.04f);
            c.transform.localScale = new Vector3(0.018f, 0.004f, 0.034f);
            string mat = i < 5 ? "M_LEDGreen" : (i < 7 ? "M_LEDAmber" : "M_LEDRed");
            var r = c.GetComponent<Renderer>();
            r.sharedMaterial = P(mat);
            r.shadowCastingMode = ShadowCastingMode.Off;
            bars[i] = c.transform;
        }
        var vu = mixer.gameObject.AddComponent<VuMeter>();
        vu.bars = bars;
    }

    static void MakeSteam(GameObject go)
    {
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.startLifetime = 2.4f;
        main.startSpeed = 0.05f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.035f);
        main.startColor = C(1f, 0.95f, 0.9f, 0.16f);
        main.maxParticles = 30;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission; em.rateOverTime = 7f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 6f; sh.radius = 0.012f;
        sh.rotation = new Vector3(-90f, 0f, 0f);
        var sz = ps.sizeOverLifetime; sz.enabled = true;
        sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.5f), new Keyframe(1, 2.6f)));
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var noise = ps.noise; noise.enabled = true; noise.strength = 0.03f; noise.frequency = 0.8f;
        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.sharedMaterial = Unlit("Particle_Steam", "puff.png", Color.white, 2, true);
        rend.shadowCastingMode = ShadowCastingMode.Off;
    }

    static void MakeDust()
    {
        var go = new GameObject("DustMotes");
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(0f, 1.7f, 1.2f);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true; main.prewarm = true;
        main.startLifetime = 16f;
        main.startSpeed = 0.01f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.02f);
        main.startColor = C(1f, 0.9f, 0.7f, 0.55f);
        main.maxParticles = 90;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission; em.rateOverTime = 6f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(4.2f, 2.2f, 3.6f);
        var noise = ps.noise; noise.enabled = true; noise.strength = 0.05f; noise.frequency = 0.25f; noise.scrollSpeed = 0.1f;
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.sharedMaterial = Unlit("Particle_Dust", "halo.png", Color.white, 3, true);
        rend.shadowCastingMode = ShadowCastingMode.Off;
    }
}
