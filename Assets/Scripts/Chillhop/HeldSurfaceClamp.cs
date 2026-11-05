using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// A grabbed record follows the hand kinematically, so it would sail straight through the desk and the deck.
/// While held, this keeps it from sinking below whatever solid surface is under it.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class HeldSurfaceClamp : MonoBehaviour
{
    public float clearance = 0.012f;
    public float rayStart = 0.35f;
    public float rayLength = 1.6f;

    XRGrabInteractable grab;
    Rigidbody body;
    static readonly RaycastHit[] hits = new RaycastHit[16];

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        body = GetComponent<Rigidbody>();
    }

    void LateUpdate()
    {
        if (grab == null || !grab.isSelected) return;
        Vector3 p = transform.position;
        int n = Physics.RaycastNonAlloc(p + Vector3.up * rayStart, Vector3.down, hits, rayLength, ~0, QueryTriggerInteraction.Ignore);
        float floorY = float.NegativeInfinity;
        for (int i = 0; i < n; i++)
        {
            Collider c = hits[i].collider;
            if (c == null || c.transform.IsChildOf(transform)) continue;
            if (c.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() != null) continue;     // hands / rig
            if (c.GetComponentInParent<XRBaseInteractable>() != null) continue;               // other controls
            if (hits[i].point.y > floorY) floorY = hits[i].point.y;
        }
        if (floorY == float.NegativeInfinity) return;
        float minY = floorY + clearance;
        if (p.y < minY)
        {
            p.y = minY;
            transform.position = p;
            if (body != null) body.position = p;
        }
    }
}
