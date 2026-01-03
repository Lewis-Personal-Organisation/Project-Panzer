using System;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

public class RenderChildrenComponent : MonoBehaviour
{
    public enum DrawShape
    {
        Sphere,
        Cube
    }
    public enum DrawShapeShadeType
    {
        Solid,
        Wireframe
    }
    public DrawShape drawShape = DrawShape.Sphere;
    public DrawShapeShadeType drawShadeType = DrawShapeShadeType.Solid;
    public float drawSize = 1f;
    public Color pointColour = Color.cyan;
    
    public bool drawLines = false;
    public Color lineColour = Color.green;
    public float lineHeight = 0.5f;
    
    public bool drawLabels;
    public Color textColour = Color.white;
    public int textSize = 12;
    public FontStyle textStyle = FontStyle.Normal;
    public float textHeight = 1f;
    public string textPrefix = "Point";
}

#if UNITY_EDITOR
[CustomEditor(typeof(RenderChildrenComponent))]
public class RenderChildComponentEditor : Editor
{
    private SerializedProperty drawShape;
    private SerializedProperty drawShapeShadeType;
    private SerializedProperty pointColour;
    private SerializedProperty drawSize;
    
    private SerializedProperty lineColour;
    private SerializedProperty lineHeight;
    
    private SerializedProperty drawLines;
    private SerializedProperty drawLabels;
    private SerializedProperty textSize;
    private SerializedProperty textStyle;
    private SerializedProperty textHeight;
    private SerializedProperty textColour;
    private SerializedProperty textPrefix;


    private void OnEnable()
    {
        drawShape = serializedObject.FindProperty("drawShape");
        drawShapeShadeType = serializedObject.FindProperty("drawShadeType");
        pointColour  = serializedObject.FindProperty("pointColour");
        drawSize  = serializedObject.FindProperty("drawSize");
        
        drawLines  = serializedObject.FindProperty("drawLines");
        lineColour    = serializedObject.FindProperty("lineColour");
        lineHeight = serializedObject.FindProperty("lineHeight");
        
        drawLabels  = serializedObject.FindProperty("drawLabels");
        textSize    = serializedObject.FindProperty("textSize");
        textStyle = serializedObject.FindProperty("textStyle");
        textHeight  = serializedObject.FindProperty("textHeight");
        textColour = serializedObject.FindProperty("textColour");
        textPrefix = serializedObject.FindProperty("textPrefix");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.LabelField("Point Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(drawShape);
        EditorGUILayout.PropertyField(drawShapeShadeType);
        EditorGUILayout.PropertyField(pointColour);
        EditorGUILayout.PropertyField(drawSize);
        
        GUILayout.Space(12);   
        EditorGUILayout.LabelField("Line Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(drawLines);      // Draw a serialised property
        if (drawLines.boolValue)
        {
            EditorGUILayout.PropertyField(lineColour);
            EditorGUILayout.PropertyField(lineHeight);
        }

        GUILayout.Space(12);   
        EditorGUILayout.LabelField("Label Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(drawLabels);      // Draw a serialised property
        if (drawLabels.boolValue)
        {
            EditorGUILayout.PropertyField(textPrefix);
            EditorGUILayout.PropertyField(textSize);
            EditorGUILayout.PropertyField(textStyle);
            EditorGUILayout.PropertyField(textHeight);
            EditorGUILayout.PropertyField(textColour);
            EditorGUILayout.PropertyField(textPrefix);
        }
        
        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// Handles drawing the Scene Handles/Elements
    /// </summary>
    private void OnSceneGUI()
    {
        RenderChildrenComponent script = (RenderChildrenComponent)target;
        
        Transform transform = script.transform;

        for (int i = 0; i < transform.childCount; i++)
        {
            Handles.zTest = CompareFunction.LessEqual;
            Handles.color = script.pointColour;

            if (script.drawShape == RenderChildrenComponent.DrawShape.Sphere)
            {
                if (script.drawShadeType == RenderChildrenComponent.DrawShapeShadeType.Solid)
                    Handles.SphereHandleCap(0, transform.GetChild(i).position, Quaternion.identity, script.drawSize, EventType.Repaint);
                else
                    DrawWireSphere(transform.GetChild(i).position, script.drawSize * 0.5F);
            }
            else
            {
                if (script.drawShadeType == RenderChildrenComponent.DrawShapeShadeType.Solid)
                    Handles.CubeHandleCap(0, transform.GetChild(i).position, Quaternion.identity, script.drawSize, EventType.Repaint);
                else 
                    Handles.DrawWireCube(transform.GetChild(i).position, Vector3.one * script.drawSize);
            }

            if (script.drawLines == true)
            {
                Handles.color = script.lineColour;
                // If we are not the last index, there must be another - draw to it
                Transform targTR = i != transform.childCount - 1 ? transform.GetChild(i + 1) : transform.GetChild(0);
                Handles.DrawLine(transform.GetChild(i).position + Vector3.up * script.lineHeight, targTR.position + Vector3.up * script.lineHeight);
            }
        }

        if (script.drawLabels)
        {
            Handles.zTest = CompareFunction.Always;

            GUIStyle labelStyle = new GUIStyle();
            labelStyle.normal.textColor = script.textColour; // Set your desired color
            labelStyle.fontSize = script.textSize;
            labelStyle.fontStyle = script.textStyle;

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform target = i != transform.childCount - 1 ? transform.GetChild(i + 1) : transform.GetChild(0);
                Handles.Label(target.position + Vector3.up * script.textHeight, $"{script.textPrefix} {i + 1}", labelStyle);
            }

            Handles.zTest = CompareFunction.LessEqual;
        }
    }
    
    void DrawWireSphere(Vector3 position, float radius)
    {
        // XY plane
        Handles.DrawWireDisc(position, Vector3.forward, radius);
        // XZ plane
        Handles.DrawWireDisc(position, Vector3.up, radius);
        // YZ plane
        Handles.DrawWireDisc(position, Vector3.right, radius);
    }
}
#endif

