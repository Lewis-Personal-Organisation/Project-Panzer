using Interface.Elements.Scripts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TextEditor = UnityEditor.UI.TextEditor;

namespace Interface.CustomEditor
{
    /// <summary>
    /// The custom editor for TextUI
    /// </summary>
    [UnityEditor.CustomEditor(typeof(TextUI))]
    public class TextUIEditor : TextEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            // Check if any control was changed inside a block of code
            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.Space();
            
            var x = (TextUI) target;

            // Capitalize the text
            x.Capitalize = EditorGUILayout.Toggle("Capitalize", x.Capitalize);
            
            // Animate the text to look as if it is typing
            x.animateTypingOnStart = EditorGUILayout.Toggle("Typing Effect", x.animateTypingOnStart);
            if (x.animateTypingOnStart)
            {
                // Adds a trailing underscore while animating
                x.addTrailingUnderscore = EditorGUILayout.Toggle("Add Trailing Underscore", x.addTrailingUnderscore);
                // Adds random characters while animating
                x.addRandomCharacters = EditorGUILayout.Toggle("Random Characters", x.addRandomCharacters);
                // The speed at which the typing animation plays
                x.typingSpeed = EditorGUILayout.FloatField("Typing Speed", x.typingSpeed);
                // The typing sound (if applicable)
                x.typingSound = (AudioClip) EditorGUILayout.ObjectField("Typing Sound", x.typingSound, typeof(AudioClip), true);
            }
            else
            {
                x.animateTypingOnStart = false;
            }

            // Save changes for multi-selection
            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }
            
            // Mark scene as dirty
            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
                EditorSceneManager.MarkSceneDirty(x.gameObject.scene);
            }
        }
    }
}