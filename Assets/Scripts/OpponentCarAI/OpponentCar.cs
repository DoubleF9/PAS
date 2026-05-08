using UnityEngine;
using System.Collections.Generic;

public class OpponentCar : MonoBehaviour
{
    [Header("Car Engine")]
    public float maxSpeed;
    public float currentSpeed;
    public float acceleration = 1f;
    public float turningSpeed = 30f;
    public float breakSpeed = 12f;

    [Header("Destination Var")]
    public Vector3 destination;
    public Vector3 destinationForward = Vector3.forward;
    public bool destinationReached;

    [Header("Wheels")]
    public List<WheelInfo> wheels = new List<WheelInfo>();
    public float maxSteerAngle = 30f;

    private Rigidbody rb;

    [Header("Respawn")]
    public float respawnTimer = 0f;
    public float respawnTimeThreshold = 10f;

    [Header("Lap")]
    public int maxLaps;
    public int currentLap;
    
    private float currentSteerAngle;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.centerOfMass = new Vector3(0, -0.3f, 0);

        LapSystem lapSystem = FindObjectOfType<LapSystem>();
        if (lapSystem != null)
        {
            maxLaps = lapSystem.maxLaps;
        }

        wheels.Clear();
        WheelCollider[] colliders = GetComponentsInChildren<WheelCollider>();
        Transform[] transforms = GetComponentsInChildren<Transform>();

        foreach (var wc in colliders)
        {
            string wcName = wc.gameObject.name.ToLower();
            Transform visual = null;
            bool isSteering = wcName.Contains("fl") || wcName.Contains("fr");

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
                info.isMotor = false;
                wheels.Add(info);
            }
        }
    }

    void Update()
    {
        if (!destinationReached)
        {
            // Only increase the respawn timer if the car is actually stuck (moving very slowly)
            if (rb.linearVelocity.magnitude < 2f)
            {
                respawnTimer += Time.deltaTime;
                if (respawnTimer >= respawnTimeThreshold)
                {
                    RespawnAtDestination();
                }
            }
            else
            {
                respawnTimer = 0f; // Reset if moving
            }
        }
        else
        {
            respawnTimer = 0f;
        }

        UpdateVisuals();
    }

    void FixedUpdate()
    {
        Drive();
    }

    public void Drive()
    {
        if (!destinationReached)
        {
            Vector3 destinationDirection = destination - transform.position;
            destinationDirection.y = 0;
            float destinationDistance = destinationDirection.magnitude;

            if (destinationDistance >= breakSpeed)
            {
                // Calculate target rotation
                Quaternion targetRotation = Quaternion.LookRotation(destinationDirection);
                
                // Calculate steer angle for wheels before rotating
                Vector3 localTarget = transform.InverseTransformPoint(destination);
                float steerAngleTarget = Mathf.Clamp((localTarget.x / localTarget.magnitude) * maxSteerAngle * 2f, -maxSteerAngle, maxSteerAngle);
                currentSteerAngle = Mathf.Lerp(currentSteerAngle, steerAngleTarget, Time.fixedDeltaTime * 10f);

                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turningSpeed * Time.fixedDeltaTime);

                currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, acceleration * Time.fixedDeltaTime);

                // Add downforce to keep them planted
                rb.AddForce(-transform.up * 50f * currentSpeed);

                Vector3 targetVel = transform.forward * currentSpeed;
                
                // Preserve falling, but clamp extreme upwards velocity from collisions
                float safeY = Mathf.Min(rb.linearVelocity.y, 2f);
                targetVel.y = safeY;
                
                // Lerp the velocity instead of hard-setting it. 
                // Hard-setting X/Z makes them infinitely heavy horizontally, forcing Unity to resolve collisions by shooting them upwards.
                rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVel, Time.fixedDeltaTime * 10f);
            }
            else
            {
                destinationReached = true;
                
                Vector3 stopVel = Vector3.zero;
                stopVel.y = rb.linearVelocity.y;
                rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, stopVel, Time.fixedDeltaTime * 10f);
                
                currentSteerAngle = Mathf.Lerp(currentSteerAngle, 0, Time.fixedDeltaTime * 10f);
            }

            foreach (var wheel in wheels)
            {
                if (wheel.collider == null) continue;
                if (wheel.isSteering)
                {
                    wheel.collider.steerAngle = currentSteerAngle;
                }
                
                // Allow wheel collider to rotate by applying a tiny bit of torque if moving
                if (currentSpeed > 0.1f)
                {
                    wheel.collider.motorTorque = 0.0001f;
                    wheel.collider.brakeTorque = 0f;
                }
                else
                {
                    wheel.collider.brakeTorque = 1000f;
                    wheel.collider.motorTorque = 0f;
                }
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

    private void RespawnAtDestination()
    {
        respawnTimer = 0f;
        currentSpeed = 0f;

        Vector3 spawnPos = destination;
        Vector3 rayOrigin = destination + Vector3.up * 50f;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 200f, ~0, QueryTriggerInteraction.Ignore))
        {
            spawnPos = hit.point + Vector3.up * 0.5f;
        }

        transform.position = spawnPos;
        Vector3 flatForward = destinationForward;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(flatForward.normalized);
        }
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        destinationReached = false;
    }

    public void LocateDestination(Vector3 newDestination)
    {
        destination = newDestination;
        destinationReached = false;
    }

    public void LocateDestination(Vector3 newDestination, Vector3 forwardHint)
    {
        destination = newDestination;
        destinationForward = forwardHint;
        destinationReached = false;
    }

    public void ResetAcceleration()
    {
        currentSpeed = Random.Range(38f, 46f);
        acceleration = Random.Range(3.5f, 5f);
    }

    public void IncreaseLap()
    {
        currentLap++;
        Debug.Log("Car " + gameObject.name + " Lap: " + currentLap);
    }
}
