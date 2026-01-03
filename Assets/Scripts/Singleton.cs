using System;
using UnityEngine;

/* Singleton Pattern
 *  Desc: Any class inherited from this will have a Singleton instance created and assigned automatically. 
 *  If a Instance is already assigned, the new instance is destroyed.
 */
public class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    public bool nullOnDestroy = false;
    
    // Our public static instance
    public static T Instance { get; private set; }

    // Our Awake function should be called ideally within the Awake function of the inheriting class
    protected void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning($"An instance of {this.GetType().Name} exists. Destroying.");
            Destroy(this.gameObject);
            return;
        }
        Instance = (T)this;
    }

    protected virtual void OnDestroy()
    {
        Debug.Log($"On Destroy called for {this.gameObject.name}");
        if (nullOnDestroy)
            Instance = null;
    }
}