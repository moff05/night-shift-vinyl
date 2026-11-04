using UnityEngine;

/// <summary>Slow breathing scale (the sleeping cat).</summary>
public class Breathe : MonoBehaviour
{
    public float amount = 0.03f, rate = 1.5f;
    Vector3 baseScale;
    void Awake() { baseScale = transform.localScale; }
    void Update()
    {
        float k = Mathf.Sin(Time.time * rate) * amount;
        transform.localScale = new Vector3(baseScale.x * (1f + k * 0.5f), baseScale.y * (1f + k), baseScale.z * (1f + k * 0.5f));
    }
}
