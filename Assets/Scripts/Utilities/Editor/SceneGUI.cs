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
        multi = 0;
        Handles.BeginGUI();
        // if (GUILayout.Button("Press Me"))
        //     Debug.Log("Got it to work.");
        GUI.color = Color.blue;
        DrawLabel($"Vert Axis: {OnGUISceneViewData.forwardInputValue}");
        DrawLabel($"Input Speed: {OnGUISceneViewData.inputSpeed}");
        DrawLabel($"Speed Target: {OnGUISceneViewData.speedTarget}");
        DrawLabel($"Rot Delta: {OnGUISceneViewData.rotationDelta}");
        DrawLabel($"Brake Force: {OnGUISceneViewData.brakeForce}");
        GUI.color = Color.red;
        multi++;
        DrawLabel($"Velocity: {OnGUISceneViewData.tankVelocity}");
        DrawLabel($"Local Velocity: {OnGUISceneViewData.tankLocalVelocity}");
        DrawLabel($"Velocity Dot: {OnGUISceneViewData.velocityDot}");
        
        Handles.EndGUI();
    }

    private static void DrawLabel(string text, float x = 10f, float width = 550f, float height = 25F)
    {
        GUI.Label(new Rect(x, baseSpace + multi * spacing, width, height), text);
        multi++;
    }
}