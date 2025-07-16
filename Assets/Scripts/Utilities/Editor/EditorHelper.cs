using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EditorHelper : MonoBehaviour
{
    private static SceneHelper sceneHelper;
    
    [MenuItem("Scenes/Open Menu Scene", false, 0)]
    private static void OpenMenuScene()
    {
        SceneHelper[] sceneHelpers = FindObjectsOfType(typeof(SceneHelper)) as SceneHelper[];

        if (sceneHelpers is { Length: > 0 })
        {
            if (EditorApplication.isPlaying)
            {
                SceneManager.LoadScene(sceneHelpers[0].mainMenuScene.Path);
            }
            else
            {
                EditorSceneManager.OpenScene(sceneHelpers[0].mainMenuScene.Path);
            }
        }
    }

    [MenuItem("Scenes/Open Gameplay Scene", false, 1)]
    private static void OpenGameplayScene()
    {
        SceneHelper[] sceneHelpers = FindObjectsOfType(typeof(SceneHelper)) as SceneHelper[];

        if (sceneHelpers is { Length: > 0 })
        {
            if (EditorApplication.isPlaying)
            {
                SceneManager.LoadScene(sceneHelpers[0].mainGameplayScene.Path);
            }
            else
            {
                EditorSceneManager.OpenScene(sceneHelpers[0].mainGameplayScene.Path);
            }
        }
    }
    
    [MenuItem("Scenes/Open Projectile Proto Scene", false, 1)]
    private static void OpenProjectileProtoScene()
    {
        SceneHelper[] sceneHelpers = FindObjectsOfType(typeof(SceneHelper)) as SceneHelper[];

        if (sceneHelpers is { Length: > 0 })
        {
            if (EditorApplication.isPlaying)
            {
                SceneManager.LoadScene(sceneHelpers[0].projectilePrototyping.Path);
            }
            else
            {
                EditorSceneManager.OpenScene(sceneHelpers[0].projectilePrototyping.Path);
            }
        }
    }
}
