using UnityEngine;

/// <summary>
/// Safety net for grabbables under optical hand tracking: if the record is dropped off the desk
/// or slips through a collider, put it back where it started. Put it on the vinyl.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class RespawnIfFallen : MonoBehaviour
{
    [Tooltip("World Y below which the object counts as lost (floor height minus a margin).")]
    [SerializeField] float minY = -0.5f;

    Rigidbody body;
    Vector3 homePos;
    Quaternion homeRot;
    Transform homeParent;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        homeParent = transform.parent;
        homePos = transform.localPosition;
        homeRot = transform.localRotation;
    }

    void Update()
    {
        if (transform.position.y >= minY) return;

        bool kinematic = body.isKinematic;
        body.isKinematic = false;                      // velocities can only be zeroed on a dynamic body
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.isKinematic = kinematic;
        transform.SetParent(homeParent, false);
        transform.SetLocalPositionAndRotation(homePos, homeRot);
    }
}
