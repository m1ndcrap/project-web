using UnityEngine;


/// <summary>
/// Represents a visual effect that displays an expanding, fading circle using a LineRenderer component. The effect
/// automatically destroys itself when it reaches its maximum radius.
/// </summary>


public class BarrierCircleEffect : MonoBehaviour
{
    private LineRenderer circleRenderer;
    private int circleSegments = 32;
    private float rad = 0f;
    private float maxRad = 0.6f;




    void Awake()
    {
        circleRenderer = GetComponent<LineRenderer>();

        if (circleRenderer == null) return;

        circleRenderer.positionCount = circleSegments + 1;
        circleRenderer.loop = false;
        circleRenderer.useWorldSpace = false;
    }




    void Update()
    {
        rad += 0.02f;

        if (rad >= maxRad)
        {
            Destroy(gameObject);
            return;
        }

        float alpha = 1f - (rad / maxRad);
        DrawCircle(rad, alpha);
    }




    /// <summary>
    /// Draws a circle with the specified radius and opacity using the current renderer.
    /// </summary>
    /// <param name="radius">The radius of the circle to draw. Must be non-negative. The value determines the size of the circle in world
    /// units.</param>
    /// <param name="opacity">The opacity of the circle, where 0 is fully transparent and 1 is fully opaque. Values outside the range [0, 1]
    /// are clamped.</param>
    void DrawCircle(float radius, float opacity)
    {
        if (circleRenderer == null) return;

        Color c = Color.white;
        c.a = Mathf.Clamp01(opacity);
        circleRenderer.startColor = c;
        circleRenderer.endColor = c;

        for (int i = 0; i <= circleSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / circleSegments;
            circleRenderer.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
        }
    }
}