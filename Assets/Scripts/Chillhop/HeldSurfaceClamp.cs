using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Keeps the record physical-feeling without real physics, so it can never glitch through furniture or get lost.
///  - While held it cannot sink below the desk, deck, mixer or crate floor under it.
///  - When let go it settles onto the surface beneath and lies flat (no falling, bouncing or throwing).
///  - If let go anywhere that is not over the desk it glides back to the crate.
/// Once the platter has taken it (the snap zone re-parents it and disables the grab) this script steps aside.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class HeldSurfaceClamp : MonoBehaviour
{
    [System.Serializable]
    public struct Zone
    {
        public Vector2 min, max;     // world XZ rectangle
        public float minY;           // lowest allowed world Y for the record's centre over it
        public Zone(float x0, float z0, float x1, float z1, float y) { min = new Vector2(x0, z0); max = new Vector2(x1, z1); minY = y; }
    }

    public Zone[] zones =
    {
        new Zone(-1.00f, 0.20f, 1.00f, 1.10f, 0.962f),   // desk top
        new Zone(-0.68f, 0.39f, -0.06f, 0.95f, 1.046f),  // turntable (platter height)
        new Zone( 0.03f, 0.43f, 0.53f, 0.89f, 1.075f),   // mixer (clears the pad and dial)
        new Zone( 0.595f, 0.435f, 0.965f, 0.805f, 1.058f) // crate felt
    };
    public float edgePad = 0.12f;           // zones count a little beyond their edge, since the record is 30 cm wide
    public float settleSpeed = 1.6f;        // m/s the record sinks to the surface after release
    public float homeGlideSeconds = 0.6f;

    XRGrabInteractable grab;
    Rigidbody body;
    Transform homeParent;
    Vector3 homePos;
    Quaternion homeRot;
    bool gliding;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        body = GetComponent<Rigidbody>();
        homeParent = transform.parent;
        homePos = transform.position;
        homeRot = transform.rotation;
        if (body != null) { body.isKinematic = true; body.useGravity = false; body.interpolation = RigidbodyInterpolation.None; }
    }

    void OnEnable() { Application.onBeforeRender += OnBeforeRender; }
    void OnDisable() { Application.onBeforeRender -= OnBeforeRender; }

    bool Active => grab != null && grab.enabled && transform.parent == homeParent && !gliding;

    float FloorAt(Vector3 p, out bool overDesk)
    {
        float y = float.NegativeInfinity;
        overDesk = false;
        for (int i = 0; i < zones.Length; i++)
        {
            Zone z = zones[i];
            if (p.x < z.min.x - edgePad || p.x > z.max.x + edgePad || p.z < z.min.y - edgePad || p.z > z.max.y + edgePad) continue;
            if (i == 0) overDesk = true;
            if (z.minY > y) y = z.minY;
        }
        return y;
    }

    void OnBeforeRender()
    {
        if (Active && grab.isSelected) ClampHeld();
    }

    void LateUpdate()
    {
        if (!Active) return;
        if (grab.isSelected) { ClampHeld(); return; }

        Vector3 p = transform.position;
        float floor = FloorAt(p, out bool overDesk);
        if (!overDesk || p.y > 3f || p.y < 0.5f)
        {
            StartCoroutine(GlideHome());
            return;
        }
        // settle onto the surface and lie flat
        p.y = Mathf.MoveTowards(p.y, floor, settleSpeed * Time.deltaTime);
        Vector3 e = transform.eulerAngles;
        Quaternion flat = Quaternion.Euler(0f, e.y, 0f);
        transform.SetPositionAndRotation(p, Quaternion.Slerp(transform.rotation, flat, 8f * Time.deltaTime));
        if (body != null) { body.position = transform.position; body.rotation = transform.rotation; }
    }

    void ClampHeld()
    {
        Vector3 p = transform.position;
        float floor = FloorAt(p, out _);
        if (floor == float.NegativeInfinity || p.y >= floor) return;
        p.y = floor;
        transform.position = p;
        if (body != null) body.position = p;
    }

    System.Collections.IEnumerator GlideHome()
    {
        gliding = true;
        Vector3 from = transform.position;
        Quaternion fromRot = transform.rotation;
        float t = 0f;
        while (t < homeGlideSeconds)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / homeGlideSeconds));
            Vector3 p = Vector3.Lerp(from, homePos, k);
            p.y += Mathf.Sin(k * Mathf.PI) * 0.12f;            // a little arc so it reads as being put back
            transform.SetPositionAndRotation(p, Quaternion.Slerp(fromRot, homeRot, k));
            yield return null;
        }
        transform.SetPositionAndRotation(homePos, homeRot);
        if (body != null) { body.position = homePos; body.rotation = homeRot; }
        gliding = false;
    }
}
