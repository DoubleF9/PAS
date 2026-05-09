using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MainMenuSetup : Editor
{
    [MenuItem("Tools/Setup Main Menu Scene")]
    public static void CreateMainMenuScene()
    {
        // 1. Create a new scene
        UnityEngine.SceneManagement.Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 2. Setup Camera
        GameObject cameraObj = new GameObject("Main Camera");
        Camera cam = cameraObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cameraObj.AddComponent<AudioListener>();
        cameraObj.tag = "MainCamera";

        // 3. Setup Canvas
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<GraphicRaycaster>();

        // 4. Setup Event System using Unity's built-in command to avoid Input System configuration errors
        EditorApplication.ExecuteMenuItem("GameObject/UI/Event System");
        EventSystem eventSystem = Object.FindObjectOfType<EventSystem>();
        if (eventSystem != null)
        {
            eventSystem.gameObject.name = "EventSystem";
        }

        // 5. Add MainMenu script to Canvas
        MainMenu mainMenu = canvasObj.AddComponent<MainMenu>();

        // 6. Setup Background Image
        GameObject bgObj = new GameObject("BackgroundImage");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        
        // --- Automatically setup the car image ---
        string imagePath = "Assets/the-best-looking-racing-cars-of-2023-01.jpg";
        TextureImporter importer = AssetImporter.GetAtPath(imagePath) as TextureImporter;
        if (importer != null)
        {
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.SaveAndReimport();
                AssetDatabase.Refresh();
            }
        }
        
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);
        if (bgSprite == null)
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);
            if (tex != null)
            {
                bgSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
        }

        if (bgSprite != null)
        {
            bgImage.sprite = bgSprite;
            bgImage.color = Color.white;
        }
        else
        {
            bgImage.color = new Color(0.2f, 0.2f, 0.2f); // Fallback color
            Debug.LogWarning("Could not load the car image as a Sprite at " + imagePath);
        }

        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // 7. Setup Start Button
        GameObject startButtonObj = CreateButton("StartButton", "START", canvasObj.transform, new Vector2(0, 50));
        Button startBtn = startButtonObj.GetComponent<Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(startBtn.onClick, mainMenu.StartGame);

        // 8. Setup Exit Button
        GameObject exitButtonObj = CreateButton("ExitButton", "EXIT", canvasObj.transform, new Vector2(0, -50));
        Button exitBtn = exitButtonObj.GetComponent<Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(exitBtn.onClick, mainMenu.QuitGame);

        // 9. Save Scene
        string scenePath = "Assets/Scenes/MainMenu.unity";
        EditorSceneManager.SaveScene(newScene, scenePath);
        Debug.Log("Main Menu scene generated at: " + scenePath);

        // 10. Add to Build Settings
        AddScenesToBuildSettings(scenePath, "Assets/Models/Tracks/CartoonTracksPack1/Track1/Demo Scenes/complete_track_demo.unity");
    }

    private static GameObject CreateButton(string name, string text, Transform parent, Vector2 anchoredPosition)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);
        Image btnImage = buttonObj.AddComponent<Image>();
        btnImage.color = Color.white;
        Button button = buttonObj.AddComponent<Button>();

        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 60);
        rect.anchoredPosition = anchoredPosition;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.color = Color.black;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.fontSize = 24;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        return buttonObj;
    }

    private static void AddScenesToBuildSettings(string menuScenePath, string demoScenePath)
    {
        EditorBuildSettingsScene[] originalScenes = EditorBuildSettings.scenes;
        bool hasMenu = false;
        bool hasDemo = false;

        foreach (var scene in originalScenes)
        {
            if (scene.path == menuScenePath) hasMenu = true;
            if (scene.path == demoScenePath) hasDemo = true;
        }

        int newCount = originalScenes.Length;
        if (!hasMenu) newCount++;
        if (!hasDemo) newCount++;

        EditorBuildSettingsScene[] newScenes = new EditorBuildSettingsScene[newCount];
        int index = 0;

        if (!hasMenu)
        {
            newScenes[index++] = new EditorBuildSettingsScene(menuScenePath, true);
        }

        foreach (var scene in originalScenes)
        {
            // Optional: skip re-adding menu if we're forcing it to be first
            if (!hasMenu && scene.path == menuScenePath) continue; 
            newScenes[index++] = scene;
        }

        if (!hasDemo)
        {
            newScenes[index++] = new EditorBuildSettingsScene(demoScenePath, true);
        }

        EditorBuildSettings.scenes = newScenes;
        Debug.Log("Build settings updated with MainMenu and Demo scenes.");
    }
}
