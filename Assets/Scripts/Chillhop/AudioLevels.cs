using UnityEngine;

/// <summary>Samples the final audio mix once per frame so room visuals (speakers, VU meter, neon) can move with the music.</summary>
[DefaultExecutionOrder(-50)]
public class AudioLevels : MonoBehaviour
{
    public static float Bass, Mid, High, Level;
    public static readonly float[] Bands = new float[8];

    static readonly int[][] BandBins =
    {
        new[] {1, 2}, new[] {3, 4}, new[] {5, 8}, new[] {9, 14}, new[] {15, 24}, new[] {25, 40}, new[] {41, 70}, new[] {71, 140}
    };
    static readonly float[] Gain = { 22f, 26f, 34f, 48f, 70f, 110f, 190f, 380f };

    readonly float[] spectrum = new float[512];

    static float Smooth(float current, float target, float dt)
    {
        float rate = target > current ? 28f : 5f;      // fast attack, slower release
        return Mathf.Lerp(current, target, 1f - Mathf.Exp(-rate * dt));
    }

    void Update()
    {
        AudioListener.GetSpectrumData(spectrum, 0, FFTWindow.Hamming);
        float dt = Time.unscaledDeltaTime;
        float sum = 0f;
        for (int b = 0; b < Bands.Length; b++)
        {
            int lo = BandBins[b][0], hi = BandBins[b][1];
            float acc = 0f;
            for (int i = lo; i <= hi; i++) acc += spectrum[i];
            float v = Mathf.Clamp01(acc / (hi - lo + 1) * Gain[b]);
            Bands[b] = Smooth(Bands[b], v, dt);
            sum += Bands[b];
        }
        Bass = (Bands[0] + Bands[1]) * 0.5f;
        Mid = (Bands[3] + Bands[4] + Bands[5]) / 3f;
        High = (Bands[6] + Bands[7]) * 0.5f;
        Level = sum / Bands.Length;
    }
}
