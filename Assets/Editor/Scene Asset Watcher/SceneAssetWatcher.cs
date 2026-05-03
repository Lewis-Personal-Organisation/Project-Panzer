using System;
using UnityEditor;
using System.IO;
using UnityEngine;

/*
 * Description: Observes and regenerates the "Scenes" menu bar for quickly switching scenes in-editor
 * 
 */

public class SceneAssetWatcher : AssetPostprocessor
{
    private static string _lastHash = "";
    
    
    [InitializeOnLoadMethod]
    private static void InitialisationChecks()
    {
        EditorSceneMenuGenerator.SetupPathIncluder();
        
        // Delay until Unity finishes refreshing assets
        EditorApplication.delayCall += ProjectLoaded;
        
        string current = string.Join("|", EditorSceneMenuGenerator.currentPathIncluder.includedPaths);
        _lastHash = EditorSceneMenuGenerator.StableHash(current);
        
        EditorApplication.projectChanged += OnProjectChanged;       // Subscribe to project updates. Called on Rename/Add/Move/Delete of files etc
    }

    /// <summary>
    /// Called when the project finished domain reloading
    /// </summary>
    private static void ProjectLoaded()
    {
        // Wait until Unity is done importing
        if (EditorApplication.isUpdating || EditorApplication.isCompiling)
            return;

        EditorApplication.delayCall -= ProjectLoaded;

        // Generate Scenes Menu file if it does not exist
        if (!File.Exists(EditorSceneMenuGenerator.OutputPath))
        {
            Debug.Log("SceneMenuItems.cs or Included Scene Paths File missing — regenerating...");
            EditorSceneMenuGenerator.Generate();
            return;
        }
        
        // Don't regenerate if there are field issues
        if (PathIncluderHasFieldErrors())
            return;
        
        // Regenerate Scenes file if the hash does not match
        if (CheckSceneFileHashMatches() == false)
        {
            Debug.Log("Hash does not match — regenerating...");
            EditorSceneMenuGenerator.Generate();
            return;
        }
    }

    private static bool CheckSceneFileHashMatches()
    {
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", EditorSceneMenuGenerator.currentPathIncluder.includedPaths);
        Array.Sort(sceneGuids);
        string sceneListString = string.Join("|", sceneGuids);
        string hash = EditorSceneMenuGenerator.StableHash(sceneListString);
        
        string fileText = File.ReadAllText(EditorSceneMenuGenerator.OutputPath);

        // Debug.Log($"Checking for Hash: // SCENE_HASH:{hash}"); 
        
        return fileText.Contains($"// SCENE_HASH:{hash}");
    }

    static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        bool sceneChanged = false;

        foreach (string path in importedAssets)
            if (path.EndsWith(".unity"))
                sceneChanged = true;

        foreach (string path in deletedAssets)
        {
            if (path.EndsWith(".unity"))
                sceneChanged = true;

            if (path == EditorSceneMenuGenerator.IncludedScenePathFull)
            {
                sceneChanged = true;
            }
        }

        foreach (string path in movedAssets)
        {
            if (path.EndsWith(".unity"))
                sceneChanged = true;
            
            if (path == EditorSceneMenuGenerator.IncludedScenePathFull)
            {
                sceneChanged = true;
            }
        }
        
        if (sceneChanged)
        {
            EditorSceneMenuGenerator.Generate();
        }
    }
    
    public static void OnProjectChanged()
    {
        if (EditorSceneMenuGenerator.currentPathIncluder == null)
            return;

        if (PathIncluderHasFieldErrors())
            return;
        
        Debug.Log("Project Change detected");
        string newHash = EditorSceneMenuGenerator.StableHash(string.Join("|", EditorSceneMenuGenerator.currentPathIncluder.includedPaths));

        if (newHash != _lastHash)
        {
            EditorSceneMenuGenerator.Generate();
            _lastHash = newHash;
            Debug.Log("Included paths changed — regenerating...");
        }
    }

    /// <summary>
    /// Returns whether there are issues with includedPaths' fields (empty length or entries)
    /// </summary>
    /// <returns></returns>
    private static bool PathIncluderHasFieldErrors()
    {
        for (int i = 0; i < EditorSceneMenuGenerator.currentPathIncluder.includedPaths.Length; i++)
        {
            if (EditorSceneMenuGenerator.currentPathIncluder.includedPaths[i] == string.Empty)
            { 
                Debug.Log("Included paths has a null field. Caught error!");
                return true;
            }
        }

        if (EditorSceneMenuGenerator.currentPathIncluder.includedPaths.Length == 0)
            return true;
        
        return false;
    }
}