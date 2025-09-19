using UnityEngine;

public class VehicleInputManager : MonoBehaviour
{
    public float moveInput { get; private set; } = 0F;
    public float turnInputValue { get; private set; } = 0.0f;
    public int rotationInput => turnInputValue > 0 ? 1 : turnInputValue < 0 ? -1 : 0;
    [field: SerializeField] public int intMoveInput => (int)moveInput;

    
    private void FixedUpdate()
    {
        moveInput = Input.GetAxisRaw("Vertical");
        turnInputValue = Input.GetAxis("Horizontal");
    }
}