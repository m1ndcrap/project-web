using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class TilemapCollisionGizmo : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private Color tileColor = Color.red;
    [SerializeField] private Color topCornerColor = Color.yellow;
    [SerializeField] private float wireScale = 0.9f;
    [SerializeField] private bool highlightTopCorners = true;
    [SerializeField] private int paddingCells = 2;

    private void OnDrawGizmos()
    {
        if (tilemap == null) return;

        Camera cam = Camera.current;
        if (cam == null) return;

        BoundsInt bounds = GetVisibleTileBounds(cam);

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            if (!tilemap.HasTile(pos)) continue;

            Vector3 center = tilemap.GetCellCenterWorld(pos);
            Vector3 bl = tilemap.CellToWorld(pos);
            Vector3 tr = tilemap.CellToWorld(pos + new Vector3Int(1, 1, 0));
            Vector3 size = tr - bl;

            bool isTopOfWall = !tilemap.HasTile(pos + Vector3Int.up);

            Gizmos.color = (highlightTopCorners && isTopOfWall) ? topCornerColor : tileColor;
            Gizmos.DrawWireCube(center, size * wireScale);
        }
    }

    private BoundsInt GetVisibleTileBounds(Camera cam)
    {
        float planeZ = tilemap.transform.position.z;
        Plane groundPlane = new Plane(Vector3.forward, new Vector3(0, 0, planeZ));

        Vector3 min = RaycastViewportToPlane(cam, groundPlane, new Vector3(0, 0, 0));
        Vector3 max = RaycastViewportToPlane(cam, groundPlane, new Vector3(1, 1, 0));

        Vector3Int cellMin = tilemap.WorldToCell(min);
        Vector3Int cellMax = tilemap.WorldToCell(max);

        int xMin = Mathf.Min(cellMin.x, cellMax.x) - paddingCells;
        int yMin = Mathf.Min(cellMin.y, cellMax.y) - paddingCells;
        int xMax = Mathf.Max(cellMin.x, cellMax.x) + paddingCells;
        int yMax = Mathf.Max(cellMin.y, cellMax.y) + paddingCells;

        return new BoundsInt(xMin, yMin, 0, xMax - xMin, yMax - yMin, 1);
    }

    private Vector3 RaycastViewportToPlane(Camera cam, Plane plane, Vector3 viewportPoint)
    {
        Ray ray = cam.ViewportPointToRay(viewportPoint);

        if (plane.Raycast(ray, out float distance))
            return ray.GetPoint(distance);

        // Fallback if the ray is parallel to the plane (shouldn't normally happen in 2D view)
        return cam.ViewportToWorldPoint(new Vector3(viewportPoint.x, viewportPoint.y, Mathf.Abs(cam.transform.position.z - plane.distance)));
    }
}