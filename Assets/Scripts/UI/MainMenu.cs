using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // The exact name of the scene to load
    private string gameSceneName = "complete_track_demo";

    private void Awake()
    {
        // Automatically create an EventSystem at runtime if one is missing
        // This prevents editor crashes from the New Input System and guarantees UI clicks work
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            
#if ENABLE_INPUT_SYSTEM
            eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystemObj.AddComponent<StandaloneInputModule>();
#endif
            
            // Keep it around so it doesn't get destroyed if it was missing
            DontDestroyOnLoad(eventSystemObj);
        }
    }

    public void StartGame()
    {
        Debug.Log("Starting Game... Loading Scene: " + gameSceneName);
        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}