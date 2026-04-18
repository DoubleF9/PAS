using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class AutoSetupScene
{
    static AutoSetupScene()
    {
        EditorApplication.delayCall += () => RunSetup(false);
    }

    [MenuItem("Tools/Setup Player Car Scene")]
    public static void ManualSetup()
    {
        RunSetup(true);
    }

    public static void RunSetup(bool force)
    {
        if (!force && EditorPrefs.GetBool("CarSetupDone_v4", false)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        Debug.Log("Antigravity: Running Simple Setup...");

        // 1. Ground Setup
        GameObject ground = GameObject.Find("Ground");
        if (ground == null && GameObject.FindObjectOfType<Terrain>() == null)
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(50, 1, 50);
            ground.transform.position = Vector3.zero;
        }
        if (ground != null) 
        {
            FixMaterials(ground);
            SetupGroundGrid(ground);
        }

        // 2. Car Setup
        SimplePlayerCar carComp = GameObject.FindObjectOfType<SimplePlayerCar>();
        GameObject car = carComp != null ? carComp.gameObject : null;

        if (car == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Models/Cars/ARCADE - FREE Racing Car/Prefabs (With Colliders)" });
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                car = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                car.transform.position = new Vector3(0, 0.5f, 0); 
                car.AddComponent<SimplePlayerCar>();
            }
        }

        if (car != null)
        {
            FixMaterials(car);
            SetupCarScript(car);
            SetupPhysics(car);
        }

        // 3. Camera Setup
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            mainCam = camObj.AddComponent<Camera>();
        }

        CameraFollow follow = mainCam.GetComponent<CameraFollow>();
        if (follow == null) follow = mainCam.gameObject.AddComponent<CameraFollow>();
        if (car != null) follow.target = car.transform;

        EditorPrefs.SetBool("CarSetupDone_v4", true);
        Debug.Log("Antigravity: Simple Setup Complete.");
    }

    private static void SetupCarScript(GameObject car)
    {
        SimplePlayerCar script = car.GetComponent<SimplePlayerCar>();
        if (script == null) return;

        Transform[] allTransforms = car.GetComponentsInChildren<Transform>(true);
        
        foreach (var t in allTransforms)
        {
            // The model's wheels are usually named "FrontLeftWheel" and "FrontRightWheel"
            string name = t.name.Replace(" ", "").ToLower();
            
            if (name.Contains("frontleftwheel") || (name.Contains("frontleft") && name.Contains("wheel")))
            {
                script.frontLeftWheel = t;
            }
            else if (name.Contains("frontrightwheel") || (name.Contains("frontright") && name.Contains("wheel")))
            {
                script.frontRightWheel = t;
            }
        }
    }

    private static void SetupGroundGrid(GameObject ground)
    {
        string texPath = "Assets/GridTexture.png";
        if (!System.IO.File.Exists(texPath))
        {
            Texture2D tex = new Texture2D(256, 256);
            Color lineColor = new Color(0.8f, 0.8f, 0.8f);
            Color bgColor = new Color(0.2f, 0.2f, 0.2f);
            for (int y = 0; y < 256; y++) {
                for (int x = 0; x < 256; x++) {
                    bool isLine = (x % 64 == 0) || (y % 64 == 0);
                    tex.SetPixel(x, y, isLine ? lineColor : bgColor);
                }
            }
            tex.Apply();
            System.IO.File.WriteAllBytes(texPath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(texPath);
        }
        
        string matPath = "Assets/GridMaterial.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null) urpShader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (urpShader == null) urpShader = Shader.Find("Standard");
            
            mat = new Material(urpShader);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        
        Texture2D loadedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (mat != null && loadedTex != null)
        {
            mat.mainTexture = loadedTex;
            mat.mainTextureScale = new Vector2(50, 50);
            Renderer r = ground.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;
        }
    }

    private static void FixMaterials(GameObject obj)
    {
        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpShader == null) urpShader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (urpShader == null) return;

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            Material[] mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                if (mats[i].shader.name.Contains("Error") || mats[i].shader.name.Contains("Standard") || mats[i].shader.name.Contains("Specular"))
                {
                    mats[i].shader = urpShader;
                    changed = true;
                }
            }
            if (changed) r.sharedMaterials = mats;
        }
    }

    private static void SetupPhysics(GameObject car)
    {
        // 1. Remove all old/extra scripts and Rigidbodies on children (this causes the body to fall off the wheels!)
        // Destroy SimplePlayerCar on children first, because it RequireComponent(typeof(Rigidbody))
        SimplePlayerCar[] childScripts = car.GetComponentsInChildren<SimplePlayerCar>(true);
        foreach (var s in childScripts)
        {
            if (s.gameObject != car) Object.DestroyImmediate(s);
        }

        Rigidbody[] childRbs = car.GetComponentsInChildren<Rigidbody>(true);
        foreach (var r in childRbs)
        {
            if (r.gameObject != car) Object.DestroyImmediate(r);
        }

        // 2. Add Rigidbody to the ROOT if missing
        Rigidbody rb = car.GetComponent<Rigidbody>();
        if (rb == null) rb = car.AddComponent<Rigidbody>();

        // 3. Remove old setup junk (WheelColliders)
        WheelCollider[] wheels = car.GetComponentsInChildren<WheelCollider>(true);
        foreach (var w in wheels)
        {
            Object.DestroyImmediate(w);
        }

        // 4. Disable ALL child colliders so they don't cause Rigidbody issues
        Collider[] allColliders = car.GetComponentsInChildren<Collider>(true);
        foreach (var c in allColliders)
        {
            if (c.gameObject != car) c.enabled = false;
        }

        // 5. Add ONE solid BoxCollider to the root that covers EVERYTHING (including wheels)
        BoxCollider rootBox = car.GetComponent<BoxCollider>();
        if (rootBox == null) rootBox = car.AddComponent<BoxCollider>();

        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        Renderer[] renderers = car.GetComponentsInChildren<Renderer>();
        bool first = true;
        foreach (var r in renderers)
        {
            if (first) { bounds = r.bounds; first = false; }
            else bounds.Encapsulate(r.bounds);
        }

        if (!first)
        {
            rootBox.center = car.transform.InverseTransformPoint(bounds.center);
            rootBox.size = car.transform.InverseTransformVector(bounds.size);
            
            // Shift the center slightly up so the bottom of the box aligns with the wheels perfectly, 
            // without sinking into the ground.
            rootBox.center = new Vector3(rootBox.center.x, rootBox.center.y + 0.02f, rootBox.center.z);
        }
    }
}
