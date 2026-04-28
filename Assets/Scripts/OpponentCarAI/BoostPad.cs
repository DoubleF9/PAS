using UnityEngine;

public class BoostPad : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        OpponentCar opponentCar = other.GetComponent<OpponentCar>();
        if(opponentCar != null)
        {
            opponentCar.acceleration = Random.Range(4f, 5f);
            opponentCar.currentSpeed = Random.Range(35f, 41f);
            
        }
    }
}
