using UnityEngine;

public class GoblinPath : MonoBehaviour
{
    public Transform[] points;

    void Awake()
    {
        if (points == null || points.Length == 0)
        {
            points = new Transform[transform.childCount];

            for (int i = 0; i < transform.childCount; i++)
            {
                points[i] = transform.GetChild(i);
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;

        for (int i = 0; i < transform.childCount - 1; i++)
        {
            Gizmos.DrawLine(transform.GetChild(i).position, transform.GetChild(i + 1).position);
        }
    }
}