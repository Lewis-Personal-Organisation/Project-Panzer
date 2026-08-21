#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class EditorApplicationUpdater
{
    // Should we repaint the scene in this frame?
    private static bool shouldRepaint = false;
    
    /// <summary>
    /// The delta time between frames, taking into account deltaClamp
    /// </summary>
    public static float DeltaTime 
    {
        get
        {
            shouldRepaint = true;
            return deltaTime;
        }
        private set => deltaTime = value;
    }
    private static float deltaTime;

    /// <summary>
    /// The maximum delta time allowed, to stop large spiked behaviour. Defaulted to Unity's Time.maximumDeltaTime value.
    /// </summary>
    public static float DeltaClamp;
    
    /// <summary>
    /// The previous cached time, used to calculate the delta time
    /// </summary>
    private static double lastTime;
    
    /// <summary>
    /// Sets the default delta clamp, last update time assigns the Update method
    /// </summary>
    static EditorApplicationUpdater()
    {
        DeltaClamp = Time.maximumDeltaTime;
        lastTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += Update;
    }
    
    /// <summary>
    /// Calculates the Delta Time value and repaints the scene view if necessary
    /// </summary>
    private static void Update()
    {
        double now = EditorApplication.timeSinceStartup;
        DeltaTime = Mathf.Min((float)(now - lastTime), DeltaClamp);
        lastTime = now;
        
        if (shouldRepaint)
        {
            // Queue a player loop update if we are not in Play Mode to force a redraw
            if (!Application.isPlaying)
                EditorApplication.QueuePlayerLoopUpdate();
            
            SceneView.RepaintAll();
            shouldRepaint = false;
        }
    }
}
#endif