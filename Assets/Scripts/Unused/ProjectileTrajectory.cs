using UnityEngine;

/// <summary>
/// Generates a random landing point within a radius of a target Transform,
/// then computes the launch velocity required to hit that point on a
/// parabolic arc with a specified apex height.
///
/// Velocity is solved analytically (not iteratively), so the resulting
/// trajectory is exact for the given gravity value, assuming no drag.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ProjectileTrajectory : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private float minRadius = 1f;
    [SerializeField] private float maxRadius = 3f;

    [Header("Arc Shape")]
    [Tooltip("Minimum height of the arc's apex above the HIGHER of the launch/landing points.")]
    [SerializeField] private float apexHeight = 5f;
    [Tooltip("Positive value. Should match the magnitude used by the Rigidbody (Physics.gravity.magnitude unless overridden).")]
    [SerializeField] private float gravity = 9.81f;

    [Header("Preview (optional)")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private int previewResolution = 30;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Picks a random point in an annulus (minRadius..maxRadius) around the target,
    /// on the target's horizontal plane.
    /// </summary>
    public Vector3 GenerateRandomLandingPoint()
    {
        if (target == null)
        {
            Debug.LogError("ProjectileTrajectory: no target assigned.", this);
            return transform.position;
        }

        Vector2 dir2D = Random.insideUnitCircle.normalized;
        float radius = Random.Range(minRadius, maxRadius);
        Vector3 offset = new Vector3(dir2D.x, 0f, dir2D.y) * radius;

        return target.position + offset;
    }

    /// <summary>
    /// Generates a random landing point around the target and launches this
    /// object's Rigidbody so it lands there.
    /// </summary>
    [ContextMenu("Launch At Random Point")]
    public void LaunchAtRandomPointAroundTarget()
    {
        Vector3 landingPoint = GenerateRandomLandingPoint();

        Vector3 launchVelocity = CalculateLaunchVelocity(
            transform.position,
            landingPoint,
            apexHeight,
            gravity,
            out float flightTime);
        
        rb.velocity = launchVelocity;

        if (lineRenderer != null)
        {
            DrawTrajectoryPreview(transform.position, launchVelocity, gravity, flightTime);
        }
    }

    /// <summary>
    /// Solves for the initial velocity needed to travel from start to end,
    /// passing through an apex at least apexHeight above the higher endpoint,
    /// under constant downward gravity. Ignores drag.
    /// </summary>
    public static Vector3 CalculateLaunchVelocity(
        Vector3 start,
        Vector3 end,
        float apexHeight,
        float gravityMagnitude,
        out float totalFlightTime)
    {
        float deltaY = end.y - start.y;
        Vector3 deltaXZ = new Vector3(end.x - start.x, 0f, end.z - start.z);

        // Apex must clear whichever endpoint is higher, plus a small margin
        // so timeDown's sqrt argument never goes negative when deltaY > 0.
        float effectiveApex = Mathf.Max(apexHeight, deltaY) + 0.01f;

        float launchVelocityY = Mathf.Sqrt(2f * gravityMagnitude * effectiveApex);
        float timeToApex = launchVelocityY / gravityMagnitude;
        float timeFromApexToLanding = Mathf.Sqrt(2f * (effectiveApex - deltaY) / gravityMagnitude);

        totalFlightTime = timeToApex + timeFromApexToLanding;

        Vector3 launchVelocityXZ = deltaXZ / totalFlightTime;

        return launchVelocityXZ + Vector3.up * launchVelocityY;
    }

    /// <summary>
    /// Samples the analytic trajectory (kinematics, not simulation) into a LineRenderer.
    /// </summary>
    private void DrawTrajectoryPreview(Vector3 start, Vector3 velocity, float gravityMagnitude, float duration)
    {
        lineRenderer.positionCount = previewResolution;
        Vector3 gravityVec = Vector3.down * gravityMagnitude;

        for (int i = 0; i < previewResolution; i++)
        {
            float t = duration * (i / (float)(previewResolution - 1));
            Vector3 point = start + velocity * t + 0.5f * gravityVec * t * t;
            lineRenderer.SetPosition(i, point);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (target == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(target.position, minRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(target.position, maxRadius);
    }
}
