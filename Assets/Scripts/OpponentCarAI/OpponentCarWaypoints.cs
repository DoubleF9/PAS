using UnityEngine;

public class OpponentCarWaypoints : MonoBehaviour
{
    [Header("Opponent Car")]
    public OpponentCar opponentCar;
    public Waypoint currentWaypoint;
    void Start()
    {
        opponentCar.LocateDestination(currentWaypoint.GetPosition(), GetForwardFrom(currentWaypoint));
    }

    // Update is called once per frame
    void Update()
    {
        if(opponentCar.destinationReached)
        {
            currentWaypoint = currentWaypoint.nextWaypoint;
            opponentCar.LocateDestination(currentWaypoint.GetPosition(), GetForwardFrom(currentWaypoint));
        }
    }

    private Vector3 GetForwardFrom(Waypoint wp)
    {
        if (wp != null && wp.nextWaypoint != null)
        {
            return (wp.nextWaypoint.transform.position - wp.transform.position).normalized;
        }
        return transform.forward;
    }
}
