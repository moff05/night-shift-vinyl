using UnityEngine;

/// <summary>Row of LED bars on the mixer that dance with the eight audio bands.</summary>
public class VuMeter : MonoBehaviour
{
    public Transform[] bars;
    public float baseY = 0.064f, maxHeight = 0.034f, minHeight = 0.003f;
    void Update()
    {
        for (int i = 0; i < bars.Length && i < AudioLevels.Bands.Length; i++)
        {
            float h = Mathf.Lerp(minHeight, maxHeight, AudioLevels.Bands[i]);
            var s = bars[i].localScale; s.y = h; bars[i].localScale = s;
            var p = bars[i].localPosition; p.y = baseY + h * 0.5f; bars[i].localPosition = p;
        }
    }
}
