#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class EditorApplicationUpdater
{
    /// <summary>
    /// The delta time between frames, taking into account deltaClamp
    /// </summary>
    public static float DeltaTime { get; private set; }

    /// <summary>
    /// The maximum delta time allowed, to stop large spiked behaviour. Defaulted to Unity's Time.maximumDeltaTime value.
    /// </summary>
    public static float deltaClamp;
    
    private static double lastTime;
    
    static EditorApplicationUpdater()
    {
        deltaClamp = Time.maximumDeltaTime;
        lastTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += Update;
    }
    
    /// <summary>
    /// Provides
    /// </summary>
    private static void Update()
    {
        double now = EditorApplication.timeSinceStartup;
        DeltaTime = Mathf.Min((float)(now - lastTime), deltaClamp);
        lastTime = now;
    }

    /// <summary>
    /// Call from OnDrawGizmos to cause OnDrawGizmos itself to loop using repaints
    /// Example use case: Applying DeltaTime every frame to movement
    /// </summary>
    public static void RepaintOnUse() => SceneView.RepaintAll();
}
#endif