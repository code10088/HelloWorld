using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XUGL;

public class UILineRenderer : Graphic
{
    private List<Vector3> points;
    public float width;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        UGL.DrawLine(vh, points, width, Color.white, false);
    }

    public void SetPoints(List<Vector3> points)
    {
        this.points = points;
        SetVerticesDirty();
    }
}
