using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Draws a soft pulsing outline around a control while its step is the active one, so the player always sees what to
/// touch next. The outline is an inverted-hull copy of the control's own mesh (front faces culled, grown a few mm),
/// so it hugs the silhouette instead of glowing from the centre. Optionally also draws a flat ring (the platter target).
/// </summary>
public class StepGlow : MonoBehaviour
{
    public SimStep step;
    public Renderer[] sources;              // meshes to outline
    public Material outlineMaterial;        // unlit, Cull Front
    public float thickness = 0.005f;        // metres of rim
    public Color color = new Color(1f, 0.75f, 0.3f);
    public float pulseRate = 2.2f;
    [Range(0f, 1f)] public float pulseDepth = 0.45f;
    public float intensity = 0.8f;

    [Header("Optional flat ring around this transform (e.g. the platter target)")]
    public float ringRadius;
    public float ringWidth = 0.008f;

    readonly List<GameObject> shells = new List<GameObject>();
    Material mat;
    float fade;

    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    void Awake()
    {
        if (outlineMaterial == null) return;
        mat = new Material(outlineMaterial);
        if (sources != null)
        {
            foreach (Renderer r in sources)
            {
                var mf = r != null ? r.GetComponent<MeshFilter>() : null;
                if (mf == null || mf.sharedMesh == null) continue;
                var go = new GameObject("Outline");
                go.transform.SetParent(r.transform, false);
                Vector3 size = mf.sharedMesh.bounds.size, ls = r.transform.lossyScale;
                var s = Vector3.one;
                for (int i = 0; i < 3; i++)
                {
                    float world = Mathf.Max(Mathf.Abs(size[i] * ls[i]), 1e-4f);
                    s[i] = Mathf.Min(1f + 2f * thickness / world, 3f);
                }
                go.transform.localScale = s;
                go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                shells.Add(go);
            }
        }
        if (ringRadius > 0f)
        {
            var go = new GameObject("OutlineRing");
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = BuildRing(ringRadius, ringWidth);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            shells.Add(go);
        }
        SetShells(false);
    }

    void OnDestroy()
    {
        if (mat != null) Destroy(mat);
    }

    void SetShells(bool on)
    {
        foreach (GameObject g in shells) if (g != null && g.activeSelf != on) g.SetActive(on);
    }

    void LateUpdate()
    {
        if (mat == null) return;
        var mgr = VinylSimulationManager.Instance;
        bool on = mgr != null && mgr.CurrentStep == step;
        fade = Mathf.MoveTowards(fade, on ? 1f : 0f, Time.deltaTime * 3f);
        if (fade <= 0f) { SetShells(false); return; }
        SetShells(true);
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseRate);
        mat.SetColor(BaseColor, color * (fade * intensity * (1f - pulseDepth + pulseDepth * pulse)));
    }

    // flat annulus, wound both ways so it shows whichever way the cull setting faces
    static Mesh BuildRing(float radius, float width)
    {
        const int seg = 72;
        var v = new Vector3[seg * 2];
        for (int i = 0; i < seg; i++)
        {
            float a = Mathf.PI * 2f * i / seg;
            var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            v[i * 2] = d * (radius - width * 0.5f);
            v[i * 2 + 1] = d * (radius + width * 0.5f);
        }
        var t = new List<int>();
        for (int i = 0; i < seg; i++)
        {
            int n = (i + 1) % seg;
            int a0 = i * 2, a1 = i * 2 + 1, b0 = n * 2, b1 = n * 2 + 1;
            t.AddRange(new[] { a0, a1, b0, a1, b1, b0 });
            t.AddRange(new[] { a0, b0, a1, a1, b0, b1 });
        }
        var m = new Mesh { name = "OutlineRing", vertices = v };
        m.SetTriangles(t, 0);
        m.RecalculateBounds();
        return m;
    }
}
