using UnityEngine;

public class VehicleInputManager : MonoBehaviour
{
    public enum InputState
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
    public InputState vehicleState => (rotationInput, moveInput) switch
    {
        (0, 0) => InputState.None,
        (0, -1) => InputState.MovingBackward,
        (-1 or 1, -1) => InputState.MovingBackwardAndRotating,
        (0, 1) => InputState.MovingForward,
        (-1 or 1, 1) => InputState.MovingForwardAndRotating,
        (-1 or 1, 0) => InputState.Rotating,
        _ => InputState.None
    };
    public void SetLastInputState() => lastInputState =  vehicleState;
    public InputState lastInputState;
    public float MouseXDelta => Input.GetAxis("Mouse X");
    public float MouseYDelta => Input.GetAxis("Mouse Y");
    
    
    private void FixedUpdate()
    {
        moveInput = Input.GetAxisRaw("Vertical");
        turnInputValue = Input.GetAxis("Horizontal");
    }
}