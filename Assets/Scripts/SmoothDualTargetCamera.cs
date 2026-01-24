using UnityEngine;

public class SmoothDualTargetCamera : MonoBehaviour
{
    [SerializeField] private Transform target1;
    [SerializeField] private Transform target2;
    [SerializeField] private Camera cam;
    [SerializeField] private float minOrthographicSize = 5f;
    [SerializeField] private float maxOrthographicSize = 15f;
    [SerializeField] private float positionSmoothness = 0.1f;
    [SerializeField] private float zoomSmoothness = 0.1f;
    [SerializeField] private float distanceThreshold = 200f;
    [SerializeField] private float zoomDivisor = 1500f;
    private float currentOrthographicSize;
    private float targetOrthographicSize;

    void Start()
    {
        if (cam == null)
            cam = GetComponent<Camera>();

        currentOrthographicSize = cam.orthographicSize;
        targetOrthographicSize = currentOrthographicSize;
    }

    void LateUpdate()
    {
        if (target1 == null || target2 == null)
            return;

        // Calculate middle point
        float xDif = Mathf.Abs((target1.position.x - target2.position.x) / 3f);
        float yDif = Mathf.Abs((target1.position.y - target2.position.y) / 3f);

        // Get direction and distance between targets
        Vector2 direction = (target2.position - target1.position).normalized;

        // Calculate middle offset
        float xMiddle = direction.x * xDif;
        float yMiddle = direction.y * yDif;

        // Target position
        Vector3 targetPos = new Vector3(target1.position.x + xMiddle, target1.position.y + yMiddle, transform.position.z);

        // Smoothen movement
        Vector3 smoothPos = Vector3.Lerp(transform.position, targetPos, positionSmoothness);
        transform.position = smoothPos;

        // Calculate zoom based on distance between targets
        float distance = Vector2.Distance(target1.position, target2.position);

        if (distance > distanceThreshold)
        {
            float zoomFactor = distance / zoomDivisor;
            float sizeIncrease = currentOrthographicSize * zoomFactor;
            targetOrthographicSize = currentOrthographicSize + sizeIncrease;
        }
        else
            targetOrthographicSize = minOrthographicSize;

        // Clamp zoom
        targetOrthographicSize = Mathf.Clamp(targetOrthographicSize, minOrthographicSize, maxOrthographicSize);

        // Smooth zoom
        currentOrthographicSize = Mathf.Lerp(currentOrthographicSize, targetOrthographicSize, zoomSmoothness);
        cam.orthographicSize = currentOrthographicSize;
    }

    void OnDrawGizmos()
    {
        if (target1 == null || target2 == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(target1.position, target2.position);
        float xDif = Mathf.Abs((target1.position.x - target2.position.x) / 3f);
        float yDif = Mathf.Abs((target1.position.y - target2.position.y) / 3f);
        Vector2 direction = (target2.position - target1.position).normalized;
        float xMiddle = direction.x * xDif;
        float yMiddle = direction.y * yDif;
        Vector3 midpoint = new Vector3(target1.position.x + xMiddle, target1.position.y + yMiddle, 0);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(midpoint, 0.5f);
    }
}