using UnityEngine;

public class LapSystem : MonoBehaviour
{
    public int maxLaps = 3;
    public int currentLap;
    public MissionEndUI missionEndUI;

    private void OnTriggerEnter(Collider other)
    {
        OpponentCar opponentCar = other.GetComponent<OpponentCar>();
        SimplePlayerCar playerCar = other.GetComponent<SimplePlayerCar>();

        if(opponentCar != null)
        {
            opponentCar.IncreaseLap();
            CheckRaceCompletion(opponentCar);
        }

        if(playerCar != null)
        {
            playerCar.IncreaseLap();
            CheckRaceCompletion(playerCar);
        }
    }

    private void CheckRaceCompletion(OpponentCar opponentCar)
    {
        if(opponentCar.currentLap > maxLaps)
        {
            EndMission(false);
        }
    }

    private void CheckRaceCompletion(SimplePlayerCar playerCar)
    {
        if(playerCar.currentLap > maxLaps)
        {
            EndMission(true);
        }
    }

    private void EndMission(bool playerWon)
    {
        if(playerWon)
        {
            Debug.Log("Player Wins!");
        }
        else
        {
            Debug.Log("Player Loses!");
        }

        if(missionEndUI != null)
        {
            missionEndUI.Show(playerWon);
        }
    }
}
