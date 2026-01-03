using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EditorHelper : MonoBehaviour
{
    private static string prefix = "Assets/Scenes/";
    private static string postfix = ".unity";

    // Scene Menu Buttons
    [MenuItem("Scenes/Main Menu", false, 0)] private static void OpenMenuScene() => OpenScene("Main Menu");
    [MenuItem("Scenes/Gameplay", false, 1)] private static void OpenGameplayScene() => OpenScene("Gameplay");
    [MenuItem("Scenes/Test Ground", false, 2)] private static void OpenPrototypeScene() => OpenScene("Test Ground");
    [MenuItem("Scenes/Projectile Movement", false, 3)] private static void OpenProjectileScene() => OpenScene("Projectile Movement");

    private static void OpenScene(string sceneName)
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

        foreach (var scene in scenes)
        {
            if (!scene.enabled) continue;

            string sceneFileName = System.IO.Path.GetFileNameWithoutExtension(scene.path);
            
            if (sceneFileName == sceneName)
            {
                if (EditorApplication.isPlaying)
                {
                    SceneManager.LoadScene(sceneName);
                }
                else
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene($"{prefix}{sceneName}{postfix}");
                    }
                }
                return;
            }
        }

        Debug.LogError($"Scene '{sceneName}' not found in Build Settings or is disabled.");
    }
}
