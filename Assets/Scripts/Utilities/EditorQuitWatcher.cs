
#if UNITY_EDITOR
using UnityEditor;

public class EditorQuitWatcher
{
    private static bool wantsToQuit = false;
    private static bool isQuiting = false;
    private static bool isExitingPlayMode = false;

    /// <summary>
    /// True if the Editor application itself is quitting, or Play Mode is being stopped.
    /// </summary>
    public static bool IsClosing => wantsToQuit || isQuiting;
    public static bool IsQuiting => IsClosing || isExitingPlayMode;
    public static bool AllowQuiting = true;
    
    [UnityEditor.InitializeOnLoadMethod]
    private static void Initialise()
    {
        EditorApplication.quitting += () => isQuiting = true;
        EditorApplication.wantsToQuit += () =>
        {
            wantsToQuit = AllowQuiting;
            return AllowQuiting;
        };
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            isExitingPlayMode = true;
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            // Reset so the flag doesn't leak into the next time Play Mode is entered.
            isExitingPlayMode = false;
        }
    }
}
#endif