using UnityEngine;

/// <summary>Scrolls a material's main texture (rain on the window).</summary>
public class ScrollUV : MonoBehaviour
{
    public Vector2 speed = new Vector2(0f, -0.8f);
    Material mat;
    void Awake() { mat = GetComponent<Renderer>().material; }
    void Update() { mat.mainTextureOffset += speed * Time.deltaTime; }
}
