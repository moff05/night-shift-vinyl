using UnityEngine;

/// <summary>Speaker cone moves with the bass.</summary>
public class WooferPump : MonoBehaviour
{
    public float travel = 0.012f;
    Vector3 home;
    void Awake() { home = transform.localPosition; }
    void Update()
    {
        float b = AudioLevels.Bass;
        transform.localPosition = home + new Vector3(0f, 0f, -b * travel);
        float s = 1f + b * 0.04f;
        transform.localScale = new Vector3(s, s, 1f + b * 0.25f);
    }
}
