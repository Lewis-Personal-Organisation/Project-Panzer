using UnityEditor;
using UnityEngine;

public class VehicleGunAngleTester : MonoBehaviour
{
    [SerializeField] private Transform aimTransform;
    [SerializeField] float minAngleForRicochet;
    private RaycastHit hit;
    private bool hitObject = false;
    [SerializeField] private float bounceLength;
    [SerializeField] private LayerMask layer;
    
    [SerializeField] private Color shotColour;
    [SerializeField] private Color bounceColour;
    
    private Vector3 incomingDirection;
    private Vector3 surfaceNormal;

    [SerializeField] Extensions.ReflectResult result = new Extensions.ReflectResult();
    public float resultDebugHeight = 4;
    public float handleLineThickness = 5;
    private GUIStyle style;
    
    
    private void Awake()
    {
        style = new GUIStyle(EditorStyles.boldLabel) 
        {
            clipping = TextClipping.Overflow,
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18,
            wordWrap = false,
            fixedWidth = 0,
            fixedHeight = 0,
        };
        style.normal.textColor = Color.white;
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;
        
        // if (Mathf.Approximately(result.hitAngle, float.MinValue))
        //     return;

        if (!hitObject)
            return;
        
        Handles.color = shotColour;
        Handles.DrawLine(aimTransform.position, hit.point, handleLineThickness);

        if (result.didRicochet && result.direction.sqrMagnitude > 0.001f)
        {
            Handles.color = bounceColour;
            Handles.DrawLine(hit.point, hit.point + result.direction.normalized * bounceLength, handleLineThickness);
        }
        
        Handles.Label(
            hit.transform.position+ Vector3.up * resultDebugHeight,
            new GUIContent(
                $"Hit: {(result.didRicochet ? "Deflected" : "Absorbed")}\n" +
                $"Angle: {result.hitAngle.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}\n" +
                $"Side: {result.boxColliderHitSide}\n"),
            style);
    }

    private void Update()
    {
        hitObject = Physics.Raycast(aimTransform.position, aimTransform.forward, out hit, Mathf.Infinity, layer);
        
        if (hitObject)
        {
            if (hit.collider is BoxCollider boxCollider)
            {
                Debug.DrawLine(aimTransform.position, hit.point, shotColour, float.MinValue);
                result = boxCollider.ReflectWithAngleAdvFromDirection(hit.point, aimTransform.forward, minAngleForRicochet);
                return;
            }
        }

        result.didRicochet = false;
        result.hitAngle = float.MinValue;
    }
}
