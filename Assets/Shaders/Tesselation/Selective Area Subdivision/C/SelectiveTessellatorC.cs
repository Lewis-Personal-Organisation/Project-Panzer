using System;
using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

[RequireComponent(typeof(MeshFilter))]
public class SelectiveTessellatorC : MonoBehaviour
{
    struct VertexData
    {
        public Vector3 pos;
        public Vector2 uv;
    }

    public enum VertDropOptions
    {
        MaxDrop,
        Transform,
    }
    
    [FoldoutGroup("Tessellation Settings")] [Tooltip("The amount of subdivisions to apply when tessellating")]
    [FoldoutGroup("Tessellation Settings")] [Min(1)] public int tessellationPasses = 1;
    [FoldoutGroup("Tessellation Settings")] [Tooltip("Should triangles remain tessellated once out of the brush zone?")]
    public bool persistentTessellation = false;
    
    [FoldoutGroup("Tessellation Settings")][EnumToggleButtons]
    [Tooltip("If enabled, vertices fall to a point on a flat plane. If false, vertices drop an amount relative to their position")]
    public VertDropOptions vertDropSetting;
    [FormerlySerializedAs("flatBottomReference")]
    [FoldoutGroup("Tessellation Settings")][ShowIf("vertDropSetting", VertDropOptions.Transform)]
    [Tooltip("Optional. If set, this transform's world Y is used as the surface reference for the flat bottom; otherwise the brush is used.")]
    public Transform vertDropRefTransform;
    [FoldoutGroup("Tessellation Settings")][ShowIf("vertDropSetting", VertDropOptions.MaxDrop)]
    [Tooltip("The drop in height of vertices in standard units")]
    public float maxVertDrop = 0.2f;
    
    [FoldoutGroup("Brush Settings")] public TesselationBrushC brush;
    
    // Tracked values for rebuilding
    private int lastTessellationPasses;
    private bool lastPersistantTessellation;
    private int lastTessellationBrushID;
    private float lastMaxVertDrop;
    private int lastFlatBottomReferenceTransformID;
    private VertDropOptions lastVertDropSetting = VertDropOptions.MaxDrop;
    
    [Header("Compute Shader")]
    public ComputeShader tessShader;

    int kApply, kMark, kCount, kPrefix, kTess, kScatter;

    ComputeBuffer triBufferIn;
    ComputeBuffer triBufferOut;
    ComputeBuffer triBufferCompact;

    ComputeBuffer vertexBufferOut;
    ComputeBuffer outOriginalVerticesBuffer; // ADDED: undeformed positions for all output verts.
                                             // Filled by CSTessellate, read by CSApplyBrush
                                             // when deforming the full expanded vertex set.

    ComputeBuffer tessFlagsBuffer;
    ComputeBuffer persistentTessMask;

    ComputeBuffer countsBuffer;
    ComputeBuffer offsetsBuffer;

    ComputeBuffer persistentPositions;
    ComputeBuffer originalPositions;
    ComputeBuffer uvBuffer;
    ComputeBuffer passInputPositions;

    Mesh originalMesh;
    Mesh workingMesh;
    int baseTriangleCount;

    Vector3 tessCenter;
    public float tessRadius;


    void Awake()
    {
        var mf = GetComponent<MeshFilter>();

        originalMesh = Instantiate(mf.sharedMesh);
        workingMesh = mf.mesh;
        baseTriangleCount = originalMesh.triangles.Length / 3;

        kApply   = tessShader.FindKernel("CSApplyBrush");
        kMark    = tessShader.FindKernel("CSMarkTessellation");
        kCount   = tessShader.FindKernel("CSCountOutput");
        kPrefix  = tessShader.FindKernel("CSPrefixSum");
        kTess    = tessShader.FindKernel("CSTessellate");
        kScatter = tessShader.FindKernel("CSScatter");

        if (brush)
        {
            brush.tessellator = this;
        }
    }

    private void Update()
    {
        // Check whether a transform was assigned or deleted
        bool vertDropTrChanged = false;
        
        if (vertDropRefTransform)
        {
            if (lastFlatBottomReferenceTransformID != vertDropRefTransform.GetInstanceID())
            {
                lastFlatBottomReferenceTransformID = vertDropRefTransform.GetInstanceID();
                vertDropTrChanged = true;
            }
            else if (lastFlatBottomReferenceTransformID == -1)
            {
                vertDropSetting = VertDropOptions.Transform;
                lastFlatBottomReferenceTransformID = vertDropRefTransform.GetInstanceID();
                vertDropTrChanged = true;
            }
        }
        else
        {
            if (lastFlatBottomReferenceTransformID != -1)
            {
                vertDropSetting = VertDropOptions.MaxDrop;
                lastFlatBottomReferenceTransformID = -1;
                vertDropTrChanged = true;
            }
        }
        
        bool discardUpdate = !vertDropTrChanged && Mathf.Approximately(lastMaxVertDrop, maxVertDrop) && lastTessellationPasses == tessellationPasses && lastVertDropSetting == vertDropSetting &&
                                   lastPersistantTessellation == persistentTessellation && 
                                   (!brush || lastTessellationBrushID == brush.GetInstanceID());

        if (discardUpdate == false)
        {
            lastMaxVertDrop = maxVertDrop;
            lastTessellationPasses = tessellationPasses;
            lastVertDropSetting = vertDropSetting;
            lastTessellationBrushID = brush.GetInstanceID();
            lastPersistantTessellation = persistentTessellation;
        }
        
        if (brush.rebuildTessellation || discardUpdate == false)
        {
            InputDataChanged();
        }
    }

    private void InputDataChanged()
    {
        tessCenter = brush.transform.position;
        tessRadius = brush.brushTessellationRadius;

        RebuildTessellation();
    }

    void EnsureBuffersForMesh(Mesh mesh, bool managePersistentPositions, bool overwritePositions = false)
    {
        int vertCount = mesh.vertexCount;
        int triCount  = mesh.triangles.Length / 3;

        AllocateIfNeeded(ref triBufferIn,        triCount,     sizeof(int) * 3);
        AllocateIfNeeded(ref triBufferOut,        triCount * 4, sizeof(int) * 3);
        AllocateIfNeeded(ref triBufferCompact,    triCount * 4, sizeof(int) * 3);

        AllocateIfNeeded(ref vertexBufferOut,         triCount * 6, sizeof(float) * 5);

        AllocateIfNeeded(ref tessFlagsBuffer,    triCount,          sizeof(uint));
        AllocateIfNeeded(ref persistentTessMask, baseTriangleCount, sizeof(uint));

        AllocateIfNeeded(ref countsBuffer,  triCount, sizeof(uint));
        AllocateIfNeeded(ref offsetsBuffer, triCount, sizeof(uint));

        AllocateIfNeeded(ref uvBuffer, vertCount, sizeof(float) * 2);

        if (!managePersistentPositions) return;

        if (persistentPositions == null || persistentPositions.count != vertCount)
        {
            AllocateIfNeeded(ref persistentPositions, vertCount, sizeof(float) * 3);
            persistentPositions.SetData(mesh.vertices);
        }
        else if (overwritePositions)
        {
            persistentPositions.SetData(mesh.vertices);
        }

        if (originalPositions == null || originalPositions.count != vertCount)
        {
            AllocateIfNeeded(ref originalPositions, vertCount, sizeof(float) * 3);
            originalPositions.SetData(mesh.vertices);
        }
        else if (overwritePositions)
        {
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
        Mesh currentMesh = originalMesh;
        Mesh tempMesh = null;

        int passCount = Mathf.Max(1, tessellationPasses);
        for (int pass = 0; pass < passCount; pass++)
        {
            bool isFirstPass = pass == 0;
            bool isLastPass  = pass == passCount - 1;

            EnsureBuffersForMesh(currentMesh, managePersistentPositions: isFirstPass, overwritePositions: false);

            Vector3[] verts = currentMesh.vertices;
            Vector2[] uvs   = currentMesh.uv;
            int[]     tris  = currentMesh.triangles;
            int triCount    = tris.Length / 3;

            triBufferIn.SetData(tris);
            uvBuffer.SetData(uvs);

            ComputeBuffer inputPositions = persistentPositions;

            // Set all shared parameters once per pass.
            bool vertDropTREmpty = vertDropSetting == VertDropOptions.Transform && vertDropRefTransform == null;
            float vertMinYPos = vertDropSetting == VertDropOptions.MaxDrop || vertDropTREmpty ?
                Mathf.Approximately(maxVertDrop, 0F) ? float.MinValue : transform.position.y - maxVertDrop :
                vertDropRefTransform.position.y;

            tessShader.SetVector("_TessCenter",      tessCenter);
            tessShader.SetFloat("_TessRadius",       tessRadius);
            tessShader.SetFloat("_FlattenDepth",     brush.brushDepth);
            tessShader.SetFloat("_FlattenSpeed",     brush.flattenSpeed);
            tessShader.SetFloat("_MaxSnowDepth",     maxVertDrop);
            tessShader.SetFloat("_DeformRadius",     brush.brushDeformRadius);
            tessShader.SetMatrix("_LocalToWorld",    transform.localToWorldMatrix);
            tessShader.SetMatrix("_WorldToLocal",    transform.worldToLocalMatrix);
            tessShader.SetFloat("_FlatBottomWorldY", vertMinYPos);

            if (isFirstPass)
            {
                // Step 1: deform original mesh corners into persistentPositions.
                int vertCount = persistentPositions.count;
                int vGroups   = Mathf.CeilToInt(vertCount / 128f);

                tessShader.SetInt("_VertexCount",       vertCount);
                tessShader.SetInt("_UsePersistentMask", persistentTessellation ? 1 : 0);
                tessShader.SetBuffer(kApply, "_InVertices",       persistentPositions);
                tessShader.SetBuffer(kApply, "_OriginalVertices", originalPositions);
                tessShader.Dispatch(kApply, vGroups, 1, 1);
            }
            else
            {
                AllocateIfNeeded(ref passInputPositions, verts.Length, sizeof(float) * 3);
                passInputPositions.SetData(verts);
                inputPositions = passInputPositions;
            }

            int compactTriCount = RunSelectiveTessellation(
                triCount,
                usePersistentMask: isFirstPass && persistentTessellation,
                inputPositions);

            int finalVertOutCount = vertexBufferOut.count;
            VertexData[] vdata = new VertexData[finalVertOutCount];
            vertexBufferOut.GetData(vdata);

            Vector3[] finalVerts = new Vector3[finalVertOutCount];
            Vector2[] finalUVs   = new Vector2[finalVertOutCount];
            for (int i = 0; i < finalVertOutCount; i++)
            {
                finalVerts[i] = vdata[i].pos;
                finalUVs[i]   = vdata[i].uv;
            }

            int[] finalTris = new int[compactTriCount * 3];
            triBufferCompact.GetData(finalTris);

            if (isLastPass)
            {
                workingMesh.Clear();
                workingMesh.vertices  = finalVerts;
                workingMesh.uv        = finalUVs;
                workingMesh.triangles = finalTris;
                workingMesh.RecalculateNormals();
                workingMesh.RecalculateTangents();
                workingMesh.RecalculateBounds();
            }
            else
            {
                if (tempMesh == null) tempMesh = new Mesh();
                tempMesh.Clear();
                tempMesh.vertices  = finalVerts;
                tempMesh.uv        = finalUVs;
                tempMesh.triangles = finalTris;
                currentMesh = tempMesh;
            }
        }

        if (tempMesh != null) 
            Destroy(tempMesh);
    }

    int RunSelectiveTessellation(int triCount, bool usePersistentMask, ComputeBuffer inputPositions)
    {
        tessShader.SetInt("_TriangleCount",       triCount);
        tessShader.SetVector("_TessCenter",       tessCenter);
        tessShader.SetFloat("_TessRadius",        tessRadius);
        tessShader.SetFloat("_FlattenDepth",      brush.brushDepth);
        tessShader.SetFloat("_FlattenSpeed",      brush.flattenSpeed);
        tessShader.SetFloat("_MaxSnowDepth",      maxVertDrop);
        tessShader.SetFloat("_DeformRadius",      brush.brushDeformRadius);
        tessShader.SetInt("_UsePersistentMask",   usePersistentMask ? 1 : 0);
        tessShader.SetInt("_PersistentMaskCount", persistentTessMask != null ? persistentTessMask.count : 0);
        tessShader.SetMatrix("_LocalToWorld",     transform.localToWorldMatrix);

        int groups = Mathf.CeilToInt(triCount / 128f);

        // MARK
        tessShader.SetBuffer(kMark, "_InVertices",     inputPositions);
        tessShader.SetBuffer(kMark, "_InTriangles",    triBufferIn);
        tessShader.SetBuffer(kMark, "_TessFlags",      tessFlagsBuffer);
        tessShader.SetBuffer(kMark, "_PersistentMask", persistentTessMask);
        tessShader.Dispatch(kMark, groups, 1, 1);

        // COUNT
        tessShader.SetBuffer(kCount, "_TessFlags", tessFlagsBuffer);
        tessShader.SetBuffer(kCount, "_Counts",    countsBuffer);
        tessShader.Dispatch(kCount, groups, 1, 1);

        // PREFIX SUM
        tessShader.SetBuffer(kPrefix, "_Counts",  countsBuffer);
        tessShader.SetBuffer(kPrefix, "_Offsets", offsetsBuffer);
        tessShader.Dispatch(kPrefix, 1, 1, 1);

        uint[] lastOffset = new uint[1];
        uint[] lastCount  = new uint[1];
        offsetsBuffer.GetData(lastOffset, 0, triCount - 1, 1);
        countsBuffer.GetData (lastCount,  0, triCount - 1, 1);
        int compactTriCount = (int)(lastOffset[0] + lastCount[0]);

        // TESSELLATE — also fills _OutOriginalVertices.
        tessShader.SetBuffer(kTess, "_InVertices",           inputPositions);
        tessShader.SetBuffer(kTess, "_InTriangles",          triBufferIn);
        tessShader.SetBuffer(kTess, "_TessFlags",            tessFlagsBuffer);
        tessShader.SetBuffer(kTess, "_InUVs",                uvBuffer);
        tessShader.SetBuffer(kTess, "_OutVertices",          vertexBufferOut);
        tessShader.SetBuffer(kTess, "_OutTriangles",         triBufferOut);
        tessShader.Dispatch(kTess, groups, 1, 1);

        // SCATTER
        tessShader.SetBuffer(kScatter, "_OutTriangles",        triBufferOut);
        tessShader.SetBuffer(kScatter, "_Offsets",             offsetsBuffer);
        tessShader.SetBuffer(kScatter, "_TessFlags",           tessFlagsBuffer);
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
        passInputPositions?.Release();
    }
}
