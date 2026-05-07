using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class AutoSpawnRoad
{
    private static readonly string[] excludedScenes = { "complete_track_demo" };

    static AutoSpawnRoad()
    {
        EditorApplication.delayCall += SpawnRoadAutomatically;
    }

    [MenuItem("Tools/Spawn Road Track")]
    public static void ManualSpawn()
    {
        SpawnRoadAutomatically();
    }

    public static void SpawnRoadAutomatically()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        foreach (var s in excludedScenes) if (s == activeScene) return;

        if (GameObject.Find("Connected Octagon Track") != null) return;
        
        // Remove old tracks if they exist
        GameObject oldTrack1 = GameObject.Find("Procedural Octagon Track");
        if (oldTrack1 != null) Object.DestroyImmediate(oldTrack1);
        
        GameObject oldTrack2 = GameObject.Find("Procedural Circuit Track");
        if (oldTrack2 != null) Object.DestroyImmediate(oldTrack2);
        
        GameObject oldTrack3 = GameObject.Find("Racing Track");
        if (oldTrack3 != null) Object.DestroyImmediate(oldTrack3);

        // Deactivate the old flat test ground if it exists
        GameObject ground = GameObject.Find("Ground");
        if (ground != null) ground.SetActive(false);

        // 1. Create a parent object
        GameObject trackRoot = new GameObject("Connected Octagon Track");
        trackRoot.transform.position = Vector3.zero;

        // 2. Generate a checkered texture
        Texture2D checkerTex = new Texture2D(256, 256);
        checkerTex.filterMode = FilterMode.Point;
        Color col1 = new Color(0.15f, 0.15f, 0.15f); // Dark grey asphalt
        Color col2 = new Color(0.25f, 0.25f, 0.25f); // Light grey asphalt
        
        for (int y = 0; y < 256; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                bool isWhite = ((x / 32) % 2 == 0) ^ ((y / 32) % 2 == 0);
                checkerTex.SetPixel(x, y, isWhite ? col1 : col2);
            }
        }
        checkerTex.Apply();

        string texPath = "Assets/CircuitCheckerTex.png";
        System.IO.File.WriteAllBytes(texPath, checkerTex.EncodeToPNG());
        AssetDatabase.ImportAsset(texPath);

        // 3. Create a material
        string matPath = "Assets/CircuitMaterial.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null) urpShader = Shader.Find("Standard");
            mat = new Material(urpShader);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        
        Texture2D loadedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        mat.mainTexture = loadedTex;

        // 4. Build an octagon circuit
        float trackWidth = 30f;
        float trackLength = 100f; // The length of the inner "square" edge it's based on
        float trackThickness = 1f;
        
        // Distance from center to the center of the track segment
        float R = (trackLength / 2f) / Mathf.Tan(22.5f * Mathf.Deg2Rad);

        float outerR = R + trackWidth / 2f + 0.5f;
        float innerR = R - trackWidth / 2f - 0.5f;
        
        // Mathematically calculate exact length of the walls to form a perfect octagon outline
        // Add a small overlap (2f) to make sure corners intersect fully
        float outerLength = 2f * outerR * Mathf.Tan(22.5f * Mathf.Deg2Rad) + 2f;
        float innerLength = 2f * innerR * Mathf.Tan(22.5f * Mathf.Deg2Rad) + 2f;

        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad));
            
            Quaternion rot = Quaternion.Euler(0, angle + 90f, 0);

            // Road segment 
            Vector3 pos = dir * R + new Vector3(0, -trackThickness / 2f, 0);
            CreateTrackSegment(trackRoot, mat, pos, rot, new Vector3(trackWidth, trackThickness, trackLength + 25f));

            // Outer wall
            Vector3 outerPos = dir * outerR + new Vector3(0, 2f, 0);
            CreateWall(trackRoot, outerPos, rot, new Vector3(1f, 4f, outerLength));

            // Inner wall
            Vector3 innerPos = dir * innerR + new Vector3(0, 2f, 0);
            CreateWall(trackRoot, innerPos, rot, new Vector3(1f, 4f, innerLength));
        }

        Debug.Log("Connected Octagon Track auto-spawned successfully with exact wall bounds!");
        
        GameObject car = GameObject.FindObjectOfType<SimplePlayerCar>()?.gameObject;
        if (car != null)
        {
            // Place on the first straight
            car.transform.position = new Vector3(0, 2f, R);
            car.transform.rotation = Quaternion.Euler(0, 90, 0);
            Selection.activeGameObject = car;
        }
    }

    private static void CreateTrackSegment(GameObject parent, Material mat, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        segment.name = "TrackSegment";
        segment.transform.SetParent(parent.transform);
        segment.transform.position = pos;
        segment.transform.rotation = rot;
        segment.transform.localScale = scale;
        
        Renderer r = segment.GetComponent<Renderer>();
        
        Material instMat = new Material(mat);
        instMat.mainTextureScale = new Vector2(scale.x / 10f, scale.z / 10f);
        r.sharedMaterial = instMat;
    }

    private static void CreateWall(GameObject parent, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Wall";
        wall.transform.SetParent(parent.transform);
        wall.transform.position = pos;
        wall.transform.rotation = rot;
        wall.transform.localScale = scale;
        
        Renderer r = wall.GetComponent<Renderer>();
        r.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        r.sharedMaterial.color = new Color(0.8f, 0.1f, 0.1f); // Red walls
    }
}