using UnityEngine;

public class OpponentCar : MonoBehaviour
{

    [Header("Car Engine")]
    public float maxSpeed;
    public float currentSpeed;
    public float acceleration=1f;
    public float turningSpeed=30f;
    public float breakSpeed=12f;

    [Header("Destination Var")]
    public Vector3 destination;
    public Vector3 destinationForward = Vector3.forward;
    public bool destinationReached;


    private Rigidbody rb;

    [Header("Respawn")]
    public float respawnTimer=0f;
    public float respawnTimeThreshold=10f;

    [Header("Lap")]
    public int maxLaps;
    public int currentLap;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb=GetComponent<Rigidbody>();
        rb.useGravity=true;
        maxLaps=FindObjectOfType<LapSystem>().maxLaps;
        //currentLap=FindObjectOfType<LapSystem>().currentLap;
    }

    void Update()
    {
        Drive();

        if(!destinationReached)
        {
            respawnTimer += Time.deltaTime;
            if(respawnTimer >= respawnTimeThreshold)
            {
                RespawnAtDestination();
            }
        }
        else{
            respawnTimer = 0f;
        }

    }

    public void Drive()
    {
        if(!destinationReached)
        {
            Vector3 destinationDirection = destination - transform.position;
            destinationDirection.y = 0;
            float destinationDistance = destinationDirection.magnitude;

            if(destinationDistance >= breakSpeed)
            {
                Quaternion targetRotation = Quaternion.LookRotation(destinationDirection);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turningSpeed * Time.deltaTime);

                currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, acceleration * Time.deltaTime);

                rb.linearVelocity = transform.forward * currentSpeed;
            }
            else
            {
                destinationReached = true;
                rb.linearVelocity = Vector3.zero;
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
        Debug.Log("Car "+ gameObject.name + " Lap: " + currentLap);
    }
}
