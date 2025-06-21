using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Serialization;
using UnityEngine.Timeline;

namespace MiniTanks
{
    [RequireComponent(typeof(Rigidbody))]
    public class TankController : MonoBehaviour
    {
        public Camera viewCamera;

        [Header("Tank parts")]
        public Transform hullBoneTransform;
        public Transform turretTransform;
        public Transform trackTransform;
        public Transform targetTransform;

        [Header("Color offset")]
        [Range(1, 12)]
        public int teamColor = 1;
        [FormerlySerializedAs("vehicleData")]
        [FormerlySerializedAs("vehicleGameplayData")]
        [Header("Tank Parameters")]
        public VehicleGameplayData data;
        float speedMulti => MovementInputValue >= 0 ? data.mobility.forwardSpeed : data.mobility.backwardSpeed;

        private Material trackMaterial;
        private Rigidbody hullRigidbody;

        private float MovementInputValue = 0.0f;
        private float TurnInputValue = 0.0f;
        private Vector3 targetPosition;

        private float trackOffset = 0.0f;
        private float targetHullLean = 0.0f;
        private float actHullLean = 0.0f;
        private float targetHullForwardLean = 0.0f;
        private float actHullForwardLean = 0.0f;
        private float isForward = 0.0f;

        private Renderer[] paintMaterials;

        private void Reset()
        {
            if (hullRigidbody == null)
                hullRigidbody = GetComponent<Rigidbody>();
        }

        // Start is called before the first frame update
        void Start()
        {
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
            Ray screenRay = viewCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(screenRay, out hit))
            {
                targetPosition = hit.point;
            }

            // move the target object to the hit position
            targetTransform.position = targetPosition;

            // Get input values for movement
            MovementInputValue = Input.GetAxis("Vertical");
            TurnInputValue = Input.GetAxis("Horizontal");

            // Set track offset to match the movement
            trackOffset += MovementInputValue * speedMulti * Time.deltaTime * data.mobility.trackMultiplier;
            trackOffset %= 1.0f;
            trackMaterial.SetFloat("_TrackOffset", trackOffset);

            // Lean the hull based on movement inputs
            if (Input.GetButtonDown("Vertical"))
            {
                isForward = Input.GetAxisRaw("Vertical");
                targetHullForwardLean = -data.mobility.VMaxLean * isForward;
            }

            if (Input.GetButtonUp("Vertical"))
            {
                targetHullForwardLean = data.mobility.VMaxLean * isForward;
            }

            // Set the material offset when a numerical key pressed
            if (Input.inputString != "")
            {
                int number;
                bool isNumber = Int32.TryParse(Input.inputString, out number);
                if (isNumber && number >= 1 && number < 10)
                {
                    foreach (Renderer renderer in paintMaterials)
                    {
                        renderer.material.SetFloat("_ColorOffset", (number - 1));
                    }
                }
            }

        }

        private void FixedUpdate()
        {
            // Rotate tank
            float yRotation = TurnInputValue * data.mobility.TurnSpeed * Time.deltaTime;
            Quaternion turnRotation = Quaternion.Euler(0f, yRotation, 0f);
            hullRigidbody.MoveRotation(hullRigidbody.rotation * turnRotation);

            // Move Tank with/without neutral steering influence
            if (data.mobility.neutralSteeringInfluence <= 0)
            {
                hullRigidbody.MovePosition(hullRigidbody.position + MovementInputValue * speedMulti * Time.deltaTime * transform.forward);
            }
            else
            {
                float inputSpeed = MovementInputValue * speedMulti; // The input speed, no delta

                // If turning and input speed is more than neutral steer speed force neutral steering speed. Else, use input speed
                if (yRotation is < 0.0f or > 0.0F && Mathf.Abs(inputSpeed) < data.mobility.neutralSteeringInfluence)
                {
                    hullRigidbody.MovePosition(hullRigidbody.position + data.mobility.neutralSteeringInfluence * Time.deltaTime * transform.forward);
                }
                else if (Mathf.Abs(inputSpeed) > data.mobility.neutralSteeringInfluence)
                {
                    hullRigidbody.MovePosition(hullRigidbody.position + inputSpeed * Time.deltaTime * transform.forward);
                }
            }

            // Rotate turret
            targetPosition = hullBoneTransform.worldToLocalMatrix.MultiplyPoint(targetPosition);
            targetPosition.y = 0f;
            Quaternion rotTarget = Quaternion.LookRotation(targetPosition);
            turretTransform.localRotation = Quaternion.RotateTowards(turretTransform.localRotation, rotTarget, Time.deltaTime * data.mobility.turretSpeed);

            // Hull lean
            targetHullLean = -TurnInputValue * data.mobility.HMaxLean;
            actHullLean = Mathf.Lerp(actHullLean, targetHullLean, Time.deltaTime * data.mobility.HleanSpeed);
            actHullForwardLean = Mathf.Lerp(actHullForwardLean, targetHullForwardLean, Time.deltaTime * data.mobility.VleanSpeed);

            if (Mathf.Abs(actHullForwardLean) >= Mathf.Abs(targetHullForwardLean) - 1.0f)
            { targetHullForwardLean = 0.0f; }

            hullBoneTransform.localRotation = Quaternion.Euler(actHullForwardLean, 0, actHullLean * MovementInputValue);
        }
    }
}