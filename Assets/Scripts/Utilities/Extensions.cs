using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class Extensions
{
    #region Float
    
    public static  bool IsNearZero(this float value) => Mathf.Abs(value) < Mathf.Epsilon && Mathf.Abs(value) > -Mathf.Epsilon;
    public static bool IsNearValue(this float value, float target, float range) => Mathf.Abs(value) >= Mathf.Abs(target) - Mathf.Abs(range) && 
                                                                                   Mathf.Abs(value) <= Mathf.Abs(target) + Mathf.Abs(range);

    #endregion

    #region Vector3

    public static Vector3 Clamp(Vector3 original, float maxX, float maxY, float maxZ)
    {
        return new Vector3(Mathf.Clamp(original.x, original.x, maxX),
            Mathf.Clamp(original.y, original.y, maxY),
            Mathf.Clamp(original.z, original.z, maxZ));
    }

    #endregion

    #region Colliders

    #region Box Colliders

    /// <summary>
    /// Returns a point within the Box collider
    /// Uses values 0 to 1 where 0 and 1 are the respective opposite edges of the box.
    /// For example 'x = -1' is the left x-axis edge
    /// /// </summary>
    public static Vector3 PointAlongBounds(this BoxCollider box, float x = 0.5F, float y = 0.5F, float z = 0.5F)
    {
        Vector3 point = Vector3.zero;
        point.x = Mathf.Lerp(-box.size.x / 2, box.size.x / 2, x);
        point.y = Mathf.Lerp(-box.size.y / 2, box.size.y / 2, y);
        point.z = Mathf.Lerp(-box.size.z / 2, box.size.z / 2, z);
        
        return box.transform.TransformPoint(box.center + point);
    }

    #endregion
    
    #endregion
    
    #region Rigidbody

    public static void PivotAroundPoint(this Rigidbody rb, Vector3 positionOffset, Vector3 axis, float forceMagnitude)
    {
        Vector3 pivotDirection = positionOffset - rb.position;
        Vector3 perpendicular = Vector3.Cross(axis, pivotDirection).normalized;
        rb.AddForce(perpendicular * forceMagnitude, ForceMode.Force);
        Debug.Log(perpendicular * forceMagnitude);
    }
    #endregion

    // public static float ForceForTopSpeedOverTime(float mass, float targetSpeed, float accelTime)
    // {
    //     return mass * (targetSpeed / accelTime);
    // }
    
    // public static Vector3 ApplyForceToReachSpeed(Vector3 velocity, float mass, Vector3 direction, float targetSpeed, float time)
    // {
    //     direction = direction.normalized;
    //     float currentSpeed = Vector3.Dot(velocity, direction);
    //     float requiredAccel = (targetSpeed - currentSpeed) / time;
    //     float gravityComp = Vector3.Dot(Physics.gravity, direction);
    //     float totalAccel = requiredAccel - gravityComp;
    //
    //     return direction * totalAccel * mass;
    // }
}
