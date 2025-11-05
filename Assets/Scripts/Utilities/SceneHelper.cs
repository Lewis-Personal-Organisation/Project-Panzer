using System;
using System.Collections.Generic;
using UnityEngine;
using Eflatun.SceneReference;
using UnityEngine.Events;

public class SceneHelper : Singleton<SceneHelper>
{
   [field: SerializeField] public SceneReference mainMenuScene { private set; get;}
   [field: SerializeField] public SceneReference mainGameplayScene { private set; get;}
   [field: SerializeField] public SceneReference prototypeScene { private set; get;}
   [field: SerializeField] public SceneReference projectilePrototyping { private set; get;}
   
   private HotkeyManager hotkeyManager;
   
   private void Awake()
   {
      base.Awake();
      
      if (this)
         DontDestroyOnLoad(this);
      
      hotkeyManager = new HotkeyManager(
         new HotkeyCombo(Extensions.Debug.ClearConsole, KeyCode.LeftControl, KeyCode.LeftShift, KeyCode.Slash)
         );
   }

   public class HotkeyManager
   {
      public static List<KeyCode> trackedKeys = new List<KeyCode>();
      public List<HotkeyCombo> combos = new List<HotkeyCombo>();

      public HotkeyManager(params HotkeyCombo[] combos)
      {
         this.combos.AddRange(combos);
      }
      
      private void Update()
      {
         for (int i = 0; i < combos.Count; i++)
         {
            combos[i].Evaluate();
         }
      }
   }
   
   public class HotkeyCombo
   {
      public class KeyState
      {
         private KeyCode keyCode;
         private bool active = false;

         public KeyState(KeyCode keyCode)
         {
            this.keyCode = keyCode;
            active = false;
         }

         public bool IsValid()
         {
            active = Input.GetKey(keyCode);
            return active;
         }

         public void Cancel()
         {
            active = false;
         }
      }
      
      private List<KeyState> keyStates = new List<KeyState>();
      private UnityAction onCombo = null;
      private int comboIndex = 1;
      
      
      public HotkeyCombo(UnityAction action, params KeyCode[] comboList)
      {
         onCombo = action;

         for (int i = 0; i < comboList.Length; i++)
         {
            keyStates.Add(new KeyState(comboList[i]));
         }
      }

      public void Evaluate()
      {
         if (keyStates[comboIndex].IsValid())
         {
            if (comboIndex == keyStates.Count - 1)
            {
               // Combo Complete
            }
            else
            {
               // Combo counting up...
               comboIndex++;
            }
         }

         for (int i = 0; i < keyStates.Count; i++)
         {
            if (keyStates[i].IsValid())
            {
               comboIndex++;
            }
            else
            {
               
            }
         }
      }
   }
}
