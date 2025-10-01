using UnityEditor;
using UnityEngine;

public class SceneGUI : EditorWindow
{
    private static float baseSpace = 10;
    private static float multi;
    private static float spacing = 15;
    
    
    // Auto enable Scene view onGUI after scene starts
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoEnable()
    {
        Enable();
    }

    [MenuItem("Scene Overlay/Enable")]
    public static void Enable()
    {
        SceneView.duringSceneGui += OnScene;
    }
 
    [MenuItem("Scene Overlay/Disable")]
    public static void Disable()
    {
        SceneView.duringSceneGui -= OnScene;
    }
 
    private static void OnScene(SceneView sceneView)
    {
        Handles.BeginGUI();
        for (int i = 0; i < OnGUISceneViewData.labelEntries.Count; i++)
        {
            
            DrawLabel(i);
        }
        Handles.EndGUI();
    }

    private static void DrawLabel(int index, float x = 15f, float width = 550f, float height = 25F)
    {
        GUI.color =  OnGUISceneViewData.GetColour(index);
        GUI.Label(new Rect(x, baseSpace + (index + 1) * spacing, width, height), OnGUISceneViewData.GetText(index));
    }
}