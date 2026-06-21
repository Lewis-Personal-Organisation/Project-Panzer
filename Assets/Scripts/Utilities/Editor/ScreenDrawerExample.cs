using System;
using System.Linq;
using UnityEngine;

public class ScreenDrawerExample : MonoBehaviour
{
    private void Update()
    {
        DrawAroundCanvas();
    }

    /// <summary>
    /// Draws the points around the canvas using Screen Drawer
    /// </summary>
    private void DrawAroundCanvas()
    {
        Vector3[] points = new Vector3[]
        {
            new Vector3(0F, 0F, 0F), 
            new Vector3(100, 0F, 0F), 
            new Vector3(100F, 100F, 0F), 
            new Vector3(0F, 100F, 0F), 
            new Vector3(0F, 0F, 0F),
        };
        
        ScreenDrawer.Instance.DrawAsCanvasPercentage(points.ToArray());
    }
}
