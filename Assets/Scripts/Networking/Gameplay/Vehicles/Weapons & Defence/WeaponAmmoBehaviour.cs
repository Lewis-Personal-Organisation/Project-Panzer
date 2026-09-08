using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public struct ShellSpawnData : INetworkSerializable
{
    public bool DirtyBool;   // Flips every shot to dirty this struct
    public Vector3 Position;
    public Quaternion Rotation;
    public bool Pooled;
    public NetworkString OwnerName;

    public ShellSpawnData(bool dirtyBool, Vector3 position, Quaternion rotation, bool pooled, NetworkString ownerName)
    {
        DirtyBool = dirtyBool;
        Position = position;
        Rotation = rotation;
        Pooled = pooled;
        OwnerName = ownerName;
    }
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Position);
        serializer.SerializeValue(ref Rotation);
        serializer.SerializeValue(ref DirtyBool);
        serializer.SerializeValue(ref Pooled);

        if (OwnerName == null)
            OwnerName = new NetworkString();
        
        serializer.SerializeValue(ref OwnerName);
    }
}

public abstract class WeaponAmmoBehaviour : NetworkBehaviour
{
    public NetworkVariable<ShellSpawnData> spawnData = new NetworkVariable<ShellSpawnData>(default,  NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    // Tracks the amount of hits for clients
    public NetworkVariable<int> usedCounter = new NetworkVariable<int>(0,  NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    [SerializeField]
    internal NetworkTransform networkTransform;
    
    public float baseDamage;
    
    [Header("Lifetime")]
    [SerializeField] internal float lifetime;
    internal float lifetimeTimer;

    [SerializeField] private MeshRenderer meshRenderer;
    public new Collider collider;
    public TrailRenderer trailRenderer;
    internal float trailTime;
    
    
    public abstract void Setup(VehicleWeaponController weaponController, Vector3 position, Quaternion rotation);
    public abstract void OwnerNetworkUpdate();
    public abstract void OnUpdate();
    public abstract void NetworkedFixedUpdate();
    public abstract void OnFixedUpdate();

    
    /// <summary>
    /// Rotate with the authoritatve owner
    /// </summary>
    /// <param name="newDirection"></param>
    public void RotateWithOwner(Vector3 newDirection)
    {
        Debug.Log($"Rotating. Owner? {IsOwner}");
        if (!IsOwner) return;
        
        networkTransform.Teleport(transform.position, Quaternion.LookRotation(newDirection), transform.localScale);
    }

    /// <summary>
    /// Called when a shell is Spawned or Collides LOCALLY only
    /// </summary>
    /// <param name="active"></param>
    public void ToggleVisuals(bool active)
    {
        meshRenderer.enabled = active;
        collider.enabled = active;
        
        trailRenderer.emitting = active;
        trailRenderer.time = active ? trailTime : 0;
        trailRenderer.transform.SetParent(active ? this.transform : null);
        
        if (active)
            trailRenderer.transform.position = this.transform.position;
    }

    /// <summary>
    /// Rotate locally, non-networked
    /// </summary>
    /// <param name="newDirection"></param>
    public void RotateWithReflectionLocal(Vector3 newDirection)
    {
        transform.forward = newDirection;
    }

    /// Tell the server we have used this shell on a client
    [ServerRpc(RequireOwnership = false)]
    public void IncreaseUseAmountServerRPC()
    {
        usedCounter.Value++;
    }
}
