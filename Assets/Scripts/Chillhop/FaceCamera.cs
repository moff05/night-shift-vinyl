using UnityEngine;

/// <summary>Keeps a quad (halo sprite) turned toward the player's head.</summary>
public class FaceCamera : MonoBehaviour
{
    Transform cam;
    void LateUpdate()
    {
        if (cam == null)
        {
            if (Camera.main == null) return;
            cam = Camera.main.transform;
        }
        transform.rotation = Quaternion.LookRotation(transform.position - cam.position, Vector3.up);
    }
}
