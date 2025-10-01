using System.Collections;
using MilkShake;
using UnityEngine;
using UnityEngine.Serialization;

public class CameraController : VehicleComponent
{
    [SerializeField] private new Camera camera;
    public Shaker shaker;
    private ShakeInstance shakeInstance;
    [SerializeField] private IShakeParameters shakeData;
    public Transform targetTransform;
    public Vector3 cachedPositionOffset;
    public Vector3 offset;
    public Vector3 shakeOffset;
    private float interpolator = 0;
    public float lerpSpeed = 1;
    private float inputValue = 0;
    public float debugInputValue = 0;
    
    
    private new void Awake()
    {
        if (!camera && !this.transform.GetChild(0).TryGetComponent(out camera))
            Debug.LogError("CameraController :: Awake :: Camera Component not set or found!", this.gameObject);
            
        if (!vehicle && !targetTransform.root.TryGetComponent(out vehicle))
            Debug.LogError("CameraController :: Setup :: Vehicle Component not set or found!", this.gameObject);
        
        cachedPositionOffset = this.transform.position - targetTransform.position;
    }

    private void FixedUpdate()
    {
        // Set Position relative to target
        this.transform.position = targetTransform.position + cachedPositionOffset;

        // Adjust offsetT towards value
        interpolator = Mathf.MoveTowards(interpolator, Mathf.Max(vehicle.inputManager.moveInput, debugInputValue), Time.deltaTime * lerpSpeed);
        
        // Position is equal to the forward axis * offset (pos or neg)
        Vector3 lookAtPos = targetTransform.position + targetTransform.forward * Mathf.Max(interpolator, debugInputValue) * offset.z;
        lookAtPos += shakeOffset;
        
        this.transform.LookAt(lookAtPos);
        
        // Position our transform a distance away from the target on local Z
        this.transform.position = targetTransform.position - this.transform.forward * offset.y;
    }

    public void Shake(ShakeParameters shakeParams)
    {
        if (shakeInstance != null && !shakeInstance.IsFinished)
            return;
        
        shakeInstance = shaker.Shake(shakeParams, UnityEngine.Random.Range(-10000, 10000));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(this.transform.position, this.transform.position + this.transform.forward * 100F);
        
        if (offset.x is > 0 or < 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(targetTransform.position, targetTransform.position + targetTransform.right * offset.x);
        }

        if (offset.y is > 0 or < 0)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(targetTransform.position, targetTransform.position + targetTransform.up * offset.y);
        }

        if (offset.z is > 0 or < 0)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(targetTransform.position, targetTransform.position + targetTransform.forward * offset.z);
        }
    }

    // public void UpdateInputValue(float value)
    // {
    //     this.inputValue = value;
    // }
}
