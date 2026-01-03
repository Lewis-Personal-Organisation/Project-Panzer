using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Splines;

[RequireComponent(typeof(SplineContainer))]
public class SplineColliderGenerator : MonoBehaviour
{
    public enum GenerationType
    {
        Path,
        BoundingBox
    }
    public enum BoundingScaleMethod
    {
        LowestPoint,
        HighestPoint,
        Average,
    }
    
    [SerializeField] private SplineContainer splineContainer;
    [EnumToggleButtons] public GenerationType generationTypeField;
    private GameObject splinePathColliderHolder;
    private GameObject splineBoundsColliderHolder;
    
    [ShowIf("generationTypeField",  GenerationType.Path)]
    [SerializeField] private float colliderWidth = 1f;
    [SerializeField] private float colliderHeight = 1f;
    [ShowIf("generationTypeField",  GenerationType.Path)]
    [SerializeField] private int resolution = 50;
    [ShowIf("generationTypeField",  GenerationType.Path)]
    [SerializeField] private bool allowTilt = true;
    [SerializeField] private bool generateAbove = true;
    [ShowIf("generationTypeField", GenerationType.BoundingBox)]
    [SerializeField] private BoundingScaleMethod scaleMethod;

    /// <summary>
    /// Finds the Colliders on editor load
    /// </summary>
    private void OnValidate()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
            return;
        
        splineBoundsColliderHolder = GameObject.Find("Spline Bounds Collider");
        splinePathColliderHolder = GameObject.Find("Spline Mesh Collider");
#endif
    }
    
    [ShowInInspector, ReadOnly, HideIf("@clickCounter == 0"), GUIColor("RGBA(0, 1, 0, 1)")]
    private string NewColliderYPos;
    private int clickCounter = 0;
    

    private bool buttonToggle;
    [Button("Generate", ButtonSizes.Medium)]
    private void MediumSizedButton()
    {
        if (generationTypeField == GenerationType.Path)
        {
            GenerateSplineCollider();
        }
        else
        {
            GenerateBoundingCollider();
        }
    }
    
    /// <summary>
    /// Generates the Spline Collider from the primary spline
    /// </summary>
    private void GenerateSplineCollider()
    {
        // Setup/Cleanup
        if (!splinePathColliderHolder)
        {
            splinePathColliderHolder = new GameObject("Spline Mesh Collider");
            splinePathColliderHolder.transform.SetParent(splineContainer.transform);
            splinePathColliderHolder.transform.localPosition = Vector3.zero;
            splinePathColliderHolder.transform.localRotation = Quaternion.identity;
        }
        else
        {
            foreach (var col in splinePathColliderHolder.GetComponents<MeshCollider>())
            {
                DestroyImmediate(col);
            }
        }
        
        // Create lists for vertices and triangles
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        
        // Sample points along the spline
        for (int i = 0; i <= resolution; i++)
        {
            float t = i / (float)resolution;
            
            splineContainer.Spline.Evaluate(t, out float3 position, out float3 tangent, out float3 upVector);
            
            float3 up;
            float3 right;
            
            if (allowTilt)
            {
                // Use spline’s orientation
                up = math.normalize(upVector);
                right = math.normalize(math.cross(up, tangent));
            }
            else
            {
                // Ignore spline roll
                float3 fixedUp = math.up();
                right = math.normalize(math.cross(fixedUp, tangent));
                up = math.normalize(math.cross(tangent, right));
            }
            
            float3 rOff = right * (colliderWidth * 0.5f);
            float3 uOff = up    * (colliderHeight * 0.5f);
            
            vertices.Add(position + rOff + uOff);
            vertices.Add(position - rOff + uOff);
            vertices.Add(position + rOff - uOff);
            vertices.Add(position - rOff - uOff);
        }
        
        // Generate triangles connecting the segments
        for (int i = 0; i < resolution; i++)
        {
            int baseIndex = i * 4;
            int nextBase = (i + 1) * 4;
            
            // Top face
            triangles.AddRange(new int[] { baseIndex, nextBase, baseIndex + 1 });
            triangles.AddRange(new int[] { baseIndex + 1, nextBase, nextBase + 1 });
            
            // Bottom face
            triangles.AddRange(new int[] { baseIndex + 2, baseIndex + 3, nextBase + 2 });
            triangles.AddRange(new int[] { nextBase + 2, baseIndex + 3, nextBase + 3 });
            
            // Right face
            triangles.AddRange(new int[] { baseIndex, baseIndex + 2, nextBase });
            triangles.AddRange(new int[] { nextBase, baseIndex + 2, nextBase + 2 });
            
            // Left face
            triangles.AddRange(new int[] { baseIndex + 1, nextBase + 1, baseIndex + 3 });
            triangles.AddRange(new int[] { nextBase + 1, nextBase + 3, baseIndex + 3 });
        }
        
        splinePathColliderHolder.transform.localPosition = generateAbove ? Vector3.up * colliderHeight * 0.5F : Vector3.zero;
        
        // Create mesh
        Mesh mesh = new Mesh
        {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        // Add MeshCollider
        MeshCollider meshCollider = splinePathColliderHolder.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
        meshCollider.convex = false;
    }
    
    /// <summary>
    /// Generates the Bounding Collider from the primary spline
    /// </summary>
    private void GenerateBoundingCollider()
    {
        // Prepare holder
        if (!splineBoundsColliderHolder)
        {
            splineBoundsColliderHolder = new GameObject("Spline Bounds Collider");
            splineBoundsColliderHolder.transform.SetParent(splineContainer.transform);
            splineBoundsColliderHolder.transform.localPosition = Vector3.zero;
            splineBoundsColliderHolder.transform.localRotation = Quaternion.identity;
        }
        else
        {
            foreach (var col in splineBoundsColliderHolder.GetComponents<Collider>())
                DestroyImmediate(col);
        }

        // Sample points
        List<Vector3> points = new List<Vector3>();

        for (int i = 0; i <= resolution; i++)
        {
            float t = i / (float)resolution;
            splineContainer.Spline.Evaluate(t, out float3 pos, out float3 tan, out float3 up);
            points.Add(pos);
        }

        // Compute bounds
        Bounds bounds = new Bounds(points[0], Vector3.zero);

        foreach (var p in points)
            bounds.Encapsulate(p);
        
        // Create BoxCollider matching bounds
        BoxCollider box = splineBoundsColliderHolder.AddComponent<BoxCollider>();
        box.center = bounds.center;
        box.size   = bounds.size;
        
        // The bounding collider Y pos is the lowest of all points
        if (scaleMethod == BoundingScaleMethod.LowestPoint)
        {
            float lowest = float.MaxValue;
            
            for (int i = 0; i < splineContainer.Spline.Knots.Count(); i++)
            {
                if (splineContainer.Spline[i].Position.y < lowest)
                {
                    lowest = splineContainer.Spline[i].Position.y;
                }
            }
            
            box.center = new Vector3(box.center.x, lowest, box.center.z);
        }
        // The Bounding collider Y pos is the highest of all points
        else if (scaleMethod == BoundingScaleMethod.HighestPoint)
        {
            float highest = float.MinValue;
            
            for (int i = 0; i < splineContainer.Spline.Knots.Count(); i++)
            {
                if (splineContainer.Spline[i].Position.y > highest)
                {
                    highest = splineContainer.Spline[i].Position.y;
                }
            }
            
            box.center = new Vector3(box.center.x, highest, box.center.z);
        }
        // The Bounding collider Y pos is the average of all points
        else if (scaleMethod == BoundingScaleMethod.Average)
        {
            float counter = 0;
            
            for (int i = 0; i < splineContainer.Spline.Knots.Count(); i++)
            {
                counter += splineContainer.Spline[i].Position.y;
            }
            
            box.center = new Vector3(box.center.x, counter / splineContainer.Spline.Knots.Count(), box.center.z);
        }
        
        ShowNewValue($"{box.center.y}", 2);
    }

    /// <summary>
    /// Create a delay with a counter for hiding the NewColliderYPos field
    /// </summary>
    private async void ShowNewValue(string value, int displayTimeSecs)
    {
        clickCounter++;
        NewColliderYPos = value;
        await Task.Delay(displayTimeSecs * 1000);
        clickCounter--;
    }
}
