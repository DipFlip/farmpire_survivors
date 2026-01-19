using UnityEngine;

/// <summary>
/// Makes this object always face the camera.
/// Attach to a World Space Canvas or any object that should billboard.
/// </summary>
public class Billboard : MonoBehaviour
{
    [Tooltip("If empty, uses Camera.main")]
    [SerializeField] private Camera targetCamera;

    [Tooltip("Only rotate on Y axis (keeps object upright)")]
    [SerializeField] private bool lockYAxis = false;

    private void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void LateUpdate()
    {
        if (targetCamera == null) return;

        if (lockYAxis)
        {
            Vector3 lookDir = targetCamera.transform.position - transform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(-lookDir);
            }
        }
        else
        {
            transform.rotation = targetCamera.transform.rotation;
        }
    }
}
