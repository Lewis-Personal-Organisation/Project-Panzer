using Interface.Elements.Scripts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Interface.CustomEditor
{
    /// <summary>
    /// The custom editor for ToggleUI
    /// </summary>
    [UnityEditor.CustomEditor(typeof(ToggleUI))]
    public class ToggleUIEditor : ToggleEditor
    {
        /// <summary>
        /// Draw the custom inspector GUI
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            // Check if any control was changed inside a block of code
            EditorGUI.BeginChangeCheck();
            
            var x = (ToggleUI) target;
            
            // The background color when ON
            x.onColor = EditorGUILayout.ColorField("On Color", x.onColor);
            // The background color when OFF
            x.offColor = EditorGUILayout.ColorField("Off Color", x.offColor);
            
            EditorGUILayout.Space();

            // The background image
            x.background = (Image) EditorGUILayout.ObjectField("Background", x.background, typeof(Image), true);
            // The outline image
            x.outline = (Image) EditorGUILayout.ObjectField("Outline", x.outline, typeof(Image), true);
            // The highlighter image that will move left or right
            x.highlighter = (Image) EditorGUILayout.ObjectField("Highlighter", x.highlighter, typeof(Image), true);

            EditorGUILayout.Space();
            
            // The text shown when ON
            x.onText = (Text) EditorGUILayout.ObjectField("On Text", x.onText, typeof(Text), true);
            // The image shown when ON
            x.onImage = (Image) EditorGUILayout.ObjectField("On Image", x.onImage, typeof(Image), true);
            
            // The text shown when OFF
            x.offText = (Text) EditorGUILayout.ObjectField("Off Text", x.offText, typeof(Text), true);
            // The image shown when OFF
            x.offImage = (Image) EditorGUILayout.ObjectField("Off Image", x.offImage, typeof(Image), true);

            EditorGUILayout.Space();

            // The highlighter on the left signifies ON
            x.leftIsOn = EditorGUILayout.Toggle("Left Is On", x.leftIsOn);
            
            
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