using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Included Scene Paths", menuName = "ScriptableObjects/ScenePathIncluder", order = 1)]
public class PathIncluder : ScriptableObject
{
    public string[] includedPaths = Array.Empty<string>();
    
    private void OnValidate()
    {
        SceneAssetWatcher.OnProjectChanged();
    }
}
