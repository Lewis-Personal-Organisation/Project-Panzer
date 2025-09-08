using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class Extensions
{
    // public static bool IsNearZero(this float value) => Mathf.Abs(value) < Mathf.Epsilon;
    
    public static  bool IsNearZero(this float value) => Mathf.Abs(value) < Mathf.Epsilon && Mathf.Abs(value) > -Mathf.Epsilon;
    public static bool IsNearValue(this float value, float target) => Mathf.Abs(value) + Mathf.Epsilon > Mathf.Abs(target) || Mathf.Abs(value) - Mathf.Epsilon < Mathf.Abs(target);

    public static Vector3 Clamp(Vector3 original, float maxX, float maxY, float maxZ)
    {
        return new Vector3(Mathf.Clamp(original.x, original.x, maxX),
            Mathf.Clamp(original.y, original.y, maxY),
            Mathf.Clamp(original.z, original.z, maxZ));
    }

    public static float ForceForTopSpeedOverTime(float mass, float targetSpeed, float accelTime)
    {
        return mass * (targetSpeed / accelTime);
    }
    
    public static Vector3 ApplyForceToReachSpeed(Vector3 velocity, float mass, Vector3 direction, float targetSpeed, float time)
    {
        direction = direction.normalized;
        float currentSpeed = Vector3.Dot(velocity, direction);
        float requiredAccel = (targetSpeed - currentSpeed) / time;
        float gravityComp = Vector3.Dot(Physics.gravity, direction);
        float totalAccel = requiredAccel - gravityComp;

        return direction * totalAccel * mass;
    }
}
