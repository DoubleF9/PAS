using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    [Header("References")]
    public GameObject panel;
    public Button resumeButton;
    public Button restartButton;
    public Button mainMenuButton;
    public CameraFollow cameraFollow;

    [Header("Scene")]
    public string mainMenuSceneName = "MainMenu";

    private bool isPaused;

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (restartButton != null) restartButton.onClick.AddListener(Restart);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GoToMainMenu);
    }

    void Update()
    {
        bool escPressed = false;
        if (Keyboard.current != null)
        {
            escPressed = Keyboard.current.escapeKey.wasPressedThisFrame;
        }
        else
        {
            escPressed = Input.GetKeyDown(KeyCode.Escape);
        }

        if (!escPressed) return;

        if (isPaused)
        {
            Resume();
        }
        else if (Time.timeScale > 0f)
        {
            Pause();
        }
    }

    public void Pause()
    {
        isPaused = true;
        if (panel != null) panel.SetActive(true);
        if (cameraFollow != null) cameraFollow.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        isPaused = false;
        if (panel != null) panel.SetActive(false);
        if (cameraFollow != null) cameraFollow.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.timeScale = 1f;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
