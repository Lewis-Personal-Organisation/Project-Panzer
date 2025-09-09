using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [field: SerializeField] public new Camera camera {private set; get; }
    public Transform trToLookAt;
    public Vector3 cachedPositionOffset;
    public Vector3 offset;
    private float interpolator = 0;
    public float lerpSpeed = 1;
    private float inputValue = 0;

    
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
        interpolator = Mathf.MoveTowards(interpolator, inputValue, Time.deltaTime * lerpSpeed);
        
        // Position is equal to the forward axis * offset (pos or neg)
        Vector3 lookAtPos = trToLookAt.position + Vector3.Slerp(Vector3.zero, trToLookAt.forward * (interpolator < 0 ? -offset.z : offset.z), Mathf.Abs(interpolator));
        this.transform.LookAt(lookAtPos);
        
        // Position our transform a distance away from the target on local Z
        this.transform.position = trToLookAt.position - this.transform.forward * offset.y;
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

    public void UpdateInputValue(float value)
    {
        this.inputValue = value;
    }
}
