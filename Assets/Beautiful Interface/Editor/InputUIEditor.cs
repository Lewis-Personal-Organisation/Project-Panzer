using Interface.Elements.Scripts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Interface.CustomEditor
{
    /// <summary>
    /// The custom editor for InputUI
    /// </summary>
    [UnityEditor.CustomEditor(typeof(InputUI))]
    public class InputUIEditor : InputFieldEditor
    {
        /// <summary>
        /// Draw the custom inspector GUI
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            // Check if any control was changed inside a block of code
            EditorGUI.BeginChangeCheck();

            var x = (InputUI) target;
            
            // Hide the placeholder text when selected. If false, the text will shrink and move up
            x.hidePlaceholderOnSelect = EditorGUILayout.Toggle("Hide Placeholder OnSelect", x.hidePlaceholderOnSelect);
            // Different color onHover or onHighlight
            x.differentTextColorOnHighlight = EditorGUILayout.Toggle("Text Highlight Color", x.differentTextColorOnHighlight);
            
            EditorGUILayout.Space();
            
            // The color for images
            x.primaryColor = EditorGUILayout.ColorField("Background Color", x.primaryColor);
            // The color for text fields
            x.secondaryColor = EditorGUILayout.ColorField("Text Color", x.secondaryColor);
            if (x.differentTextColorOnHighlight)
                // The color of text field when finished editing
                x.highlightTextColor = EditorGUILayout.ColorField("Text Highlight Color", x.highlightTextColor);
            
            EditorGUILayout.Space();

            // Background image of the input field
            x.background = (Image) EditorGUILayout.ObjectField("Background", x.background, typeof(Image), true);

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