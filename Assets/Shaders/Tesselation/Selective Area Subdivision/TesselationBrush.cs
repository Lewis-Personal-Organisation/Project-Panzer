using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

/// <summary>
/// Solid transparent sphere showing brush / effector bounds. Attach to the same GameObject as the brush transform
/// referenced by <see cref="SelectiveTessellator"/>. The tessellator calls <see cref="DrawTessRadius"/> each frame.
/// </summary>
[DisallowMultipleComponent]
public class TesselationBrush : MonoBehaviour
{
    public SelectiveTessellator tessellator;
    
    [Title("Brush")]
    [FormerlySerializedAs("brushRadius")]
    [FoldoutGroup("Brush Settings")] public float brushTessellationRadius = 0.5f;
    [FoldoutGroup("Brush Settings")] public float brushDeformRadius = 0.5f;
    [FoldoutGroup("Brush Settings")] public float brushDepth = 0.1f;
    
    private float lastBrushTessellationRadius;
    private float lastBrushDeformRadius;
    private float lastBrushDepth;
    
    [FormerlySerializedAs("sphereRenderer")]
    [SerializeField] MeshRenderer tessellationSphereRenderer;
    [SerializeField]
    private Transform tessellationSphereTransform;
    
    [SerializeField] MeshRenderer deformSphereRenderer;
    [SerializeField]
    private Transform deformSphereTransform;

    private Vector3 lastTransformPosition;
    [HideInInspector] public bool rebuildTessellation = false;
    
    
    [Space(5)]
    [Header("Brush Visuals (Game View)")]
    [FoldoutGroup("Brush Visuals")] public bool showBrushIndicator = true;
    [FormerlySerializedAs("brushIndicatorColor")]
    [FoldoutGroup("Brush Visuals")] public Color brushTesselationIndicatorColor = new Color(0.2f, 0.8f, 1f, 0.35f);
    [FoldoutGroup("Brush Visuals")] public Color brushDeformationIndicatorColor = new Color(0.2f, 0.8f, 1f, 0.35f);
    [FoldoutGroup("Brush Visuals")] [Min(0.01F)] public float brushIndicatorWidth = 0.02f;
    [FoldoutGroup("Brush Visuals")] public bool includeIndicatorWidthInEffectRadius = true;
    [FoldoutGroup("Brush Visuals")] public bool showBrushDepthIndicator = false;
    [FoldoutGroup("Brush Visuals")] public Color brushDepthColor = new Color(1f, 0.35f, 0.35f, 0.9f);
    [FoldoutGroup("Brush Visuals")] public Color brushMaxDepthColor = new Color(1f, 0.85f, 0.2f, 0.9f);
    [FoldoutGroup("Brush Visuals")] [Min(0f)] public float brushDepthIndicatorWidth = 0.02f;

    
    private void Awake()
    {
        deformSphereRenderer.shadowCastingMode = ShadowCastingMode.Off;
        deformSphereRenderer.receiveShadows = false;
        deformSphereRenderer.lightProbeUsage = LightProbeUsage.Off;
        deformSphereRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        
        tessellationSphereRenderer.shadowCastingMode = ShadowCastingMode.Off;
        tessellationSphereRenderer.receiveShadows = false;
        tessellationSphereRenderer.lightProbeUsage = LightProbeUsage.Off;
        tessellationSphereRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    private void Update()
    {
        if (transform.position != lastTransformPosition)
        {
            lastTransformPosition = transform.position;
            rebuildTessellation = true;
        }
        else if (!Mathf.Approximately(lastBrushDepth, brushDepth))
        {
            lastBrushDepth = brushDepth;
            rebuildTessellation = true;
        }
        else if (!Mathf.Approximately(lastBrushTessellationRadius, brushTessellationRadius))
        {
            lastBrushTessellationRadius = brushTessellationRadius;
            rebuildTessellation = true;
        }
        else if (!Mathf.Approximately(lastBrushDeformRadius, brushDeformRadius))
        {
            lastBrushDeformRadius = brushDeformRadius;
            rebuildTessellation = true;
        }

        if (rebuildTessellation)
        {
            UpdateBrushIndicator();
        }
    }


    /// <param name="worldRadius">World-space radius (should match tessellation effect radius).</param>
    /// <param name="color">Tint and alpha.</param>
    /// <param name="visible"></param>
    public void DrawTessRadius(float worldRadius, Color color, bool visible)
    {
        tessellationSphereRenderer.enabled = visible && worldRadius > 0f;
        if (!tessellationSphereRenderer.enabled)
            return;

        tessellationSphereTransform.position = this.transform.position;
        
        // Unity sphere mesh has radius 0.5 in local space at scale 1.
        float uniform = Mathf.Max(0f, worldRadius) * 2f;
        tessellationSphereTransform.localScale = new Vector3(uniform, uniform, uniform);

        tessellationSphereRenderer.sharedMaterial.color = color;

        if (tessellationSphereRenderer.sharedMaterial.HasProperty("_BaseColor"))
            tessellationSphereRenderer.sharedMaterial.SetColor("_BaseColor", color);
        if (tessellationSphereRenderer.sharedMaterial.HasProperty("_Color"))
            tessellationSphereRenderer.sharedMaterial.SetColor("_Color", color);
    }

    public void DrawDeformRadius(float worldRadius, Color color, bool visible)
    {
        deformSphereRenderer.enabled = visible && worldRadius > 0f;
        if (!deformSphereRenderer.enabled)
            return;

        deformSphereTransform.position = this.transform.position;
        
        // Unity sphere mesh has radius 0.5 in local space at scale 1.
        float uniform = Mathf.Max(0f, worldRadius) * 2f;
        deformSphereTransform.localScale = new Vector3(uniform, uniform, uniform);

        deformSphereRenderer.sharedMaterial.color = color;

        if (deformSphereRenderer.sharedMaterial.HasProperty("_BaseColor"))
            deformSphereRenderer.sharedMaterial.SetColor("_BaseColor", color);
        if (deformSphereRenderer.sharedMaterial.HasProperty("_Color"))
            deformSphereRenderer.sharedMaterial.SetColor("_Color", color);
    }
    
    void UpdateBrushIndicator()
    {
        // var brushVisual = brush != null ? brush.GetComponent<TesselationBrush>() : null;
        bool showSphere = showBrushIndicator && brushTessellationRadius > 0f;
            DrawTessRadius(tessellator.tessRadius, brushTesselationIndicatorColor, showSphere);
            DrawDeformRadius(brushDeformRadius, brushDeformationIndicatorColor, showSphere);
    }
}
