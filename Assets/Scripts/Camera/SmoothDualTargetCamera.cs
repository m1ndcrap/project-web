using UnityEngine;


/// <summary>
/// Frames two targets at once (for example, the player and a boss) by smoothly moving to the point
/// one-third of the way from the first target toward the second, and zooming out as they get further
/// apart. Useful for encounters where both participants need to stay on screen without a fixed camera.
/// </summary>


public class SmoothDualTargetCamera : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("The primary target, the camera frames a point 1/3 of the way from this target toward Target 2, not the exact midpoint.")]
    [SerializeField] private Transform target1;
    [Tooltip("The secondary target.")]
    [SerializeField] private Transform target2;
    [Tooltip("The camera to control. Auto-fetched from this GameObject in Start() if left empty.")]
    [SerializeField] private Camera cam;




    [Header("Zoom Limits")]
    [Tooltip("The closest (smallest) orthographic size the camera will zoom to, used whenever the targets are within Distance Threshold of each other.")]
    [SerializeField] private float minOrthographicSize = 5f;
    [Tooltip("The furthest (largest) orthographic size the camera is allowed to zoom out to, no matter how far apart the targets get.")]
    [SerializeField] private float maxOrthographicSize = 15f;




    [Header("Smoothing")]
    [Tooltip("How quickly the camera's position catches up to the targets each frame. Lower is smoother/slower, higher is snappier.")]
    [Range(0f, 1f)]
    [SerializeField] private float positionSmoothness = 0.1f;
    [Tooltip("How quickly the camera's zoom catches up to the target zoom level each frame. Lower is smoother/slower, higher is snappier.")]
    [Range(0f, 1f)]
    [SerializeField] private float zoomSmoothness = 0.1f;




    [Header("Zoom Distance Tuning")]
    [Tooltip("Once the targets are further apart than this (in world units), the camera starts zooming out to keep both in view.")]
    [SerializeField] private float distanceThreshold = 200f;
    [Tooltip("Controls how aggressively the camera zooms out as distance increases past the threshold. Larger values zoom out more gently; smaller values zoom out faster.")]
    [SerializeField] private float zoomDivisor = 1500f;




    private float currentOrthographicSize;
    private float targetOrthographicSize;




    private void Start()
    {
        if (cam == null)
            cam = GetComponent<Camera>();

        if (cam == null)
        {
            Debug.LogWarning($"[SmoothDualTargetCamera] No Camera assigned or found on '{gameObject.name}'. Zoom won't work until one is set.", this);
            return;
        }

        currentOrthographicSize = cam.orthographicSize;
        targetOrthographicSize = currentOrthographicSize;
    }




    private void LateUpdate()
    {
        if (target1 == null || target2 == null || cam == null)
            return;

        Vector2 framingPoint = CalculateFramingPoint();
        Vector3 targetPos = new Vector3(framingPoint.x, framingPoint.y, transform.position.z);

        // Smoothly move toward the framing point
        transform.position = Vector3.Lerp(transform.position, targetPos, positionSmoothness);

        // Zoom out once the targets are further apart than the threshold, otherwise hold at the minimum zoom
        float distance = Vector2.Distance(target1.position, target2.position);

        if (distance > distanceThreshold)
        {
            float zoomFactor = distance / zoomDivisor;
            float sizeIncrease = currentOrthographicSize * zoomFactor;
            targetOrthographicSize = currentOrthographicSize + sizeIncrease;
        }
        else
        {
            targetOrthographicSize = minOrthographicSize;
        }

        targetOrthographicSize = Mathf.Clamp(targetOrthographicSize, minOrthographicSize, maxOrthographicSize);

        // Smoothly zoom toward the target size
        currentOrthographicSize = Mathf.Lerp(currentOrthographicSize, targetOrthographicSize, zoomSmoothness);
        cam.orthographicSize = currentOrthographicSize;
    }




    /// <summary>
    /// Calculates the X/Y point the camera frames: 1/3 of the way from <see cref="target1"/> toward
    /// <see cref="target2"/>, rather than the exact midpoint. Shared by <see cref="LateUpdate"/> and
    /// <see cref="OnDrawGizmos"/> so the two can never drift out of sync with each other.
    /// </summary>
    private Vector2 CalculateFramingPoint()
    {
        float xOffset = Mathf.Abs((target1.position.x - target2.position.x) / 3f);
        float yOffset = Mathf.Abs((target1.position.y - target2.position.y) / 3f);

        Vector2 direction = (target2.position - target1.position).normalized;

        float xPoint = direction.x * xOffset;
        float yPoint = direction.y * yOffset;

        return new Vector2(target1.position.x + xPoint, target1.position.y + yPoint);
    }




    private void OnDrawGizmos()
    {
        if (target1 == null || target2 == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(target1.position, target2.position);

        Gizmos.color = Color.green;
        Vector2 framingPoint = CalculateFramingPoint();
        Gizmos.DrawWireSphere(new Vector3(framingPoint.x, framingPoint.y, 0f), 0.5f);
    }
}