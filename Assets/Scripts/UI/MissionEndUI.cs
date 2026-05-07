using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MissionEndUI : MonoBehaviour
{
    [Header("References")]
    public GameObject panel;
    public TextMeshProUGUI resultText;
    public Button mainMenuButton;
    public CameraFollow cameraFollow;

    [Header("Scene")]
    public string mainMenuSceneName = "MainMenu";

    void Awake()
    {
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GoToMainMenu);
    }

    public void Show(bool playerWon)
    {
        if (panel != null) panel.SetActive(true);
        if (resultText != null) resultText.text = playerWon ? "You Won!" : "You Lost!";
        if (cameraFollow != null) cameraFollow.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
