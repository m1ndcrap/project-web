using UnityEngine;

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