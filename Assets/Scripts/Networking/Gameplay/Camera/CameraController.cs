using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviourExt
{
    private new Camera camera;
    public Transform trToLookAt;
    public Vector3 cachedPositionOffset;
    public Vector3 offset;
    public Vector3 shakeOffset;
    private float interpolator = 0;
    public float lerpSpeed = 1;
    private float inputValue = 0;

    
    private void Awake()
    {
        TryGetLocalComponent(ref camera);
        
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
        lookAtPos += shakeOffset;
        
        this.transform.LookAt(lookAtPos);
        
        // Position our transform a distance away from the target on local Z
        this.transform.position = trToLookAt.position - this.transform.forward * offset.y;
    }

    public void Shake()
    {
        if (shakeCoroutine == null)
        {
            shakeCoroutine = StartCoroutine(Shake(1, 1));
        }
    }
    
    private Coroutine shakeCoroutine = null;
    
    private IEnumerator Shake(float duration, float magnitude)
    {
        Debug.Log($"Starting shake");
        
        float elapsed = 0F;
        float step = 1F / duration;

        while (elapsed < duration)
        {
            elapsed += step * Time.deltaTime;
            float x = Random.Range(-1F, 1F) * magnitude;
            float y = Random.Range(-1F, 1F) * magnitude;
            shakeOffset = new Vector3(x, y, 0);
            Debug.Log($"Shaking {x}, {y}");
            yield return null;
        }
        
        shakeOffset =  Vector3.zero;
        shakeCoroutine = null;
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
