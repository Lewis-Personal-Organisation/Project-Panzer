using UnityEditor;
using UnityEngine;

public static class SceneGUI
{
    private static bool enabled = false;
    private const float baseSpace = 10F;
    private static float multi;
    private static float activeSpacing = baseSpace;
    // private static void IncrementSpacing() => activeSpacing += baseSpace + spacing;
    private static void IncrementSpacing(float space = baseSpace) => activeSpacing += space;
    private static void ResetSpacing() => activeSpacing = baseSpace;
    
    // Auto enable Scene view onGUI after scene starts
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoEnable()
    {
        Enable();
    }

    /// <summary>
    /// Enables scene rendering by subscribing to the duringSceneGui event
    /// </summary>
    [MenuItem("Scene Overlay/Enable")]
    public static void Enable()
    {
        if (enabled) return;
        
        enabled = true;
        SceneView.duringSceneGui += OnScene;
    }
 
    /// <summary>
    /// Disables scene rendering by unsubscribing from the duringSceneGui event
    /// </summary>
    [MenuItem("Scene Overlay/Disable")]
    public static void Disable()
    {
        if (!enabled) return;
        
        enabled = false;
        SceneView.duringSceneGui -= OnScene;
    }
 
    /// <summary>
    /// The main rending loop for displaying data
    /// </summary>
    /// <param name="sceneView"></param>
    private static void OnScene(SceneView sceneView)
    {
        Handles.BeginGUI();
        
        // Draw requests
        Draw();
        
        Handles.EndGUI();
    }

    /// <summary>
    /// Draw the list of all labels
    /// </summary>
    private static void Draw()
    {
        // Debug.Log($"Labels Drawn (Fixed): {SceneData.labelsFixed.Count}");
        // Draw Fixed Labels
        for (int i = 0; i < SceneData.labelsFixed.Count; i++)
        {
            GUI.color = SceneData.GetColourFixed(i);
            Rect rect = SceneData.GetRectFixed(i);
            GUI.Label(rect, SceneData.GetTextFixed(i));
        }

        // if (SceneData.texture != null)
        // {
        //     IncrementSpacing(32);
        //     GUI.color = Color.white;
        //     Rect r = new Rect(15f, activeSpacing, 64, 64);
        //     GUI.DrawTexture(r, SceneData.texture);
        // }

        // Debug.Log($"Labels Drawn (Dynamic): {SceneData.labels.Count}");
        // Draw dynamic Labels
        for (int i = 0; i < SceneData.labels.Count; i++)
        {
            GUI.color =  SceneData.GetColour(i);
            Rect rect = SceneData.GetRect(i);
            GUI.Label(rect, SceneData.GetText(i));
        }
    }
    
    private static Texture2D CreateColorTexture(int width, int height, Color color)
    {
        Texture2D texture = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];
        
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
}