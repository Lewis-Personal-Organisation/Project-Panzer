using System;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
public class SelectiveTessellator : MonoBehaviour
{
    struct VertexData
    {
        public Vector3 pos;
        public Vector2 uv;
    }

    [Header("Brush")]
    public Transform brush;
    public float brushRadius = 0.5f;
    public float brushDepth = 0.1f;
    public float maxSnowDepth = 0.2f;

    [Header("Compute Shader")]
    public ComputeShader tessShader;

    int kApply, kMark, kCount, kPrefix, kTess, kScatter;

    ComputeBuffer triBufferIn;
    ComputeBuffer triBufferOut;
    ComputeBuffer triBufferCompact;

    ComputeBuffer vertexBufferOut;

    ComputeBuffer tessFlagsBuffer;
    ComputeBuffer persistentTessMask;

    ComputeBuffer countsBuffer;
    ComputeBuffer offsetsBuffer;

    ComputeBuffer persistentPositions;   // deformable snow surface
    ComputeBuffer originalPositions;     // fixed reference for max drop
    ComputeBuffer uvBuffer;

    Mesh originalMesh;
    Mesh workingMesh;

    Vector3 tessCenter;
    float tessRadius;

    void Awake()
    {
        var mf = GetComponent<MeshFilter>();

        originalMesh = Instantiate(mf.sharedMesh);
        workingMesh = mf.mesh;

        kApply   = tessShader.FindKernel("CSApplyBrush");
        kMark    = tessShader.FindKernel("CSMarkTessellation");
        kCount   = tessShader.FindKernel("CSCountOutput");
        kPrefix  = tessShader.FindKernel("CSPrefixSum");
        kTess    = tessShader.FindKernel("CSTessellate");
        kScatter = tessShader.FindKernel("CSScatter");
    }

    void Update()
    {
        if (!brush) return;

        tessCenter = transform.InverseTransformPoint(brush.position);
        tessRadius = brushRadius;

        RebuildTessellation();
    }

    void EnsureBuffersForMesh(Mesh mesh)
    {
        int vertCount = mesh.vertexCount;
        int triCount  = mesh.triangles.Length / 3;

        AllocateIfNeeded(ref triBufferIn, triCount, sizeof(int) * 3);
        AllocateIfNeeded(ref triBufferOut, triCount * 4, sizeof(int) * 3);
        AllocateIfNeeded(ref triBufferCompact, triCount * 4, sizeof(int) * 3);

        AllocateIfNeeded(ref vertexBufferOut, triCount * 6, sizeof(float) * 5);

        AllocateIfNeeded(ref tessFlagsBuffer, triCount, sizeof(uint));
        AllocateIfNeeded(ref persistentTessMask, triCount, sizeof(uint));

        AllocateIfNeeded(ref countsBuffer, triCount, sizeof(uint));
        AllocateIfNeeded(ref offsetsBuffer, triCount, sizeof(uint));

        AllocateIfNeeded(ref uvBuffer, vertCount, sizeof(float) * 2);

        if (persistentPositions == null)
        {
            AllocateIfNeeded(ref persistentPositions, vertCount, sizeof(float) * 3);
            persistentPositions.SetData(mesh.vertices);
        }

        if (originalPositions == null)
        {
            AllocateIfNeeded(ref originalPositions, vertCount, sizeof(float) * 3);
            originalPositions.SetData(mesh.vertices);
        }
    }

    void AllocateIfNeeded(ref ComputeBuffer buffer, int count, int stride)
    {
        if (buffer == null || buffer.count != count)
        {
            buffer?.Release();
            buffer = new ComputeBuffer(count, stride);
        }
    }

    void RebuildTessellation()
    {
        Mesh mesh = originalMesh;

        EnsureBuffersForMesh(mesh);

        Vector3[] verts = mesh.vertices;
        Vector2[] uvs   = mesh.uv;
        int[] tris      = mesh.triangles;

        int triCount = tris.Length / 3;

        triBufferIn.SetData(tris);
        uvBuffer.SetData(uvs);

        // 1) Apply brush to persistent base vertices (persistent + max drop)
        int vertCount = persistentPositions.count;
        int vGroups   = Mathf.CeilToInt(vertCount / 128f);

        tessShader.SetInt("_VertexCount", vertCount);
        tessShader.SetVector("_TessCenter", tessCenter);
        tessShader.SetFloat("_TessRadius", tessRadius);
        tessShader.SetFloat("_FlattenDepth", brushDepth);
        tessShader.SetFloat("_MaxSnowDepth", maxSnowDepth);

        tessShader.SetBuffer(kApply, "_InVertices", persistentPositions);
        tessShader.SetBuffer(kApply, "_OriginalVertices", originalPositions);
        tessShader.Dispatch(kApply, vGroups, 1, 1);

        // 2) Run tessellation pipeline on updated persistentPositions
        int compactTriCount = RunSelectiveTessellation(triCount);

        int vertOutCount = vertexBufferOut.count;

        VertexData[] vdata = new VertexData[vertOutCount];
        vertexBufferOut.GetData(vdata);

        Vector3[] finalVerts = new Vector3[vertOutCount];
        Vector2[] finalUVs   = new Vector2[vertOutCount];

        for (int i = 0; i < vertOutCount; i++)
        {
            finalVerts[i] = vdata[i].pos;
            finalUVs[i]   = vdata[i].uv;
        }

        int[] finalTris = new int[compactTriCount * 3];
        triBufferCompact.GetData(finalTris);

        workingMesh.Clear();
        workingMesh.vertices  = finalVerts;
        workingMesh.uv        = finalUVs;
        workingMesh.triangles = finalTris;

        workingMesh.RecalculateNormals();
        workingMesh.RecalculateTangents();
        workingMesh.RecalculateBounds();
    }

    int RunSelectiveTessellation(int triCount)
    {
        tessShader.SetInt("_TriangleCount", triCount);
        tessShader.SetVector("_TessCenter", tessCenter);
        tessShader.SetFloat("_TessRadius", tessRadius);
        tessShader.SetFloat("_FlattenDepth", brushDepth);
        tessShader.SetFloat("_MaxSnowDepth", maxSnowDepth);

        int groups = Mathf.CeilToInt(triCount / 128f);

        // MARK
        tessShader.SetBuffer(kMark, "_InVertices", persistentPositions);
        tessShader.SetBuffer(kMark, "_InTriangles", triBufferIn);
        tessShader.SetBuffer(kMark, "_TessFlags", tessFlagsBuffer);
        tessShader.SetBuffer(kMark, "_PersistentMask", persistentTessMask);
        tessShader.Dispatch(kMark, groups, 1, 1);

        // COUNT
        tessShader.SetBuffer(kCount, "_TessFlags", tessFlagsBuffer);
        tessShader.SetBuffer(kCount, "_Counts", countsBuffer);
        tessShader.Dispatch(kCount, groups, 1, 1);

        // PREFIX SUM
        tessShader.SetBuffer(kPrefix, "_Counts", countsBuffer);
        tessShader.SetBuffer(kPrefix, "_Offsets", offsetsBuffer);
        tessShader.Dispatch(kPrefix, 1, 1, 1);

        uint[] lastOffset = new uint[1];
        uint[] lastCount  = new uint[1];

        offsetsBuffer.GetData(lastOffset, 0, triCount - 1, 1);
        countsBuffer.GetData(lastCount,  0, triCount - 1, 1);

        int compactTriCount = (int)(lastOffset[0] + lastCount[0]);

        // TESSELLATE (no extra flatten here; vertices already deformed)
        tessShader.SetBuffer(kTess, "_InVertices", persistentPositions);
        tessShader.SetBuffer(kTess, "_InTriangles", triBufferIn);
        tessShader.SetBuffer(kTess, "_TessFlags", tessFlagsBuffer);
        tessShader.SetBuffer(kTess, "_InUVs", uvBuffer);
        tessShader.SetBuffer(kTess, "_OutVertices", vertexBufferOut);
        tessShader.SetBuffer(kTess, "_OutTriangles", triBufferOut);
        tessShader.Dispatch(kTess, groups, 1, 1);

        // SCATTER
        tessShader.SetBuffer(kScatter, "_OutTriangles", triBufferOut);
        tessShader.SetBuffer(kScatter, "_Offsets", offsetsBuffer);
        tessShader.SetBuffer(kScatter, "_TessFlags", tessFlagsBuffer);
        tessShader.SetBuffer(kScatter, "_OutTrianglesCompact", triBufferCompact);
        tessShader.Dispatch(kScatter, groups, 1, 1);

        return compactTriCount;
    }

    void OnDestroy()
    {
        triBufferIn?.Release();
        triBufferOut?.Release();
        triBufferCompact?.Release();
        vertexBufferOut?.Release();
        tessFlagsBuffer?.Release();
        persistentTessMask?.Release();
        countsBuffer?.Release();
        offsetsBuffer?.Release();
        persistentPositions?.Release();
        originalPositions?.Release();
        uvBuffer?.Release();
    }
}