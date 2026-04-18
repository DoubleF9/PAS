using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class SimplePlayerCar : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 20f;
    public float turnSpeed = 100f;
    
    [Header("Visual Wheels (Optional)")]
    public Transform frontLeftWheel;
    public Transform frontRightWheel;
    public float maxVisualSteerAngle = 30f;

    private Rigidbody rb;
    private float moveInput;
    private float turnInput;

    // We store the original local rotations to avoid gimbal lock or accumulating rotations
    private Quaternion flOriginalRot;
    private Quaternion frOriginalRot;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Make it behave like a simple object, not a complex physics vehicle
        rb.mass = 1000f;
        rb.linearDamping = 2f; 
        rb.angularDamping = 2f;
        rb.centerOfMass = new Vector3(0, -0.5f, 0); // Keep it from flipping easily

        if (frontLeftWheel != null) flOriginalRot = frontLeftWheel.localRotation;
        if (frontRightWheel != null) frOriginalRot = frontRightWheel.localRotation;
    }

    void Update()
    {
        moveInput = 0;
        turnInput = 0;

        // Try Input System first
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveInput += 1;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveInput -= 1;

            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) turnInput -= 1;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) turnInput += 1;
        }
        else
        {
            // Fallback to old input manager just in case
            moveInput = Input.GetAxisRaw("Vertical");
            turnInput = Input.GetAxisRaw("Horizontal");
        }

        UpdateVisualSteering();
    }

    void FixedUpdate()
    {
        // 1. Move Forward/Backward
        if (Mathf.Abs(moveInput) > 0.05f)
        {
            Vector3 moveForce = transform.forward * moveInput * moveSpeed;
            rb.AddForce(moveForce, ForceMode.Acceleration);
        }

        // 2. Turn Left/Right
        // Only allow turning if the car is actually moving
        if (rb.linearVelocity.magnitude > 0.5f || Mathf.Abs(moveInput) > 0.05f)
        {
            // Reverse steering direction when going backwards
            float turnMultiplier = moveInput >= 0 ? 1f : -1f;
            float rotationAmount = turnInput * turnSpeed * turnMultiplier * Time.fixedDeltaTime;
            
            Quaternion turnRotation = Quaternion.Euler(0, rotationAmount, 0);
            rb.MoveRotation(rb.rotation * turnRotation);
        }

        // 3. Fake Grip (Stop sideways sliding)
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        localVelocity.x *= 0.1f; // Kill 90% of sideways velocity
        rb.linearVelocity = transform.TransformDirection(localVelocity);
        
        // 4. Downforce (Keep it on the ground)
        rb.AddForce(Vector3.down * 50f, ForceMode.Force);
    }

    void UpdateVisualSteering()
    {
        float targetSteerAngle = turnInput * maxVisualSteerAngle;

        if (frontLeftWheel != null)
        {
            frontLeftWheel.localRotation = flOriginalRot * Quaternion.Euler(0, targetSteerAngle, 0);
        }

        if (frontRightWheel != null)
        {
            frontRightWheel.localRotation = frOriginalRot * Quaternion.Euler(0, targetSteerAngle, 0);
        }
    }
}
