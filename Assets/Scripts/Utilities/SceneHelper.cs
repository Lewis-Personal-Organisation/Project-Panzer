using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Eflatun.SceneReference;

public class SceneHelper : Singleton<SceneHelper>
{
   private void Awake()
   {
      base.Awake();
      
      if (this)
         DontDestroyOnLoad(this);
   }
   
   [field: SerializeField] public SceneReference mainMenuScene { private set; get;}
   [field: SerializeField] public SceneReference mainGameplayScene { private set; get;}
}
