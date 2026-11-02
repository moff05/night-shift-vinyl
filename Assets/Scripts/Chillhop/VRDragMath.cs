using UnityEngine;

/// <summary>Small helpers shared by the hand-driven rotary and slider controls.</summary>
public static class VRDragMath
{
    /// <summary>
    /// Direction from <paramref name="center"/> to <paramref name="point"/> flattened onto the plane
    /// perpendicular to <paramref name="axisWorld"/>. False when the hand is too close to the axis to give a stable angle.
    /// </summary>
    public static bool TryPlanarDirection(Vector3 point, Vector3 center, Vector3 axisWorld, out Vector3 direction)
    {
        Vector3 d = Vector3.ProjectOnPlane(point - center, axisWorld);
        if (d.sqrMagnitude < 1e-6f)
        {
            direction = Vector3.zero;
            return false;
        }
        direction = d.normalized;
        return true;
    }
}
