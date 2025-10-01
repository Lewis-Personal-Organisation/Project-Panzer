using UnityEngine;

public class RigidBodyVelocityTracker : VehicleComponent
{
    [System.Serializable]
    public class VelocityAxisData
    {
        public bool track = false;
        public float velocity = 0F;
        internal float delta = 0F;
        internal float normalisedDelta = 0F;
        [SerializeField] float deadZone = 0.01F;
        private float lastVelocity = 0F;
        
        public float Clamped(float min, float max) => Mathf.Clamp(velocity, min, max);
        internal void Update(float newVelocity)
        {
            velocity = newVelocity;
            
            // Calculate the change since last frame
            delta = velocity - lastVelocity;

            // Discard values below dead zone
            if (Mathf.Abs(delta) < deadZone)
            {
                delta = 0F;
                normalisedDelta = 0F;
            }
            else
            {
                normalisedDelta = delta > 0 ? 1 : delta < 0 ? -1 : 0;
            }
        
            // Store current for next frame
            lastVelocity = velocity;
        }
    }
    
    [SerializeField] private Rigidbody rb;
    private Vector3 velocity;

    public VelocityAxisData x = new VelocityAxisData();
    public VelocityAxisData y = new VelocityAxisData();
    public VelocityAxisData z = new VelocityAxisData();


    private new void Awake()
    {
        base.Awake();
    }

    private void FixedUpdate()
    {
        if (!enabled) return;
        if (!x.track && !y.track && !z.track) return;

        // local velocity
        velocity = transform.InverseTransformDirection(rb.velocity);

        if (x.track)
        {
            x.Update(velocity.x);
        }

        if (y.track)
        {
            y.Update(velocity.y);
        }

        if (z.track)
        {
            z.Update(velocity.z);
        }
    }
}