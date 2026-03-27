using System;
using UnityEngine;
using UnityEngine.Serialization;

public enum DrawShape
{
    Sphere,
    Box
}

struct VertexData
{
    public Vector3 pos;
    public Vector2 uv;
}

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Tessellator : MonoBehaviour
{
    const int THREADS_PER_GROUP = 128;
    
    public ComputeShader tessShader;
    [FormerlySerializedAs("TessLevel")]
    [Range(0, 6)] public int tessLevel = 1;
    public Material tessMaterial;

    Mesh sourceMesh;
    Mesh tessMesh;

    [Header("Brush")]
    [SerializeField] private DrawShape shape;
    public float radius = 0.5f;
    public float strength = 0.1f;
    public bool useFalloff = true;
    public Material brushMaterial;

    public float maxVertDrop = 0F;
    public float vertDrop = 0F;
    
    struct uint3 { public uint x, y, z; }

    private int InstID;
    private Vector3 hitPoint;
    public float avg = 0;
    
    // Persistent buffers
    ComputeBuffer vertexBufferA;
    ComputeBuffer vertexBufferB;
    ComputeBuffer triBufferA;
    ComputeBuffer triBufferB;
    
    // Current counts
    int currentVertCount;
    int currentTriCount;

    // Kernels
    int kernelCSMain;
    
    
    void Start()
    {
        InstID = this.gameObject.GetInstanceID();
        
        if (tessShader != null)
            kernelCSMain = tessShader.FindKernel("CSMain");
        Debug.Log("kernelCSMain = " + kernelCSMain);
        var mr = GetComponent<MeshRenderer>();
        if (mr != null && tessMaterial != null)
            mr.material = tessMaterial;

        sourceMesh = GetComponent<MeshFilter>().sharedMesh;
        tessMesh = new Mesh();
        tessMesh.name = "TessellatedMesh";

        GetComponent<MeshFilter>().mesh = tessMesh;

        RebuildTessellation();
        
        avg = GetAverageHeight();
        maxVertDrop = avg - vertDrop;
    }

    private void Update()
    {
        TessellatorBrush();
    }
    
    void OnRenderObject()
    {
        if (!brushMaterial) return;

        brushMaterial.SetPass(0);

        GL.PushMatrix();
        GL.MultMatrix(Matrix4x4.identity);

        if (shape == DrawShape.Sphere)
        {
            GL.Begin(GL.LINE_STRIP);
            GL.Color(Color.red);

            int segments = 64;
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                Vector3 worldPos = hitPoint + offset;
                GL.Vertex(worldPos);
            }
        }
        else
        {
            GL.Begin(GL.LINES);
            GL.Color(Color.red);

            Vector3 h = new Vector3(radius, radius, radius);

            // 8 cube corners
            Vector3[] v = new Vector3[8]
            {
                hitPoint + new Vector3(-h.x, -h.y, -h.z),
                hitPoint + new Vector3( h.x, -h.y, -h.z),
                hitPoint + new Vector3( h.x, -h.y,  h.z),
                hitPoint + new Vector3(-h.x, -h.y,  h.z),

                hitPoint + new Vector3(-h.x,  h.y, -h.z),
                hitPoint + new Vector3( h.x,  h.y, -h.z),
                hitPoint + new Vector3( h.x,  h.y,  h.z),
                hitPoint + new Vector3(-h.x,  h.y,  h.z)
            };

            // 12 edges (24 vertices)
            int[] edges = {
                0,1, 1,2, 2,3, 3,0, // bottom square
                4,5, 5,6, 6,7, 7,4, // top square
                0,4, 1,5, 2,6, 3,7  // vertical edges
            };

            for (int i = 0; i < edges.Length; i += 2)
            {
                GL.Vertex(v[edges[i]]);
                GL.Vertex(v[edges[i + 1]]);
            }
        }

        GL.End();
        GL.PopMatrix();
    }
    
    float GetAverageHeight()
    {
        if (tessMesh == null) return 0f;

        Vector3[] verts = tessMesh.vertices;

        float sum = 0f;
        for (int i = 0; i < verts.Length; i++)
            sum += verts[i].y;

        return sum / verts.Length;
    }

    private void TessellatorBrush()
    {
        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform.gameObject.GetInstanceID() == InstID)
                {
                    SculptAtPoint(hit.point, radius, strength);
                    hitPoint = hit.point;
                }
            }
            else
            {
                hitPoint = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            }
        }
    }

    void OnValidate()
    {
        if (Application.isPlaying)
            RebuildTessellation();
    }

    void EnsureBuffersForMesh(Mesh sourceMesh)
    {
        int baseVertCount = sourceMesh.vertexCount;
        int baseTriCount  = sourceMesh.triangles.Length / 3;

        int maxTriCount  = baseTriCount;
        int maxVertCount = baseVertCount;

        for (int i = 0; i < tessLevel; i++)
        {
            maxTriCount *= 4;  // each tri → 4
            maxVertCount *= 6; // each tri → 6 verts
        }

        int vertexStride = sizeof(float) * (3 + 2); // float3 + float2

        AllocateIfNeeded(ref vertexBufferA, maxVertCount, vertexStride);
        AllocateIfNeeded(ref vertexBufferB, maxVertCount, vertexStride);
        AllocateIfNeeded(ref triBufferA,    maxTriCount,  sizeof(int) * 3);
        AllocateIfNeeded(ref triBufferB,    maxTriCount,  sizeof(int) * 3);
    }

    void AllocateIfNeeded(ref ComputeBuffer buffer, int count, int stride)
    {
        if (buffer != null && buffer.count >= count && buffer.stride == stride)
            return;

        if (buffer != null)
            buffer.Release();

        buffer = new ComputeBuffer(count, stride);
    }
    
    void RebuildTessellation()
    {
        if (sourceMesh == null || tessShader == null) return;

        // Ensure persistent buffers are large enough
        EnsureBuffersForMesh(sourceMesh);

        // Load original mesh data
        Vector3[] verts = sourceMesh.vertices;
        Vector2[] uvs   = sourceMesh.uv;
        int[] tris      = sourceMesh.triangles;

        currentVertCount = verts.Length;
        currentTriCount  = tris.Length / 3;

        // Pack into VertexData[]
        VertexData[] vData = new VertexData[currentVertCount];
        for (int i = 0; i < currentVertCount; i++)
        {
            vData[i].pos = verts[i];
            vData[i].uv  = (i < uvs.Length) ? uvs[i] : Vector2.zero;
        }

        // Upload to A
        vertexBufferA.SetData(vData);
        triBufferA.SetData(tris);

        ComputeBuffer inVert  = vertexBufferA;
        ComputeBuffer inTri   = triBufferA;
        ComputeBuffer outVert = vertexBufferB;
        ComputeBuffer outTri  = triBufferB;

        int level = Mathf.Max(0, tessLevel);

        for (int i = 0; i < level; i++)
        {
            SubdivideOnceGPU_KeepOnGPU(
                inVert, inTri,
                outVert, outTri,
                ref currentVertCount,
                ref currentTriCount
            );

            // Swap buffers
            (inVert,  outVert) = (outVert, inVert);
            (inTri,   outTri)  = (outTri, inTri);
        }

        // Read back final mesh from "in" buffers
        VertexData[] finalVData = new VertexData[currentVertCount];
        int[] finalTris      = new int[currentTriCount * 3];
        
        inVert.GetData(finalVData, 0, 0, currentVertCount);
        inTri.GetData(finalTris, 0, 0, currentTriCount * 3);
        
        Vector3[] finalVerts = new Vector3[currentVertCount];
        Vector2[] finalUVs   = new Vector2[currentVertCount];

        for (int i = 0; i < currentVertCount; i++)
        {
            finalVerts[i] = finalVData[i].pos;
            finalUVs[i]   = finalVData[i].uv;
        }
        
        // Apply to Unity mesh
        tessMesh.Clear();
        tessMesh.vertices  = finalVerts;
        tessMesh.uv        = finalUVs;
        tessMesh.triangles = finalTris;
        tessMesh.RecalculateNormals();
        tessMesh.RecalculateBounds();
    }

    /// <summary>
    /// Subdivides the vertices for the mesh, once.
    /// </summary>
    /// <param name="verts"></param>
    /// <param name="uvs"></param>
    /// <param name="tris"></param>
    void SubdivideOnceGPU_KeepOnGPU(
        ComputeBuffer vertIn,
        ComputeBuffer triIn,
        ComputeBuffer vertOut,
        ComputeBuffer triOut,
        ref int vertCount,
        ref int triCount)
    {
        if (triCount == 0) return;

        int outTriCount  = triCount * 4;
        int outVertCount = triCount * 6;

        // Bind input buffers
        tessShader.SetBuffer(kernelCSMain, "_InVertices",  vertIn);
        tessShader.SetBuffer(kernelCSMain, "_InTriangles", triIn);

        // Bind output buffers
        tessShader.SetBuffer(kernelCSMain, "_OutVertices",  vertOut);
        tessShader.SetBuffer(kernelCSMain, "_OutTriangles", triOut);

        tessShader.SetInt("_TriangleCount", triCount);

        int groups = Mathf.Max(1, Mathf.CeilToInt((float)triCount / THREADS_PER_GROUP));
        tessShader.Dispatch(kernelCSMain, groups, 1, 1);

        // Update counts for next pass
        vertCount = outVertCount;
        triCount  = outTriCount;
    }
    
    public void SculptAtPoint(Vector3 worldPoint, float radius, float strength)
    {
        if (tessMesh == null) return;

        Vector3[] verts = tessMesh.vertices;
        // Vector3[] normals = tessMesh.normals;

        Matrix4x4 worldToLocal = transform.worldToLocalMatrix;
        Vector3 localPoint = worldToLocal.MultiplyPoint(worldPoint);

        if (shape == DrawShape.Sphere)
        {
            for (int i = 0; i < verts.Length; i++)
            {
                float dist = Vector3.Distance(verts[i], localPoint);
                if (dist > radius) continue;

                float falloff = 1f - (dist / radius);

                if (useFalloff)
                    falloff = falloff * falloff; // smooth falloff

                // Move vertex down
                Vector3 localUp = transform.InverseTransformDirection(Vector3.up);
                verts[i] += localUp * (strength * falloff);

                if (verts[i].y < maxVertDrop)
                    verts[i].y = maxVertDrop;
            }
        }
        else
        {
            // Box half‑size
            Vector3 halfSize = new Vector3(radius, radius, radius);
            
            // Move direction (down)
            Vector3 localUp = transform.InverseTransformDirection(Vector3.up);

            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 delta = verts[i] - localPoint;

                // Box bounds check
                if (Mathf.Abs(delta.x) > halfSize.x ||
                    Mathf.Abs(delta.y) > halfSize.y ||
                    Mathf.Abs(delta.z) > halfSize.z)
                    continue;

                // Box falloff
                float fx = 1f - Mathf.Abs(delta.x) / halfSize.x;
                float fy = 1f - Mathf.Abs(delta.y) / halfSize.y;
                float fz = 1f - Mathf.Abs(delta.z) / halfSize.z;

                float falloff = fx * fy * fz;
                if (useFalloff)
                    falloff *= falloff;

                // Apply sculpt
                verts[i] += localUp * (strength * falloff);

                // Clamp to dynamic floor
                if (verts[i].y < maxVertDrop)
                    verts[i].y = maxVertDrop;
            }
        }

        tessMesh.vertices = verts;
        tessMesh.RecalculateNormals();
        tessMesh.RecalculateBounds();
    }
}