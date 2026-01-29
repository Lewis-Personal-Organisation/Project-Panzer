using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PersistentDataHost : Singleton<PersistentDataHost>
{
    public class CrossSceneData
    {
        public string errorMessage = string.Empty;
    }
    public CrossSceneData crossSceneData;
    
    private new void Awake()
    {
        base.Awake();
        
        // If this instance was destroyed by the base class, don't continue
        if (Instance != this)
            return;
        
        DontDestroyOnLoad(this);

        crossSceneData = new CrossSceneData();
    }
}
