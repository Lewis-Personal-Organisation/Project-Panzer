using System;
using System.Collections;
using System.Collections.Generic;
using MiniTanks;
using UnityEngine;
using UnityEngine.Events;

public class TankMovementCollider : MonoBehaviour
{
    // [SerializeField] private TankController tankController;
    // public enum ColliderLocation
    // {
    //     Left,
    //     Right
    // }
    //
    // [SerializeField] private ColliderLocation colliderLocation;
    //
    // [SerializeField] public UnityAction OnColliderEnter;
    // [SerializeField] public UnityAction OnColliderExit;
    //
    //
    // private void Awake()
    // {
    //     if (tankController == null)
    //     {
    //         tankController = transform.root.GetComponent<TankController>();
    //     }
    //     
    //     OnColliderEnter += colliderLocation switch
    //     {
    //         ColliderLocation.Left => () => tankController.isLeftGrounded = true,
    //         ColliderLocation.Right => () => tankController.isRightGrounded = true,
    //     };
    //     
    //     OnColliderExit += colliderLocation switch
    //     {
    //         ColliderLocation.Left => () => tankController.isLeftGrounded = false,
    //         ColliderLocation.Right => () => tankController.isRightGrounded = false,
    //     };
    // }
    //
    // private void OnCollisionEnter(Collision other)
    // {
    //     Debug.Log($"Touching {other.gameObject.name}");
    //     OnColliderEnter?.Invoke();
    // }
    //
    // private void OnCollisionExit(Collision other)
    // {
    //     Debug.Log($"Seperated from {other.gameObject.name}");
    //     OnColliderExit?.Invoke();
    // }
}
