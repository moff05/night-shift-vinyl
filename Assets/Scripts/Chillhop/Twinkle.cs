using UnityEngine;

/// <summary>Fairy lights: two interleaved bulb materials breathe in antiphase; speeds up when the session is mastered.</summary>
public class Twinkle : MonoBehaviour
{
    public Color[] colors;            // HDR emission colours, one per material slot that should twinkle
    public int[] slots;
    public float speed = 0.9f;
    Material[] mats;

    void Awake()
    {
        var r = GetComponent<Renderer>();
        mats = r.materials;           // instances
    }

    void Update()
    {
        float s = speed;
        if (VinylSimulationManager.Instance != null && VinylSimulationManager.Instance.CurrentStep == SimStep.Complete) s *= 1.8f;
        for (int i = 0; i < slots.Length; i++)
        {
            float k = 0.55f + 0.45f * Mathf.Sin(Time.time * s * 2f + i * Mathf.PI) + AudioLevels.Level * 0.5f;
            mats[slots[i]].SetColor("_EmissionColor", colors[i] * k);
        }
    }
}
