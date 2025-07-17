using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Serialization;
using UnityEngine.Timeline;

namespace MiniTanks
{
    [RequireComponent(typeof(Rigidbody))]
    public class TankController : NetworkBehaviour
    {
        [FormerlySerializedAs("viewCamera")] public CameraController cameraController;

        [Header("Tank parts")]
        [SerializeField] Transform hullBoneTransform;
        [SerializeField] Transform turretTransform;
        [SerializeField] Transform trackTransform;
        [SerializeField] Transform targetTransform;
        [SerializeField] private Transform movementCastPoint;
        private readonly RaycastHit[] movementCastHit = new RaycastHit[1];
        
        [Header("Color offset")]
        [Range(1, 12)]
        public int teamColor = 1;
        [Header("Tank Parameters")]
        [SerializeField] public VehicleType type;
        public TankMobility data;
        float speedMulti => forwardInputValue >= 0 ? data.forwardSpeed : data.backwardSpeed;
        private float inputSpeed;
        
        [SerializeField] private Material trackMaterial;
        [SerializeField] private Rigidbody hullRigidbody;

        private float forwardInputValue = 0.0f;
        private float turnInputValue = 0.0f;
        private Vector3 targetPosition;
        private float yRotationDelta;
        
        [SerializeField] private float trackOffset = 0.0f;
        private float targetHullLean = 0.0f;
        private float actHullLean = 0.0f;
        [SerializeField] private float targetHullForwardLean = 0.0f;
        [SerializeField] private float restingHullVertLean;
        private float actHullForwardLean = 0.0f;
        private float isForward = 0.0f;

        private Renderer[] paintMaterials;

        private const float MOVE_FORCE_MULTIPLIER = 50F;
        

        private void Awake()
        {
            Setup();
        }

        // Start is called before the first frame update
        public void Setup()
        {
            Debug.Log($"TankController :: Setup :: Are we host? {base.IsHost}");
            // viewCamera = Camera.main;
            
            if (!hullRigidbody)
                hullRigidbody = GetComponent<Rigidbody>();
            
            trackMaterial = trackTransform.GetComponent<Renderer>().material;

            // Get and Set the materials ColorOffset
            paintMaterials = transform.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in paintMaterials)
            {
                renderer.material.SetFloat("_ColorOffset", (teamColor - 1));
            }
        }

        // Update is called once per frame
        void Update()
        {
            // Hit the scene for target position
            Ray screenRay = cameraController.camera.ScreenPointToRay(Input.mousePosition);
            
            if (Physics.Raycast(screenRay, out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask("Ground", "PlayerAimDetectable")))
            {
                targetPosition = hit.point;
            }
            
            // move the target object to the hit position
            targetTransform.position = targetPosition;
            
            // Get input values for movement
            forwardInputValue = Input.GetAxis("Vertical");
            turnInputValue = Input.GetAxis("Horizontal");
            
            // Set track offset to match the movement
            trackOffset += forwardInputValue * speedMulti * Time.deltaTime * data.trackMultiplier;
            // Debug.Log($"Speed: {forwardInputValue * speedMulti}");
            trackOffset %= 1.0f;
            trackMaterial.SetFloat("_TrackOffset", trackOffset);
            
            // Lean the hull based on movement inputs
            if (Input.GetButtonDown("Vertical"))
            {
                isForward = Input.GetAxisRaw("Vertical");
                targetHullForwardLean = -data.verticalMaxLean * isForward;
            }
            
            if (Input.GetButtonUp("Vertical"))
            {
                targetHullForwardLean = data.verticalMaxLean * isForward;
            }
        }

        // [Rpc(SendTo.Server)]
        // void SubmitPositionRequestRpc(RpcParams rpcParams = default)
        // {
        //     var randomPosition  = new Vector3(UnityEngine.Random.Range(-0.03f, 0.03f), 0, UnityEngine.Random.Range(-0.03f, 0.03f));
        //     hullRigidbody.MovePosition(spawnPosition + randomPosition);
        //     Position.Value = hullRigidbody.position;
        // }
        //
        // [Rpc(SendTo.Server)]
        // void SubmitRotationRequestRpc(RpcParams rpcParams = default)
        // {
        //     var randomPosition  = new Vector3(UnityEngine.Random.Range(-0.03f, 0.03f), 0, UnityEngine.Random.Range(-0.03f, 0.03f));
        //     hullRigidbody.MovePosition(spawnPosition + randomPosition);
        //     Position.Value = hullRigidbody.position;
        // }

        private void FixedUpdate()
        {
            if (NetworkManager != null)
            {
                if (!IsOwner)
                    return;
            }
            else
            {
                // FIX FOR KINEMATIC BEING SET TRUE BY UNITY NETWORK
                hullRigidbody.isKinematic = false;
            }
            
            yRotationDelta = turnInputValue * data.TurnSpeed * Time.deltaTime;
            RotateTank();
            MoveTank();
            RotateTankTurret();
            ApplyTankLean();
            
            cameraController.inputValue = forwardInputValue;
        }

        private void RotateTank()
        {
            Quaternion turnRotation = Quaternion.Euler(0f, yRotationDelta, 0f);
            hullRigidbody.MoveRotation(hullRigidbody.rotation * turnRotation);
        }

        private void MoveTank()
        {
            // If no ground casts are found, dont move
            if (Physics.RaycastNonAlloc(movementCastPoint.position, movementCastPoint.forward, movementCastHit, 0.03F, LayerMask.GetMask("Ground", "PlayerAimDetectable")) == 0)
                return;
            
            inputSpeed = Mathf.MoveTowards(inputSpeed, forwardInputValue * speedMulti, data.speedChangeDelta * Time.deltaTime);
            
            // Move Tank with/without neutral steering influence
            if (data.neutralSteeringInfluence <= 0)
            {
                Debug.Log($"Setting Velocity (NO NEUTRAL): {this.transform.forward * (inputSpeed * Time.deltaTime)}");
                hullRigidbody.velocity = transform.forward * inputSpeed * MOVE_FORCE_MULTIPLIER * Time.deltaTime;
                // hullRigidbody.MovePosition(hullRigidbody.position + inputSpeed * Time.deltaTime * transform.forward);
            }
            else
            {
                // inputSpeed = movementInputValue * speedMulti; // The input speed, no delta

                // If turning and input speed is more than neutral steer speed force neutral steering speed. Else, use input speed
                if (yRotationDelta is < 0.0f or > 0.0F && Mathf.Abs(inputSpeed) < data.neutralSteeringInfluence)
                {
                    Debug.Log($"Setting Velocity A: {this.transform.forward * (inputSpeed * Time.deltaTime)}");
                    hullRigidbody.velocity = transform.forward * inputSpeed * MOVE_FORCE_MULTIPLIER * Time.deltaTime;
                    // hullRigidbody.MovePosition(hullRigidbody.position + data.neutralSteeringInfluence * Time.deltaTime * transform.forward);
                }
                else if (Mathf.Abs(inputSpeed) > data.neutralSteeringInfluence)
                {
                    Debug.Log($"Setting Velocity B: {this.transform.forward * (inputSpeed * Time.deltaTime)}");
                    hullRigidbody.velocity = transform.forward * inputSpeed * MOVE_FORCE_MULTIPLIER * Time.deltaTime;
                    // hullRigidbody.MovePosition(hullRigidbody.position + inputSpeed * Time.deltaTime * transform.forward);
                }
            }
        }

        private void RotateTankTurret()
        {
            // Rotate turret
            targetPosition = hullBoneTransform.worldToLocalMatrix.MultiplyPoint(targetPosition);
            targetPosition.y = 0f;
            Quaternion rotTarget = Quaternion.LookRotation(targetPosition);
            turretTransform.localRotation = Quaternion.RotateTowards(turretTransform.localRotation, rotTarget, Time.deltaTime * data.turretSpeed);
        }

        private void ApplyTankLean()
        {
            // Hull lean
            targetHullLean = -turnInputValue * data.horizontalMaxLean;
            actHullLean = Mathf.Lerp(actHullLean, targetHullLean, Time.deltaTime * data.horizontalLeanSpeed);
            actHullForwardLean = Mathf.Lerp(actHullForwardLean, targetHullForwardLean, Time.deltaTime * data.verticalLeanSpeed);

            if (Mathf.Abs(actHullForwardLean) >= Mathf.Abs(targetHullForwardLean) - 1.0f)
            {
                targetHullForwardLean = forwardInputValue > 0 ? restingHullVertLean * -1 : 0;
            }

            hullBoneTransform.localRotation = Quaternion.Euler(actHullForwardLean, 0, actHullLean * forwardInputValue);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(movementCastPoint.position, movementCastPoint.forward * 0.03F);
        }
    }
}