using UnityEngine;
using UnityEngine.Serialization;

public class LocalZVelocityTracker : VehicleComponent
{
    [SerializeField] Rigidbody rb;
    public float velocity { get; private set; }
    private float lastZVelocity = 0F;
    public float delta;
    public int normalisedDelta;
    [SerializeField] private float deadZone = 0.01f;


    private new void Awake()
    {
        base.Awake();
    }

    void FixedUpdate()
    {
        if (!enabled) return;
        
        // Extract the z-component (forward speed) of local velocity
        velocity = transform.InverseTransformDirection(rb.velocity).z;
        
        // Calculate the change since last frame
        delta = velocity - lastZVelocity;
        
        if (Mathf.Abs(delta) < deadZone)
            delta = 0F;
        
        normalisedDelta = delta > 0 ? 1 : delta < 0 ? -1 : 0;
        
        // Store current for next frame
        lastZVelocity = velocity;
    }
}