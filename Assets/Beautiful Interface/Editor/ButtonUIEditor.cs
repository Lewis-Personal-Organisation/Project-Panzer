using Interface.Elements.Scripts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Interface.CustomEditor
{
    /// <summary>
    /// The custom editor for ButtonUI
    /// </summary>
    [UnityEditor.CustomEditor(typeof(ButtonUI))]
    public class ButtonUIEditor : ButtonEditor
    {
        /// <summary>
        /// Draw the custom inspector GUI
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            // Check if any control was changed inside a block of code
            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.Space();
            
            var x = (ButtonUI) target;

            // The duration for each tween animation
            x.duration = EditorGUILayout.FloatField("Transition Time", x.duration);

            EditorGUILayout.Space();
            
            // The button has slider effect
            x.hasSlider = EditorGUILayout.Toggle("Slider Effect", x.hasSlider);
            if (x.hasSlider) 
                // The slider used for the slider effect
                x.slider = (Slider) EditorGUILayout.ObjectField("Slider", x.slider, typeof(Slider), true);

            EditorGUILayout.Space();

            // The button state when normal
            var propNormal = serializedObject.FindProperty("normalStates");
            EditorGUILayout.PropertyField(propNormal);
            
            EditorGUILayout.Space();

            // The button state when highlighted
            var propHighlight = serializedObject.FindProperty("highlightStates");
            EditorGUILayout.PropertyField(propHighlight);

            EditorGUILayout.Space();

            // The button state when clicked
            var propClick = serializedObject.FindProperty("clickStates");
            EditorGUILayout.PropertyField(propClick);
            
            EditorGUILayout.Space();

            // On mouse hover sound
            x.hasHoverSound = EditorGUILayout.Toggle("Hover Sound", x.hasHoverSound);
            if (x.hasHoverSound) x.onHoverAudio = (AudioClip) EditorGUILayout.ObjectField(x.onHoverAudio, typeof(AudioClip), true);

            // On mouse click sound
            x.hasClickSound = EditorGUILayout.Toggle("Click Sound", x.hasClickSound);
            if (x.hasClickSound) x.onClickAudio = (AudioClip) EditorGUILayout.ObjectField(x.onClickAudio, typeof(AudioClip), true);

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

            // Apply property modifications
            serializedObject.ApplyModifiedProperties();
        }
    }
}