using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// Applies a horizontal color gradient effect to a UI Graphic element by blending between two colors from left to
/// right.
/// </summary>


[RequireComponent(typeof(Graphic))]
[AddComponentMenu("UI/Effects/UI Horizontal Gradient")]


public class UIHorizontalGradient : BaseMeshEffect
{
    public Color colorLeft = new Color32(255, 235, 4, 255);
    public Color colorRight = new Color32(255, 165, 0, 255);




    /// <summary>
    /// Modifies the mesh to apply a horizontal color gradient to the UI graphic.
    /// </summary>
    /// <param name="vh">The VertexHelper instance containing the mesh data to modify. Must not be null and must contain at least one vertex.</param>
    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0) return;

        Rect rect = graphic.rectTransform.rect;
        UIVertex vertex = default;

        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            float t = Mathf.InverseLerp(rect.xMin, rect.xMax, vertex.position.x);
            vertex.color = Color.Lerp(colorLeft, colorRight, t);
            vh.SetUIVertex(vertex, i);
        }
    }
}