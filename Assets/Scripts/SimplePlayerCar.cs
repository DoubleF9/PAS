using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[System.Serializable]
public class WheelInfo
{
    public WheelCollider collider;
    public Transform visualMesh;
    public bool isSteering;
    public bool isMotor;
}

[RequireComponent(typeof(Rigidbody))]
public class SimplePlayerCar : MonoBehaviour
{
    public List<WheelInfo> wheels = new List<WheelInfo>();
    public float motorTorque = 1500f;
    public float maxSteerAngle = 35f;
    public float brakeTorque = 5000f;

    private Rigidbody rb;
    private float moveInput;
    private float turnInput;
    private bool brakeInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Lower center of mass to prevent the car from rolling over easily in corners
        rb.centerOfMass = new Vector3(0, -0.2f, 0);

        // FORCE clear and rebuild to avoid any Unity Editor serialization bugs
        wheels.Clear();
        
        WheelCollider[] colliders = GetComponentsInChildren<WheelCollider>();
        Transform[] transforms = GetComponentsInChildren<Transform>();

        foreach (var wc in colliders)
        {
            string wcName = wc.gameObject.name.ToLower();
            Transform visual = null;
            bool isSteering = wcName.Contains("fl") || wcName.Contains("fr");
            bool isMotor = wcName.Contains("rl") || wcName.Contains("rr");

            // Find the corresponding visual mesh
            foreach (var t in transforms)
            {
                string tname = t.name.ToLower().Replace(" ", "");
                if ((wcName.Contains("fl") && tname.Contains("frontleftwheel")) ||
                    (wcName.Contains("fr") && tname.Contains("frontrightwheel")) ||
                    (wcName.Contains("rl") && tname.Contains("rearleftwheel")) ||
                    (wcName.Contains("rr") && tname.Contains("rearrightwheel")))
                {
                    visual = t;
                    break;
                }
            }

            if (visual != null)
            {
                WheelInfo info = new WheelInfo();
                info.collider = wc;
                info.visualMesh = visual;
                info.isSteering = isSteering;
                info.isMotor = isMotor;
                wheels.Add(info);
            }
        }
        
        Debug.Log("SimplePlayerCar: Wheels auto-linked. Count = " + wheels.Count);
    }

    void Update()
    {
        moveInput = 0;
        turnInput = 0;
        brakeInput = false;

        // Input System Support
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveInput += 1;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveInput -= 1;

            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) turnInput -= 1;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) turnInput += 1;
            
            if (Keyboard.current.spaceKey.isPressed) brakeInput = true;
        }
        else
        {
            // Fallback
            moveInput = Input.GetAxisRaw("Vertical");
            turnInput = Input.GetAxisRaw("Horizontal");
            brakeInput = Input.GetKey(KeyCode.Space);
        }

        UpdateVisuals();
    }

    void FixedUpdate()
    {
        float currentTorque = moveInput * motorTorque;
        float currentSteer = turnInput * maxSteerAngle;
        
        foreach (var wheel in wheels)
        {
            if (wheel.collider == null) continue;

            // Apply Steering
            if (wheel.isSteering)
            {
                wheel.collider.steerAngle = currentSteer;
            }

            // Apply Motor
            if (wheel.isMotor)
            {
                wheel.collider.motorTorque = currentTorque;
            }

            // Apply Brakes
            if (brakeInput)
            {
                wheel.collider.brakeTorque = brakeTorque;
                wheel.collider.motorTorque = 0;
            }
            else
            {
                wheel.collider.brakeTorque = 0;
            }
        }
    }

    void UpdateVisuals()
    {
        foreach (var wheel in wheels)
        {
            if (wheel.collider != null && wheel.visualMesh != null)
            {
                Vector3 pos;
                Quaternion rot;
                wheel.collider.GetWorldPose(out pos, out rot);
                wheel.visualMesh.position = pos;
                wheel.visualMesh.rotation = rot;
            }
        }
    }
}