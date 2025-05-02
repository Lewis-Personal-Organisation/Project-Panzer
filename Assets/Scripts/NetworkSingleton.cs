using Unity.Netcode;
using UnityEngine;

public class NetworkSingleton<T> : NetworkBehaviour where T : NetworkSingleton<T>
{
    // Our public static instance
    public static T Instance { get; private set; }

    // Our Awake function should be called ideally within the Awake function of the inheriting class
    protected void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError($"An instance of {this.GetType().Name} already exists");
            Destroy(this);
            return;
        }
        Instance = (T)this;
    }
}
