using System.Collections.Generic;
using Interface.Elements.Scripts;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Interface.CustomEditor
{
    /// <summary>
    /// This script handles the MenuItem in Tools -> Beautiful Interface
    /// </summary>
    public static class ReplaceElements
    {
        /// <summary>
        /// Upgrade all UI elements to BI elements
        /// </summary>
        [MenuItem("Tools/Beautiful Interface/Upgrade/All")]
        public static void UpgradeAll()
        {
            UpdgradeTexts();
            UpdgradeButtons();
            UpgradeInputs();
            Debug.Log("Upgrade all finished");
        }

        /// <summary>
        /// Upgrade the selected element to it's corresponding Beautiful Interface element
        /// </summary>
        [MenuItem("Tools/Beautiful Interface/Upgrade/Selected")]
        public static void UpgradeSelected()
        {
            foreach (var selected in Selection.transforms)
            {
                if (selected.GetComponent<Text>() != null)
                {
                    UpgradeObject<Text, TextUI>(selected.GetComponent<Text>());
                }
                else if (selected.GetComponent<Button>() != null)
                {
                    UpgradeObject<Button, ButtonUI>(selected.GetComponent<Button>());
                }
                else if (selected.GetComponent<InputField>() != null)
                {
                    UpgradeObject<InputField, InputUI>(selected.GetComponent<InputField>());
                }
            }
        }
        
        /// <summary>
        /// Upgrade all Text to TextUI elements
        /// </summary>
        [MenuItem("Tools/Beautiful Interface/Upgrade/Texts Only")]
        public static void UpdgradeTexts()
        {
            var count = UpgradeConfirmed<Text, TextUI>();
            Debug.Log("Upgraded " + count + " Texts");
        }
        
        /// <summary>
        /// Upgrade all Button to ButtonUI elements
        /// </summary>
        [MenuItem("Tools/Beautiful Interface/Upgrade/Buttons Only")]
        public static void UpdgradeButtons()
        {
            var count = UpgradeConfirmed<Button, ButtonUI>();
            Debug.Log("Upgraded " + count + " Buttons");
        }
        
        /// <summary>
        /// Upgrade all InputField to InputUI elements
        /// </summary>
        [MenuItem("Tools/Beautiful Interface/Upgrade/Input Fields Only")]
        public static void UpgradeInputs()
        {
            var count = UpgradeConfirmed<InputField, InputUI>();
            Debug.Log("Upgraded " + count + " Input Fields");
        }

        /// <summary>
        /// Revert all BI elements to UnityEngine.UI elements
        /// </summary>
        [MenuItem("Tools/Beautiful Interface/Revert/All")]
        public static void RevertAll()
        {
            RevertTexts();
            RevertButtons();
            RevertInputs();
        }
        
        /// <summary>
        /// Revert all TextUI to Text elements
        /// </summary>
        [MenuItem("Tools/Beautiful Interface/Revert/Texts Only")]
        public static void RevertTexts()
        {
            var count = UpgradeConfirmed<TextUI, Text>();
            Debug.Log("Reverted " + count + " Texts");
        }
        
        /// <summary>
        /// Revert all ButtonUI to Button elements
        /// </summary>
        [MenuItem("Tools/Beautiful Interface/Revert/Buttons Only")]
        public static void RevertButtons()
        {
            var count = UpgradeConfirmed<ButtonUI, Button>();
            Debug.Log("Reverted " + count + " Buttons");
        }
        
        /// <summary>
        /// Revert all InputUI to InputField elements
        /// </summary>
        [MenuItem("Tools/Beautiful Interface/Revert/Input Fields Only")]
        public static void RevertInputs()
        {
            var count = UpgradeConfirmed<InputUI, InputField>();
            Debug.Log("Reverted " + count + " Input Fields");
        }
        
        
        
        /// <summary>
        /// Remove the TExisting component and replace it with TNew
        /// TExisting must be a parent or same type of TNew
        /// </summary>
        /// <typeparam name="TExisting"></typeparam>
        /// <typeparam name="TNew"></typeparam>
        /// <returns></returns>
        private static int UpgradeConfirmed<TExisting, TNew>() where TExisting: MonoBehaviour where TNew: MonoBehaviour
        {
            var components = GetComponents<TExisting>();
            foreach (var tComp in components)
            {
                UpgradeObject<TExisting, TNew>(tComp);
            }

            return components.Count;
        }

        private static void UpgradeObject<TExisting, TNew>(TExisting tComp) where TExisting : MonoBehaviour where TNew : MonoBehaviour
        {
            var newObject = GameObject.Instantiate(tComp.gameObject, tComp.transform.parent);
            newObject.transform.SetSiblingIndex(tComp.transform.GetSiblingIndex());
                
            Undo.DestroyObjectImmediate(newObject.GetComponent<TExisting>());
            var newTComp = Undo.AddComponent<TNew>(newObject);
            PropertyCopier<TExisting, TNew>.Copy(tComp, newTComp);
            GameObject.DestroyImmediate(tComp.gameObject);
        }

        
        /// <summary>
        /// Get the root objects in the scene
        /// </summary>
        /// <returns></returns>
        private static GameObject[] GetRootObjects()
        {
            return UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        }
        
        /// <summary>
        /// Get all the components of Type T in the scene
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static List<T> GetComponents<T>()
        {
            var comps = new List<T>();
            var gameObjects = GetRootObjects();
            foreach (var o in gameObjects)
            {
                comps.AddRange(GetComponentsAux<T>(o.transform));
            }
            
            return comps;
        }
        
        /// <summary>
        /// Get all the components of Type T under parent gameobject
        /// </summary>
        /// <param name="parent"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static List<T> GetComponentsAux<T>(Transform parent)
        {
            var comps = new List<T>();
            foreach (Transform child in parent)
            {
                var c = child.GetComponent<T>();
                if (c != null) comps.Add(c);

                comps.AddRange(GetComponentsAux<T>(child));
            }
            
            return comps;
        }
        
        
        /// <summary>
        /// Copy the properties of one Component to another
        /// </summary>
        /// <typeparam name="TParent">The parent copy FROM</typeparam>
        /// <typeparam name="TChild">The child copy TO</typeparam>
        public static class PropertyCopier<TParent, TChild> where TParent : MonoBehaviour where TChild : MonoBehaviour
        {
            public static void Copy(TParent parent, TChild child)
            {
                var parentProperties = parent.GetType().GetProperties();
                var childProperties = child.GetType().GetProperties();

                foreach (var parentProperty in parentProperties)
                {
                    foreach (var childProperty in childProperties)
                    {
                        if (parentProperty.Name == childProperty.Name && parentProperty.PropertyType == childProperty.PropertyType)
                        {
                            if (childProperty.CanWrite)
                                childProperty.SetValue(child, parentProperty.GetValue(parent));
                            break;
                        }
                    }
                }
            }
        }
    }
}