using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [field: SerializeField] public Camera camera {private set; get; }
    // public bool enabled;
    public Transform trToLookAt;
    private Vector3 cachedPositionOffset;
    public Vector3 offset;
    public float offsetT = 0;
    public float lerpSpeed = 1;
    public float inputValue = 0;
    
    // public CacheableVector3 offsetN;
    //private Vector3 lookAtPos;
    
    // private void OnEnable()
    // {
    //     if (offsetN.cacheBase == null)
    //         offsetN.cacheBase = ScriptableObject.CreateInstance<CacheableVector3.CacheableV3>();
    // }
    //
    // private void Awake()
    // {
    //     EditorApplication.playModeStateChanged += offsetN.cacheBase.Restore;
    // }
    //
    // private void OnValidate()
    // {
    //     if (offsetN.cacheBase == null)
    //         return;
    //     
    //     offsetN.cacheBase.Save();
    // }
    //
    // [System.Serializable]
    // public class CacheableVector3
    // {
    //     [SerializeField] private Vector3 value;
    //
    //     [SerializeField] public CacheableV3 cacheBase;
    //     
    //     [FilePath("Persistent/V3Cache.foo", FilePathAttribute.Location.PreferencesFolder)]
    //     public class CacheableV3 : ScriptableSingleton<CacheableV3>
    //     {
    //         private Vector3 value;
    //         
    //         public void Save()
    //         {
    //             base.Save(true);
    //         }
    //
    //         public void Restore(PlayModeStateChange state)
    //         {
    //             switch (state)
    //             {
    //                 case PlayModeStateChange.ExitingPlayMode:
    //                     this.value = instance.value;
    //                     Debug.Log(instance.value);
    //                     break;
    //             }
    //         }
    //     }
    //     
    //     
    // }

    
    private void Awake()
    {
        if (camera == null)
            camera = GetComponent<Camera>();
        
        cachedPositionOffset = this.transform.position - trToLookAt.position;
    }

    private void FixedUpdate()
    {
        // Set Position relative to target
        this.transform.position = trToLookAt.position + cachedPositionOffset;
        
        // Adjust offsetT towards value
        offsetT = Mathf.MoveTowards(offsetT, inputValue, Time.deltaTime * lerpSpeed);

        Vector3 lookAtPos = Vector3.zero;
        
        if (offsetT < 0)
        {
            lookAtPos = trToLookAt.position + Vector3.Slerp(Vector3.zero, trToLookAt.forward * -offset.z, offsetT);
        }
        else if (offsetT >= 0)
        {
            lookAtPos = trToLookAt.position + Vector3.Slerp(Vector3.zero, trToLookAt.forward * offset.z, offsetT);
        }
        
        this.transform.LookAt(lookAtPos);
    }

    private void OnDrawGizmosSelected()
    {
        if (offset.x is > 0 or < 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(trToLookAt.position, trToLookAt.position + trToLookAt.right * offset.x);
        }

        if (offset.y is > 0 or < 0)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(trToLookAt.position, trToLookAt.position + trToLookAt.up * offset.y);
        }

        if (offset.z is > 0 or < 0)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(trToLookAt.position, trToLookAt.position + trToLookAt.forward * offset.z);
        }
    }
}
