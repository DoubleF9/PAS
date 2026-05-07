using UnityEngine;
using TMPro;
using System.Collections;

public class RaceCountdown : MonoBehaviour
{
    [Header("References")]
    public GameObject panel;
    public TextMeshProUGUI countdownText;
    public CameraFollow cameraFollow;

    [Header("Timing")]
    public int startNumber = 3;
    public float numberDuration = 1f;
    public float goDuration = 0.7f;

    void Start()
    {
        StartCoroutine(RunCountdown());
    }

    private IEnumerator RunCountdown()
    {
        if (panel != null) panel.SetActive(true);
        if (cameraFollow != null) cameraFollow.enabled = false;
        Time.timeScale = 0f;

        for (int i = startNumber; i > 0; i--)
        {
            if (countdownText != null) countdownText.text = i.ToString();
            yield return new WaitForSecondsRealtime(numberDuration);
        }

        if (countdownText != null) countdownText.text = "GO!";
        if (cameraFollow != null) cameraFollow.enabled = true;
        Time.timeScale = 1f;
        yield return new WaitForSecondsRealtime(goDuration);

        if (panel != null) panel.SetActive(false);
    }
}
