using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

/// <summary>
/// Delegates the call to OnTrigger2D for this object to another object.
/// </summary>
public class TriggerDelegator : MonoBehaviour
{
    [SerializeField] private Collider caller;

    private void Awake()
    {
        if (!caller)
            caller = GetComponent<Collider>();
    }

    [Space(10)]
    [FormerlySerializedAs("OnEnter")]
    [Tooltip("Which function should be called when trigger was entered.")]
    public UnityEvent<OnTriggerDelegation> OnTriggerEnterEvent;

    [Tooltip("Which function should be called when trigger was exited.")]
    public UnityEvent<OnTriggerDelegation> OnTriggerExitEvent;

    private void OnTriggerEnter(Collider other) => OnTriggerEnterEvent.Invoke(new OnTriggerDelegation(caller, other));
    private void OnTriggerExit(Collider other) => OnTriggerExitEvent.Invoke(new OnTriggerDelegation(caller, other));
    
    
    [Tooltip("Which function should be called when Collision was entered.")]
    public UnityEvent<OnCollisionDelegation> OnCollisionEnterEvent;
    
    [Tooltip("Which function should be called when Collision was exited.")]
    public UnityEvent<OnCollisionDelegation> OnCollisionExitEvent;

    private void OnCollisionEnter(Collision other) => OnCollisionEnterEvent.Invoke(new OnCollisionDelegation(caller, other));
    private void OnCollisionExit(Collision other) => OnCollisionExitEvent.Invoke(new OnCollisionDelegation(caller, other));
}

/// <summary>
/// Stores which collider triggered this call and which collider belongs to the other object.
/// </summary>
public struct OnCollisionDelegation
{
    /// <summary>
    /// Creates an OnTriggerDelegation struct.
    /// Stores which collider triggered this call and which collider belongs to the other object.
    /// </summary>
    /// <param name="caller">The trigger collider which triggered the call.</param>
    /// <param name="collision">The Collision.</param>
    public OnCollisionDelegation(Collider caller, Collision collision)
    {
        Caller = caller;
        Collision = collision;
    }

    /// <summary>
    /// The trigger collider which triggered the call.
    /// </summary>
    public Collider Caller { get; private set; }

    /// <summary>
    /// The other collider.
    /// </summary>
    public Collision Collision { get; private set; }
}

/// <summary>
/// Stores which collider triggered this call and which collider belongs to the other object.
/// </summary>
public struct OnTriggerDelegation
{
    /// <summary>
    /// Creates an OnTriggerDelegation struct.
    /// Stores which collider triggered this call and which collider belongs to the other object.
    /// </summary>
    /// <param name="caller">The trigger collider which triggered the call.</param>
    /// <param name="other">The collider which belongs to the other object.</param>
    public OnTriggerDelegation(Collider caller, Collider other)
    {
        Caller = caller;
        Other = other;
    }

    /// <summary>
    /// The trigger collider which triggered the call.
    /// </summary>
    public Collider Caller { get; private set; }

    /// <summary>
    /// The other collider.
    /// </summary>
    public Collider Other { get; private set; }
}