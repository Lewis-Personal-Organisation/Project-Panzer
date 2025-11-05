using System.Reflection;
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
    public static Vector3 ReplaceX(this Vector3 original, float x)
    {
        return new Vector3(x, original.y, original.z);
    }
    public static Vector3 ReplaceY(this Vector3 original, float y)
    {
        return new Vector3(original.x, y, original.z);
    }
    public static Vector3 ReplaceZ(this Vector3 original, float z)
    {
        return new Vector3(original.x, original.y, z);
    }
    #endregion

    #region Colliders

    #region Box Colliders
    
    /// <summary>
    /// The ReflectResult struct. Contains info about a shell Ricochet
    /// </summary>
    public struct ReflectResult
    {
        public bool didRicochet;
        public Vector3 direction;
    }
    
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
    
    /// <summary>
    /// Returns a Vector3 Indicating the closes side of a boxCollider regarding a world position
    /// </summary>
    /// <param name="boxCollider"></param>
    /// <param name="worldPos"></param>
    /// <returns></returns>
    public static Vector3 ClosestSide(this BoxCollider boxCollider, Vector3 worldPos)
    {
        Vector3 localPos = boxCollider.transform.InverseTransformPoint(worldPos) - boxCollider.center;

        // Scale relative to box size (so non-square boxes are handled correctly)
        Vector3 halfSize = boxCollider.size * 0.5f;
        float x = localPos.x / halfSize.x;
        float y = localPos.y / halfSize.y;
        float z = localPos.z / halfSize.z;

        // Find the axis with the largest absolute value
        if (Mathf.Abs(x) > Mathf.Abs(y) && Mathf.Abs(x) > Mathf.Abs(z))
        {
            if (x > 0)
                return boxCollider.transform.right;
            else
                return boxCollider.transform.right * -1;
        }
        else if (Mathf.Abs(y) > Mathf.Abs(z))
        {
            if (y > 0)
                return Vector3.up;
            else
                return boxCollider.transform.up * -1;
        }
        else
        {
            if (z > 0)
                return boxCollider.transform.forward;
            else
                return boxCollider.transform.forward * -1;
        }
    }

    /// <summary>
    /// Returns whether a transform should be reflected and applies the result.
    /// Reflection occurs when the targetTransform angle is at or above the specified angle
    /// </summary>
    /// <param name="boxCollider"></param>
    /// <param name="targetTransform"></param>
    /// <param name="ricochetAngle"></param>
    /// <returns></returns>
    public static bool ReflectWithAngle(this BoxCollider boxCollider, Transform targetTransform, float ricochetAngle)
    {
        Vector3 sideDirection = boxCollider.ClosestSide(targetTransform.position);
    
        if (!(Mathf.Abs(180F - Vector3.Angle(targetTransform.forward, sideDirection)) > ricochetAngle))
            return false;
        
        targetTransform.forward = Vector3.Reflect(targetTransform.forward.normalized, sideDirection.normalized);
        return true;
    }
    
    /// <summary>
    /// Returns a Reflect result containing the direction and ricochet state
    /// </summary>
    /// <param name="boxCollider"></param>
    /// <param name="targetTransform"></param>
    /// <param name="ricochetAngle"></param>
    /// <returns></returns>
    public static ReflectResult ReflectWithAngleAdv(this BoxCollider boxCollider, Transform targetTransform, float ricochetAngle)
    {
        Vector3 surfaceNormal = boxCollider.ClosestSide(targetTransform.position).normalized;

        bool didReflect = Vector3.Angle(targetTransform.forward, -surfaceNormal) > ricochetAngle;
        
        return new ReflectResult
        {
            didRicochet = didReflect,
            direction = didReflect ? Vector3.Reflect(targetTransform.forward, surfaceNormal) : surfaceNormal
        };
    }
    #endregion
    
    #endregion

    #region Transform

    // public static Vector3 OrbitAroundY(this Transform t, Vector3 orbitOffset, Vector3 target, float angle)
    // {
    //     orbitOffset = Quaternion.AngleAxis(angle, Vector3.up) * orbitOffset;
    //     t.position = target + orbitOffset;
    //     return orbitOffset;
    // }

    #endregion
    
    #region Rigidbody

    public static void PivotAroundPoint(this Rigidbody rb, Vector3 positionOffset, Vector3 axis, float forceMagnitude)
    {
        Vector3 pivotDirection = positionOffset - rb.position;
        Vector3 perpendicular = Vector3.Cross(axis, pivotDirection).normalized;
        rb.AddForce(perpendicular * forceMagnitude, ForceMode.Force);
        UnityEngine.Debug.Log(perpendicular * forceMagnitude);
    }
    #endregion
    
    public static class Debug
    {
        /// <summary>
        /// Clears the Unity Console Window
        /// </summary>
        public static void ClearConsole()
        {
#if UNITY_EDITOR
            var assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
            var type = assembly.GetType("UnityEditor.LogEntries");
            var method = type.GetMethod("Clear");
            method.Invoke(new object(), null);
#endif
        }
    }
}
