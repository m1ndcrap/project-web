using UnityEngine;


/// <summary>
/// A script that controls the visibility of a map sprite based on its position within the camera's view for optimization purposes.
/// </summary>


public class MapController : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Camera cam;




    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        cam = Camera.main;
    }




    void Update()
    {
        spriteRenderer.enabled = IsVisible(cam, spriteRenderer, 1.5f);
    }




    /// <summary>
    /// Determines whether the specified sprite renderer is visible within the camera's view frustum, optionally
    /// expanding the bounds by a given factor. Used for optimization purposes to reduce rendering load.
    /// </summary>
    /// <param name="cam">The camera whose view frustum is used to test visibility.</param>
    /// <param name="sr">The sprite renderer whose bounds are evaluated for visibility.</param>
    /// <param name="factor">A multiplier applied to the sprite's bounds size before testing visibility. Must be greater than or equal to
    /// 1.0. Values greater than 1.0 expand the bounds, making the visibility test more lenient.</param>
    /// <returns>true if the (optionally expanded) bounds of the sprite renderer intersect the camera's view frustum; otherwise,
    /// false.</returns>
    bool IsVisible(Camera cam, SpriteRenderer sr, float factor)
    {
        Bounds bounds = sr.bounds;

        float extra = factor - 1f;
        Vector3 expansion = bounds.size * extra;
        bounds.Expand(expansion);

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
        return GeometryUtility.TestPlanesAABB(planes, bounds);
    }
}