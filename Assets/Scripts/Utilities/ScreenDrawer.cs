using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ScreenDrawer : Singleton<ScreenDrawer>
{
    [SerializeField]
    private Canvas renderCanvas;
    [SerializeField] private RectTransform canvasRectTransform;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float lineWidth = 1F;
    
    public Vector2 CanvasSize => canvasRectTransform.rect.size;
    public float StartWidth => lineRenderer.startWidth;

    [SerializeField] List<Vector3> points = new List<Vector3>();
    
    public void SetLineWidth(float width) => lineRenderer.widthMultiplier = width;
    public void SetColourGradient(Gradient colourGradient)  => lineRenderer.colorGradient = colourGradient;
    public void SetMaterialInt(string key, int value) => lineRenderer.material.SetInt(key, value);
    public void SetRenderQueue(int value) => lineRenderer.material.renderQueue = value;

    
#if UNITY_EDITOR
    private void Reset()
    {
        EnsureComponents();
    }

    private void OnEnable()
    {
        EnsureComponents();
    }
    
    private void EnsureComponents()
    {
        if (!Instance)
            base.Awake();
        
        if (renderCanvas == null)
            renderCanvas = GetComponent<Canvas>();
        
        if (canvasRectTransform == null) 
            canvasRectTransform =  renderCanvas.GetComponent<RectTransform>();
        
        if (lineRenderer != null) return;
        
        lineRenderer = GetComponent<LineRenderer>();
        if (!lineRenderer)
        {
            lineRenderer = new GameObject("Line Renderer", typeof(RectTransform), typeof(LineRenderer)).GetComponent<LineRenderer>();
            lineRenderer.transform.SetParent(renderCanvas.transform, false);
        }

        lineRenderer.useWorldSpace = false;
        lineRenderer.alignment = LineAlignment.TransformZ;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.sortingOrder = 1000;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(gameObject);
            EditorUtility.SetDirty(lineRenderer);
        }
    }
    
    public void Draw(params Vector3[] positions)
    {
        // Width is 1 if other values are below 0
        lineRenderer.widthMultiplier = lineWidth > 0F ? lineWidth : 1F;
        lineRenderer.positionCount = positions.Length;
        
        for (int i = 0; i < lineRenderer.positionCount; i++)
        {
            lineRenderer.SetPosition(i, positions[i]);
        }
    }

    public void DrawAsCanvasPercentage(params Vector3[] percentages)
    {
        // Width is 1 if other values are below 0
        lineRenderer.widthMultiplier = lineWidth > 0F ? lineWidth : 1F;
        lineRenderer.positionCount = percentages.Length;
        
        float startX = -Screen.width / 2;       // left edge point
        float StartY =  -Screen.height / 2;     // bottom Edge point
        
        for (int i = 0; i < lineRenderer.positionCount; i++)
        {
            lineRenderer.SetPosition(i, new Vector3(startX + percentages[i].x * Screen.width * 0.01F, StartY + percentages[i].y * Screen.height * 0.01F));
        }
    }
#endif
}
