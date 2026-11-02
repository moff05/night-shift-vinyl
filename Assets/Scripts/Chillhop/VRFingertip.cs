using UnityEngine;

/// <summary>
/// Put this on a small child of each hand's Poke Interactor attach point (the index fingertip).
/// It makes a kinematic trigger sphere that VRPokeButton can feel. No tags needed.
/// </summary>
[RequireComponent(typeof(SphereCollider), typeof(Rigidbody))]
public class VRFingertip : MonoBehaviour
{
    SphereCollider sphere;

    /// <summary>World-space radius of the fingertip sphere.</summary>
    public float Radius
    {
        get
        {
            if (sphere == null) sphere = GetComponent<SphereCollider>();
            Vector3 s = transform.lossyScale;
            return sphere.radius * Mathf.Max(s.x, Mathf.Max(s.y, s.z));
        }
    }

    void Reset()
    {
        Configure();
        sphere.radius = 0.008f;
    }

    void Awake()
    {
        Configure();
    }

    void Configure()
    {
        sphere = GetComponent<SphereCollider>();
        sphere.isTrigger = true;

        var body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
    }
}
