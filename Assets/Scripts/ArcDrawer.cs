using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
public class ArcDrawer : MonoBehaviour
{
    public enum DrawAxis
    {
        X,
        Y,
        Z
    }

    public enum DrawMode
    {
        Arc,
        Outline,
        Full
    }
    [Header("Arc Settings")]
    [Tooltip("The plane Axis in 3D space to draw the arc")]
    public DrawAxis drawAxis = DrawAxis.X;
    [Tooltip("Should we close the drawing of the Angle to create a cone shape?")] 
    public DrawMode drawMode = DrawMode.Outline;
    public bool useWorldRotation = true;
    [Tooltip("Should the Start and End angles be swapped if values crossover?")] 
    public bool allowStartEndAngleSwap = false;
    [Tooltip("Draw the angle counter-clockwise instead of clockwise")] 
    [SerializeField] private bool reverse;
    [Range(0f, 360f)] public float startAngle = 0f;
    [Range(0f, 360f)] public float endAngle = 90f;
    public float radius = 5f;
    [Tooltip("The amount of lines used to draw the angle")]
    [Range(2F, 360F)] public int stepCount = 50;
    public Color arcColor = Color.green;
    public Color closedLineColour = Color.green;

    [Header("Runtime Settings")]
    public bool drawInEditor = true;
    public bool drawInPlayMode = true;

    // [Header("Mesh")]
    // Mesh mesh;
    // MeshFilter meshFilter;
    // MeshRenderer meshRenderer;
    // List<Vector3> vertices;
    // int[] triangles;
    // public Material material;
    
    void Update()
    {
        if (Application.isPlaying && drawInPlayMode)
        {
            DrawArc();
        }
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying && drawInEditor)
        {
            DrawArc();
        }
    }

    void DrawArc()
    {
        // Normalize angles
        float start = Mathf.Min(startAngle, endAngle);
        float end = Mathf.Max(startAngle, endAngle);
        float angleRange = allowStartEndAngleSwap ? end - start : end;

        // if (!meshFilter)
        //     meshFilter = gameObject.AddComponent<MeshFilter>();
        //
        // if (!meshRenderer)
        //     meshRenderer = gameObject.AddComponent<MeshRenderer>();
        //
        // if (meshRenderer.material == null)
        //     meshRenderer.material = material;
        
        
        // mesh = new Mesh();
        // meshFilter.mesh = mesh;
        
        // vertices = new List<Vector3>() { this.transform.position };
        // triangles = new int[stepCount + 2];

        Vector3 GetWorldPoint(float angle)
        {
            Vector3 localPoint = GetPointOnCircle(angle);
            return transform.position + (useWorldRotation ? transform.rotation * localPoint : localPoint);
        }
        
        Vector3 lastPoint = GetWorldPoint(start);
        // vertices.Add(lastPoint);

        for (int i = 1; i <= stepCount; i++)
        {
            float t = i / (float)stepCount;
            float angle = start + t * angleRange;
            Vector3 nextPoint = GetWorldPoint(angle);
            
            // vertices.Add(nextPoint);
            // triangles[i - 1] = 0;
            // triangles[i] = i;
            // triangles[i + 1] = i + 1;

            Debug.DrawLine(lastPoint, nextPoint, arcColor);

            if (drawMode != DrawMode.Arc)
            {
                Debug.DrawLine(transform.position, GetWorldPoint(start), closedLineColour);
                Debug.DrawLine(transform.position, GetWorldPoint(start + 1 * angleRange), closedLineColour);

                if (drawMode == DrawMode.Full)
                {
                    Debug.DrawLine(transform.position, lastPoint, arcColor);
                }
            }
            
            lastPoint = nextPoint;
        }
        
        
        
        // mesh.vertices = vertices.ToArray();
        // Debug.Log(triangles.Length);
        // mesh.triangles = triangles;
    }

    Vector3 GetPointOnCircle(float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;

        float start = reverse ? Mathf.Cos(rad) :  Mathf.Sin(rad);
        float end = reverse ? Mathf.Sin(rad) : Mathf.Cos(rad);
        
        return drawAxis switch
        {
            DrawAxis.X => new Vector3(start, end, 0F) * radius,
            DrawAxis.Y => new Vector3(start, 0F, end) * radius,
            DrawAxis.Z => new Vector3(0F, end, start) * radius
        };
    }
}