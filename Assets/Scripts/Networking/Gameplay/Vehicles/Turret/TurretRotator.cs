using UnityEngine;

public class TurretRotator : VehicleComponent
{
    [SerializeField] private new Camera camera;
    private Vector3 targetPosition;
    [SerializeField] private Transform turretTransform;
    [SerializeField] private Transform targetTransform;
    [SerializeField] private LayerMask castMask;

    private new void Awake()
    {
        base.Awake();
        TryGetLocalComponent(ref camera);
    }

    private void FixedUpdate()
    {
        AdjustMouseAimPosition();
        RotateTankTurret();
    }

    private void AdjustMouseAimPosition()
    {
        if (!Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, Mathf.Infinity, castMask.value)) return;
        
        targetPosition = hit.point;
            
        // move the target object to the hit position
        targetTransform.position = targetPosition;
    }

    private void RotateTankTurret()
    {
        // Rotate turret
        targetPosition = vehicle.hullBoneTransform.worldToLocalMatrix.MultiplyPoint(targetPosition);
        targetPosition.y = 0f;
        Quaternion targetRotation = Quaternion.LookRotation(targetPosition);
        turretTransform.localRotation = Quaternion.RotateTowards(turretTransform.localRotation, targetRotation, Time.deltaTime * vehicle.mobility.turretRotationSpeed);
    }
}
