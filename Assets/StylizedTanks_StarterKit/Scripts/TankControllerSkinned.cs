using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Timeline;


namespace MiniTanks
{
    public class TankControllerSkinned : MonoBehaviour
    {
        public Camera viewCamera;

        [Header("Tank parts")]
        public Transform hullBoneTransform;
        public Transform turretTransform;
        public Transform hullTransform;
        public Transform trackTransform;
        public Transform targetTransform;

        [Header("Color offset")]
        [Range(1, 12)]
        public int teamColor = 1;
        [Header("Tank Parameters")]
        public float Speed = 10.0f;
        public float TurnSpeed = 180.0f;
        public float turretSpeed = 240.0f;
        public float trackMultiplier = 0.75f;
        [Header("Lean Settings")]
        public float HleanSpeed = 8.0f;
        public float VleanSpeed = 6.0f;
        public float HMaxLean = 15.0f;
        public float VMaxLean = 15.0f;

        private Material trackMaterial;
        private Material hullMaterial;
        private Rigidbody Rigidbody;

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


        // Start is called before the first frame update
        void Start()
        {
            Rigidbody = GetComponent<Rigidbody>();
            trackMaterial = trackTransform.GetComponent<Renderer>().material;
            hullMaterial = hullTransform.GetComponent<Renderer>().material;

            // Get and Set the materials ColorOffset
            /*
            paintMaterials = transform.GetComponentsInChildren<Renderer>();
            
            foreach (Renderer renderer in paintMaterials)
            {
                renderer.material.SetFloat("_ColorOffset", (teamColor - 1));
            }*/
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
            trackOffset += MovementInputValue * Speed * Time.deltaTime * trackMultiplier;
            trackOffset %= 1.0f;
            trackMaterial.SetFloat("_TrackOffset", trackOffset);

            // Lean the hull based on movement inputs
            if (Input.GetButtonDown("Vertical"))
            {
                targetHullForwardLean = -VMaxLean * Input.GetAxisRaw("Vertical");
                isForward = Input.GetAxisRaw("Vertical");
            }

            if (Input.GetButtonUp("Vertical"))
            {
                targetHullForwardLean = VMaxLean * isForward;
            }

            // Set the material offset when a numerical key pressed
            if (Input.inputString != "")
            {
                int number;
                bool is_a_number = Int32.TryParse(Input.inputString, out number);
                if (is_a_number && number >= 1 && number < 10)
                {
                    hullMaterial.SetFloat("_ColorOffset", (number - 1));
                    /*
                    foreach (Renderer renderer in paintMaterials)
                    {
                        renderer.material.SetFloat("_ColorOffset", (number - 1));
                    }*/
                }
            }

        }

        private void FixedUpdate()
        {
            // move tank
            Vector3 movement = MovementInputValue * Speed * Time.deltaTime * transform.forward;
            Rigidbody.MovePosition(Rigidbody.position + movement);

            // rotate tank
            float turn = TurnInputValue * TurnSpeed * Time.deltaTime;
            Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
            Rigidbody.MoveRotation(Rigidbody.rotation * turnRotation);

            // rotate turret
            targetPosition = hullBoneTransform.worldToLocalMatrix.MultiplyPoint(targetPosition);
            targetPosition.y = 0f;
            Quaternion rotTarget = Quaternion.LookRotation(targetPosition);
            turretTransform.localRotation = Quaternion.RotateTowards(turretTransform.localRotation, rotTarget, Time.deltaTime * turretSpeed);

            // hull lean
            targetHullLean = -TurnInputValue * HMaxLean;

            actHullLean = Mathf.Lerp(actHullLean, targetHullLean, Time.deltaTime * HleanSpeed);
            actHullForwardLean = Mathf.Lerp(actHullForwardLean, targetHullForwardLean, Time.deltaTime * VleanSpeed);

            if (Mathf.Abs(actHullForwardLean) >= Mathf.Abs(targetHullForwardLean) - 1.0f)
            { targetHullForwardLean = 0.0f; }

            hullBoneTransform.localRotation = Quaternion.Euler(actHullForwardLean, 0, actHullLean * MovementInputValue);

        }
    }
}
