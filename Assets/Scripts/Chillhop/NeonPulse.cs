using UnityEngine;

/// <summary>Neon tubes and their halo glow with the music.</summary>
public class NeonPulse : MonoBehaviour
{
    public int[] slots;
    public Color[] colors;
    public float baseLevel = 0.8f, reactive = 0.9f;
    public Transform[] halos;
    Material[] mats;
    Vector3[] haloScale;

    void Awake()
    {
        mats = GetComponent<Renderer>().materials;
        haloScale = new Vector3[halos.Length];
        for (int i = 0; i < halos.Length; i++) haloScale[i] = halos[i].localScale;
    }

    void Update()
    {
        float k = baseLevel + reactive * AudioLevels.Bass + 0.06f * Mathf.Sin(Time.time * 7f);
        for (int i = 0; i < slots.Length; i++) mats[slots[i]].SetColor("_EmissionColor", colors[i] * k);
        for (int i = 0; i < halos.Length; i++) halos[i].localScale = haloScale[i] * (0.92f + 0.3f * AudioLevels.Bass);
    }
}
