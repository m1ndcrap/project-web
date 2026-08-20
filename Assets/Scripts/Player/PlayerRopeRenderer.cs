using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Draws the player's rope as a chain of pooled segment GameObjects stretched between two points.
/// Used for both the swing rope and the quick-zip line. Segments are recycled instead of
/// instantiated/destroyed every frame to avoid garbage collection churn while swinging.
/// </summary>


public class PlayerRopeRenderer
{
    private readonly GameObject segmentPrefab;
    private readonly float segmentLength;
    private readonly int maxPoolSize;


    private readonly List<GameObject> activeSegments = new List<GameObject>();
    private readonly Queue<GameObject> pooledSegments = new Queue<GameObject>();




    /// <summary>
    /// Creates a new rope renderer.
    /// </summary>
    /// <param name="segmentPrefab">The prefab to instantiate for each rope segment.</param>
    /// <param name="segmentLength">The world-space length each segment covers along the rope.</param>
    /// <param name="maxPoolSize">The maximum number of inactive segments to keep pooled before excess ones are destroyed instead of recycled.</param>
    public PlayerRopeRenderer(GameObject segmentPrefab, float segmentLength = 0.15f, int maxPoolSize = 200)
    {
        this.segmentPrefab = segmentPrefab;
        this.segmentLength = segmentLength;
        this.maxPoolSize = maxPoolSize;
    }




    /// <summary>
    /// Draws a rope between two points by placing and rotating pooled segments along the line
    /// between them. Returns all previously active segments to the pool first.
    /// </summary>
    /// <param name="start">One end of the rope.</param>
    /// <param name="end">The other end of the rope.</param>
    public void Draw(Vector2 start, Vector2 end)
    {
        ReturnAllToPool();

        Vector2 direction = (end - start).normalized;
        float distance = Vector2.Distance(start, end);
        int segmentCount = Mathf.CeilToInt(distance / segmentLength);

        for (int i = 0; i < segmentCount; i++)
        {
            Vector2 position = start + direction * segmentLength * i;
            GameObject segment = GetSegment(position);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            segment.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }




    /// <summary>
    /// Gets a segment from the pool (or instantiates a new one if the pool is empty) and positions it at the given world-space coordinates.
    /// </summary>
    /// <param name="position">The world-space position to place the segment at.</param>
    /// <returns>The segment GameObject.</returns>
    private GameObject GetSegment(Vector2 position)
    {
        GameObject segment;

        if (pooledSegments.Count > 0)
        {
            segment = pooledSegments.Dequeue();
            segment.SetActive(true);
        }
        else
        {
            segment = Object.Instantiate(segmentPrefab);
        }

        segment.transform.position = position;
        activeSegments.Add(segment);
        return segment;
    }




    /// <summary>
    /// Deactivates every currently-drawn segment and returns it to the pool (or destroys it if the pool is already full).
    /// </summary>
    public void ReturnAllToPool()
    {
        foreach (var segment in activeSegments)
        {
            if (pooledSegments.Count < maxPoolSize)
            {
                segment.SetActive(false);
                pooledSegments.Enqueue(segment);
            }
            else
            {
                Object.Destroy(segment);
            }
        }

        activeSegments.Clear();
    }
}