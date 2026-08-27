using System.Reflection;
using Unity.Collections;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;

public static class Extensions
{
    #region Float

    public static bool IsNearZero(this float value) => Mathf.Abs(value) < Mathf.Epsilon && Mathf.Abs(value) > -Mathf.Epsilon;
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
        public TankSide tankSide;
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

        UnityEngine.Debug.Log(halfSize);
        
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
                return boxCollider.transform.up;
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
    
    public static Vector3 ClosestSideFromDirection(this BoxCollider boxCollider, Vector3 incomingDirection)
    {
        Vector3 localDir = boxCollider.transform.InverseTransformDirection(incomingDirection).normalized;

        float x = Mathf.Abs(localDir.x);
        float y = Mathf.Abs(localDir.y);
        float z = Mathf.Abs(localDir.z);

        if (x > y && x > z)
            return localDir.x > 0 ? -boxCollider.transform.right : boxCollider.transform.right;
        else if (y > z)
            return localDir.y > 0 ? -boxCollider.transform.up : boxCollider.transform.up;
        else
            return localDir.z > 0 ? -boxCollider.transform.forward : boxCollider.transform.forward;
    }

    public enum TankSide
    {
        Front,
        Right,
        Back,
        Left
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
    /// Returns a Reflect result containing the direction and ricochet state.
    /// Uses the shell's actual travel direction (not its transform.forward or position)
    /// to determine which face was struck, avoiding tunneling misclassification.
    /// </summary>
    public static ReflectResult ReflectWithAngleAdvFromDirection(this BoxCollider boxCollider, Vector3 incomingDirection, float ricochetAngle)
    {
        incomingDirection = incomingDirection.normalized;

        Vector3 surfaceNormal = boxCollider.ClosestSideFromDirection(incomingDirection);

        bool didReflect = Vector3.Angle(incomingDirection, -surfaceNormal) > ricochetAngle;

        float dotForward = Vector3.Dot(surfaceNormal, boxCollider.transform.forward);
        float dotRight = Vector3.Dot(surfaceNormal, boxCollider.transform.right);
        float dotBack = Vector3.Dot(surfaceNormal, -boxCollider.transform.forward);
        float dotLeft = Vector3.Dot(surfaceNormal, -boxCollider.transform.right);

        float maxDot = Mathf.Max(dotForward, dotRight, dotBack, dotLeft);
        
        TankSide tankSide = maxDot switch
        {
            var d when d == dotForward => TankSide.Front,
            var d when d == dotRight => TankSide.Right,
            var d when d == dotBack => TankSide.Back,
            _ => TankSide.Left
        };
        
        // UnityEngine.Debug.Log($"{Vector3.Angle(incomingDirection, -surfaceNormal)} > {ricochetAngle} ? {didReflect}, {tankSide}, {incomingDirection}"); // DEBUG LINE ONLY

        return new ReflectResult
        {
            didRicochet = didReflect,
            direction = didReflect ? Vector3.Reflect(incomingDirection, surfaceNormal) : surfaceNormal,
            tankSide = tankSide
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

    // The 'this' keyword attaches this function to the Transform class
    public static bool MoveTowards(this Transform transform, Vector3 target, float maxDistDelta)
    {
        Vector3 currentPosition = transform.position;
        Vector3 direction = target - currentPosition;
        float dist = direction.magnitude;

        // Check if we are already there or will arrive this frame
        if (dist <= maxDistDelta || dist == 0f)
        {
            transform.position = target;
            return true; // Returns true to signify it reached the destination
        }

        // Move the transform closer
        transform.position = currentPosition + (direction / dist) * maxDistDelta;
        return false; // Returns false because it is still moving
    }

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

    #region Camera

    private static Plane[] planes = new Plane[6];
    /// <summary>
    /// Check if the camera can see the bounds of a Mesh
    /// </summary>
    /// <param name="renderer">The renderer holding the mesh</param>
    /// <param name="camera">The target Camera</param>
    /// <returns></returns>
    public static bool CanSeeBounds(this Camera camera, Renderer renderer)
    {
        if (!renderer)
            return false;

        if (!renderer.enabled)
            return false;

        if (!renderer.gameObject.activeInHierarchy)
            return false;

        planes = GeometryUtility.CalculateFrustumPlanes(camera);

        return GeometryUtility.TestPlanesAABB(planes, renderer.bounds);
    }

    #endregion

    #region FixedStringBytes

    public static bool TryAppendChar(ref FixedString32Bytes str, char c)
    {
        // Compute UTF-8 byte length for the character
        int requiredBytes = char.IsHighSurrogate(c) || char.IsLowSurrogate(c) ? 2 : (c > 0x7F ? 2 : 1);

        // Check if there is enough space remaining (Capacity - Length)
        if (str.Capacity - str.Length >= requiredBytes)
        {
            str.Append(c);
            return true;
        }

        return false; // Not enough room, handled safely without throwing
    }

    public static unsafe bool TryPrependByte(ref this FixedString32Bytes str, byte value)
    {
        if (str.Length >= str.Capacity) return false;

        byte* ptr = str.GetUnsafePtr();

        if (str.Length > 0)
        {
            UnsafeUtility.MemMove(ptr + 1, ptr, str.Length);
        }

        ptr[0] = value;
        str.Length++;

        return true;
    }
    
    public static unsafe bool TryRemoveFirstByte(ref this FixedString32Bytes str)
    {
        if (str.Length == 0) return false;

        byte* ptr = str.GetUnsafePtr();
        int newLength = str.Length - 1;

        if (newLength > 0)
        {
            // Shift remaining bytes left by 1 byte
            UnsafeUtility.MemMove(ptr, ptr + 1, newLength);
        }

        str.Length = (ushort)newLength;
        return true;
    }
    
    public static void Split(this FixedString32Bytes input, char delimiter, ref NativeList<FixedString32Bytes> results)
    {
        results.Clear();
        
        FixedString32Bytes currentPiece = default;
        
        // Loop through each UTF-8 rune/character in the fixed string
        foreach (var rune in input)
        {
            // Convert rune to uint value for comparison with the character
            if (rune.value == delimiter)
            {
                // Push the current slice to the results and clear for the next part
                results.Add(currentPiece);
                currentPiece.Clear();
            }
            else
            {
                // Append non-delimiter characters to the current slice
                currentPiece.Append(rune);
            }
        }

        // Add the remaining string slice after the last delimiter
        results.Add(currentPiece);
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
