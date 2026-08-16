using System;
using UnityEngine;
using UnityEngine.Serialization;

public class VehicleInputManager : MonoBehaviour
{
    public enum TraversalState
    {
        None,
        MovingForward,
        MovingBackward,
        MovingBackwardAndRotating,
        MovingForwardAndRotating,
        Rotating,
    };
    
    public float moveInput { get; private set; } = 0F;
    public float turnInputValue { get; private set; } = 0.0f;
    public int rotationInput => turnInputValue > 0 ? 1 : turnInputValue < 0 ? -1 : 0;
    public TraversalState vehicleState => (rotationInput, moveInput) switch
    {
        (0, 0) => TraversalState.None,
        (0, -1) => TraversalState.MovingBackward,
        (-1 or 1, -1) => TraversalState.MovingBackwardAndRotating,
        (0, 1) => TraversalState.MovingForward,
        (-1 or 1, 1) => TraversalState.MovingForwardAndRotating,
        (-1 or 1, 0) => TraversalState.Rotating,
        _ => TraversalState.None
    };
    public void SetLastInputState() => lastTraversalState = vehicleState;
    [FormerlySerializedAs("lastInputState")]
    public TraversalState lastTraversalState;
    public float MouseXDelta => Input.GetAxis("Mouse X");
    public float MouseYDelta => Input.GetAxis("Mouse Y");
    public bool lmbPressed = false;
    
    
    private void FixedUpdate()
    {
        moveInput = Input.GetAxisRaw("Vertical");
        turnInputValue = Input.GetAxis("Horizontal");
        lmbPressed = Input.GetMouseButtonDown(0);
    }

    private void OnDisable()
    {
        moveInput = 0;
        turnInputValue = 0;
        lmbPressed = false;
    }
}