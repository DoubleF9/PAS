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
    
    [Header("Engine & Performance")]
    public bool isAWD = true;
    public float topSpeedKmh = 320f;
    public float maxEngineTorque = 8000f;
    public float tractionControlSlipLimit = 15f;

    [Header("Brake & Coast Settings")]
    public float brakeTorque = 15000f;
    [Tooltip("Simulates natural coasting when no gas is applied. Replaces abrupt idle braking with smooth aerodynamic drag.")]
    public float coastingDrag = 0.25f; 
    
    [Header("Steering Settings")]
    public float maxSteerAngle = 30f;
    public float minSteerAngle = 8f;
    public float steerDropoffSpeed = 120f;
    public float steerSpeed = 15f;

    [Header("Handling Assists (On Rails)")]
    [Tooltip("Pushes the car down into the track at speed to massively increase tire grip.")]
    public float downforce = 50f;
    [Tooltip("Actively cancels out lateral sliding during normal driving so the car never drifts by accident.")]
    public float lateralGripAssist = 5f;

    [Header("Tire Grip Settings")]
    [Tooltip("Normal grip when driving. High value = completely planted, no random drifting.")]
    public float normalFriction = 3.5f;

    [Header("Wall Collision Settings")]
    [Tooltip("Maximum impact angle (in degrees) to allow sliding along the wall. Above this, the car stops completely.")]
    public float wallSlideAngleThreshold = 30f;
    [Tooltip("Base speed multiplier when sliding perfectly parallel to a wall (1.0 = no speed lost, 0.0 = full stop).")]
    public float wallSlideBaseSpeedPenalty = 0.8f;

    private Rigidbody rb;
    private float moveInput;
    private float turnInput;
    private bool handbrakeInput;
    private float currentSteerAngle;
    
    // For smooth wall alignment
    private float wallAlignTimer = 0f;
    private Quaternion targetWallRotation;

    [Header("Lap")]
    public int maxLaps;
    public int currentLap;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.centerOfMass = new Vector3(0, 0.3f, 0);

        wheels.Clear();
        
        WheelCollider[] colliders = GetComponentsInChildren<WheelCollider>();
        Transform[] transforms = GetComponentsInChildren<Transform>();

        foreach (var wc in colliders)
        {
            WheelFrictionCurve fFriction = wc.forwardFriction;
            fFriction.stiffness = normalFriction;
            wc.forwardFriction = fFriction;

            WheelFrictionCurve sFriction = wc.sidewaysFriction;
            sFriction.stiffness = normalFriction;
            wc.sidewaysFriction = sFriction;
            
            string wcName = wc.gameObject.name.ToLower();
            Transform visual = null;
            bool isSteering = wcName.Contains("fl") || wcName.Contains("fr");
            bool isMotor = isAWD || wcName.Contains("rl") || wcName.Contains("rr");

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

        maxLaps=FindObjectOfType<LapSystem>().maxLaps;
    }

    void Update()
    {
        moveInput = 0;
        turnInput = 0;
        handbrakeInput = false;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveInput += 1;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveInput -= 1;

            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) turnInput -= 1;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) turnInput += 1;
            
            if (Keyboard.current.spaceKey.isPressed) handbrakeInput = true;
        }
        else
        {
            moveInput = Input.GetAxisRaw("Vertical");
            turnInput = Input.GetAxisRaw("Horizontal");
            handbrakeInput = Input.GetKey(KeyCode.Space);
        }

        UpdateVisuals();
    }

    void FixedUpdate()
    {
        float forwardSpeedMps = Vector3.Dot(transform.forward, rb.linearVelocity);
        float speedKmh = Mathf.Abs(forwardSpeedMps) * 3.6f;
        
        rb.AddForce(-transform.up * downforce * rb.linearVelocity.magnitude);

        float speedFactor = Mathf.InverseLerp(0, steerDropoffSpeed, speedKmh);
        float currentMaxSteer = Mathf.Lerp(maxSteerAngle, minSteerAngle, speedFactor);

        float targetSteer = turnInput * currentMaxSteer;
        currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetSteer, steerSpeed * Time.fixedDeltaTime);
        
        float torqueCurve = 1f - (speedKmh / topSpeedKmh);
        
        if (speedKmh < 60f) 
        {
            torqueCurve = Mathf.Lerp(2.5f, torqueCurve, speedKmh / 60f);
        }
        else if (speedKmh > topSpeedKmh) 
        {
            torqueCurve = 0f; 
        }

        if (moveInput < 0 && forwardSpeedMps < 0.1f)
        {
            if (speedKmh > (topSpeedKmh / 4f)) torqueCurve = 0f;
        }

        float currentTorque = moveInput * maxEngineTorque * torqueCurve;
        
        int motorWheelsCount = isAWD ? 4 : 2;
        float torquePerWheel = currentTorque / motorWheelsCount;

        bool isMovingForward = forwardSpeedMps > 0.1f;
        bool isMovingBackward = forwardSpeedMps < -0.1f;
        bool isFootBrake = false;
        bool isCoasting = false;

        if (moveInput < 0 && isMovingForward)
        {
            isFootBrake = true;
            torquePerWheel = 0;
        }
        else if (moveInput > 0 && isMovingBackward)
        {
            isFootBrake = true;
            torquePerWheel = 0;
        }
        else if (moveInput == 0)
        {
            isCoasting = true;
            torquePerWheel = 0;
        }

        if (isCoasting && !handbrakeInput && !isFootBrake)
        {
            rb.linearDamping = coastingDrag;
        }
        else
        {
            rb.linearDamping = 0.05f; 
        }

        if (!handbrakeInput && !isFootBrake)
        {
            Vector3 rightDir = transform.right;
            float sidewaysSpeed = Vector3.Dot(rb.linearVelocity, rightDir);
            rb.AddForce(rightDir * -sidewaysSpeed * rb.mass * lateralGripAssist);
        }

        // Apply smooth wall alignment rotation
        if (wallAlignTimer > 0)
        {
            wallAlignTimer -= Time.fixedDeltaTime;
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetWallRotation, Time.fixedDeltaTime * 8f));
        }

        foreach (var wheel in wheels)
        {
            if (wheel.collider == null) continue;

            if (wheel.isSteering)
            {
                wheel.collider.steerAngle = currentSteerAngle;
            }

            float wheelRpmKmh = Mathf.Abs(wheel.collider.rpm * wheel.collider.radius * 2f * Mathf.PI * 60f / 1000f);

            if (wheel.isMotor && !isFootBrake && !isCoasting && !handbrakeInput)
            {
                if (wheelRpmKmh > speedKmh + tractionControlSlipLimit)
                {
                    wheel.collider.motorTorque = 0;
                }
                else
                {
                    wheel.collider.motorTorque = torquePerWheel;
                }
                wheel.collider.brakeTorque = 0;
            }
            else
            {
                wheel.collider.motorTorque = 0;
            }

            WheelFrictionCurve sidewaysFriction = wheel.collider.sidewaysFriction;
            WheelFrictionCurve forwardFriction = wheel.collider.forwardFriction;

            sidewaysFriction.stiffness = normalFriction;
            forwardFriction.stiffness = normalFriction;

            if (handbrakeInput)
            {
                if (!wheel.isSteering)
                {
                    wheel.collider.brakeTorque = brakeTorque;
                }
                else
                {
                    wheel.collider.brakeTorque = 0;
                }
            }
            else if (isFootBrake)
            {
                wheel.collider.brakeTorque = brakeTorque * Mathf.Abs(moveInput);
            }
            else
            {
                wheel.collider.brakeTorque = 0;
            }
            
            wheel.collider.sidewaysFriction = sidewaysFriction;
            wheel.collider.forwardFriction = forwardFriction;
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

    void OnCollisionStay(Collision collision)
    {
        if (rb != null && collision.gameObject.name.Contains("Wall"))
        {
            if (rb.linearVelocity.magnitude < 1.0f)
            {
                foreach (var wheel in wheels)
                {
                    if (wheel.collider != null)
                    {
                        wheel.collider.motorTorque = 0;
                        wheel.collider.brakeTorque = brakeTorque;
                    }
                }
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (rb != null && collision.gameObject.name.Contains("Wall"))
        {
            if (collision.contactCount > 0)
            {
                // We only care about the horizontal impact angle!
                // If a wall's geometry is slightly sloped or the car hits an edge, 
                // a 3D normal could trick the system into thinking a head-on crash is a parallel one.
                Vector3 flatNormal = collision.contacts[0].normal;
                flatNormal.y = 0;
                
                // If the normal is completely vertical (hit a floor/ceiling), ignore it.
                if (flatNormal.sqrMagnitude < 0.01f) return;
                
                flatNormal.Normalize();

                // rb.linearVelocity is modified by Unity's physics resolution before OnCollisionEnter fires!
                // We MUST use -collision.relativeVelocity to get the true, pre-impact incoming velocity vector.
                Vector3 incomingVelocity = -collision.relativeVelocity;
                incomingVelocity.y = 0;

                if (incomingVelocity.sqrMagnitude > 0.1f)
                {
                    // Calculate impact angle. 0 = grazing/parallel, 90 = head-on
                    float angleToInvertedNormal = Vector3.Angle(incomingVelocity, -flatNormal);
                    float impactAngle = Mathf.Abs(90f - angleToInvertedNormal);

                    if (impactAngle <= wallSlideAngleThreshold)
                    {
                        // Project incoming velocity onto the wall plane so it slides perfectly along it
                        Vector3 projectedVelocity = Vector3.ProjectOnPlane(incomingVelocity, flatNormal);
                        
                        // Scale speed penalty: 0 degrees = base penalty, threshold degrees = 0 multiplier (full stop)
                        float speedMultiplier = Mathf.Lerp(wallSlideBaseSpeedPenalty, 0f, impactAngle / wallSlideAngleThreshold);
                        
                        // Apply the new slide velocity
                        rb.linearVelocity = projectedVelocity.normalized * (incomingVelocity.magnitude * speedMultiplier) + new Vector3(0, rb.linearVelocity.y, 0);
                        rb.angularVelocity = Vector3.zero;

                        // Align car's forward direction smoothly instead of teleporting
                        Vector3 wallDir1 = Vector3.Cross(flatNormal, Vector3.up).normalized;
                        Vector3 wallDir2 = -wallDir1;
                        
                        Vector3 carForward = transform.forward;
                        carForward.y = 0;
                        if (carForward.sqrMagnitude > 0.01f)
                        {
                            carForward.Normalize();
                            // Pick the wall direction that the car is already facing towards
                            Vector3 targetForward = Vector3.Dot(carForward, wallDir1) > 0 ? wallDir1 : wallDir2;
                            
                            targetWallRotation = Quaternion.LookRotation(targetForward, Vector3.up);
                            wallAlignTimer = 0.4f; // Smoothly rotate over the next 0.4 seconds
                        }
                    }
                    else
                    {
                        // Impact angle exceeds threshold; full stop
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                        wallAlignTimer = 0f; // Cancel any ongoing smooth rotation
                    }
                }
            }
            else
            {
                // Fallback to full stop if no contact data
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                wallAlignTimer = 0f;
            }
        }
    }

    public void IncreaseLap()
    {
        currentLap++;
        Debug.Log("Car "+ gameObject.name + " Lap: " + currentLap);
    }
}