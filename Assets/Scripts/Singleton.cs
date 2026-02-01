using System;
using UnityEngine;

/* Singleton Pattern
 *  Desc: Any class inherited from this will have a Singleton instance created and assigned automatically. 
 *  If a Instance is already assigned, the new instance is destroyed.
 */
public class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    public static T Instance { get; private set; }          // Public static Instance

    // Our Awake function should be called ideally within the Awake function of the inheriting class
    protected void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning($"Instance of {this.GetType().Name} already exists ({Instance.GetInstanceID()}). Destroying duplicate.");
            Destroy(this.gameObject);
            return;
        }
        Instance = (T)this;
    }

    protected virtual void OnDestroy()
    {
        Debug.Log($"On Destroy called for {this.GetType().Name} on GameObject{this.gameObject.name} ({GetInstanceID()})");
    }
}