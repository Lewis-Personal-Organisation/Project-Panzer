using UnityEditor;
using UnityEngine;
using Sirenix.OdinInspector.Editor;

[CustomEditor(typeof(MonoBehaviour), true)]
[CanEditMultipleObjects]
public class DebuggableHeaderEditor : OdinEditor
{
    private static GUIContent onIcon;
    private static GUIContent offIcon;

    protected override void OnEnable()
    {
        base.OnEnable();
        onIcon  ??= EditorGUIUtility.IconContent("d_DebuggerAttached");
        offIcon ??= EditorGUIUtility.IconContent("d_DebuggerDisabled");
    }

    protected override void OnHeaderGUI()
    {
        base.OnHeaderGUI();

        if (target is not IDebuggable debuggable)
            return; // not a debuggable component, draw nothing extra

        Rect headerRect = GUILayoutUtility.GetLastRect();
        var iconRect = new Rect(headerRect.xMax - 42f, headerRect.y + 4f, 18f, 18f);

        var icon = debuggable.DebugMode ? onIcon : offIcon;

        EditorGUI.BeginChangeCheck();
        bool newValue = GUI.Toggle(iconRect, debuggable.DebugMode, icon, EditorStyles.iconButton);
        if (EditorGUI.EndChangeCheck())
        {
            foreach (var t in targets)
            {
                if (t is IDebuggable d)
                {
                    Undo.RecordObject(t, "Toggle Debug Mode");
                    d.DebugMode = newValue;
                    EditorUtility.SetDirty(t);
                }
            }
        }
    }
}