using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

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
        if (!force && EditorPrefs.GetBool("CarSetupDone_v5", false)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        Debug.Log("RealPhysics: Running Advanced WheelCollider Setup...");

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
                car.transform.position = new Vector3(0, 1.5f, 0); // Drop from slightly higher
                car.AddComponent<SimplePlayerCar>();
            }
        }

        if (car != null)
        {
            FixMaterials(car);
            SetupAdvancedPhysics(car);
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

        EditorPrefs.SetBool("CarSetupDone_v5", true);
        Debug.Log("RealPhysics: Advanced Setup Complete.");
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

    private static void SetupAdvancedPhysics(GameObject car)
    {
        // 1. Clean rigidbodies and scripts from children
        SimplePlayerCar[] childScripts = car.GetComponentsInChildren<SimplePlayerCar>(true);
        foreach (var s in childScripts) { if (s.gameObject != car) Object.DestroyImmediate(s); }

        Rigidbody[] childRbs = car.GetComponentsInChildren<Rigidbody>(true);
        foreach (var r in childRbs) { if (r.gameObject != car) Object.DestroyImmediate(r); }

        // 2. Setup Root Rigidbody
        Rigidbody rb = car.GetComponent<Rigidbody>();
        if (rb == null) rb = car.AddComponent<Rigidbody>();
        rb.mass = 1500f; // Realistic mass for WheelColliders
        rb.linearDamping = 0.05f;
        rb.angularDamping = 0.05f;

        // 3. Remove old built-in WheelColliders that cause falling through floor
        WheelCollider[] oldWheels = car.GetComponentsInChildren<WheelCollider>(true);
        foreach (var w in oldWheels) { Object.DestroyImmediate(w); }

        // 4. Disable child colliders (visual meshes should not have colliders overlapping WheelColliders)
        Collider[] allColliders = car.GetComponentsInChildren<Collider>(true);
        foreach (var c in allColliders)
        {
            if (c.gameObject != car) c.enabled = false;
        }

        // 5. Add a BoxCollider for the CAR BODY ONLY (elevated so wheels hit ground first)
        BoxCollider rootBox = car.GetComponent<BoxCollider>();
        if (rootBox == null) rootBox = car.AddComponent<BoxCollider>();
        
        // Approximate body bounds, kept above ground
        rootBox.center = new Vector3(0, 0.6f, 0); 
        rootBox.size = new Vector3(1.8f, 0.8f, 4.5f);

        // 6. Find Visual Wheels
        Transform fl = null, fr = null, rl = null, rr = null;
        Transform[] allTransforms = car.GetComponentsInChildren<Transform>(true);
        foreach (var t in allTransforms)
        {
            string name = t.name.ToLower().Replace(" ", "");
            if (name.Contains("frontleftwheel")) fl = t;
            else if (name.Contains("frontrightwheel")) fr = t;
            else if (name.Contains("rearleftwheel")) rl = t;
            else if (name.Contains("rearrightwheel")) rr = t;
        }

        // 7. Create Dedicated WheelCollider GameObjects
        Transform wcRoot = car.transform.Find("WheelColliders");
        if (wcRoot != null) Object.DestroyImmediate(wcRoot.gameObject);
        
        wcRoot = new GameObject("WheelColliders").transform;
        wcRoot.SetParent(car.transform, false);
        wcRoot.localPosition = Vector3.zero;
        wcRoot.localRotation = Quaternion.identity;

        SimplePlayerCar script = car.GetComponent<SimplePlayerCar>();
        if (script != null)
        {
            script.wheels.Clear();
            if (fl != null) script.wheels.Add(CreateWheel(wcRoot, fl, "FL", true, false));
            if (fr != null) script.wheels.Add(CreateWheel(wcRoot, fr, "FR", true, false));
            if (rl != null) script.wheels.Add(CreateWheel(wcRoot, rl, "RL", false, true));
            if (rr != null) script.wheels.Add(CreateWheel(wcRoot, rr, "RR", false, true));
        }
    }

    private static WheelInfo CreateWheel(Transform root, Transform visual, string name, bool steer, bool motor)
    {
        GameObject wcObj = new GameObject("WC_" + name);
        wcObj.transform.SetParent(root, false);
        
        // Match visual wheel position exactly
        wcObj.transform.position = visual.position;
        // Keep rotation neutral relative to car
        wcObj.transform.localRotation = Quaternion.identity;

        WheelCollider wc = wcObj.AddComponent<WheelCollider>();
        wc.radius = 0.33f; 
        wc.suspensionDistance = 0.2f;
        wc.mass = 20f;
        
        JointSpring suspension = wc.suspensionSpring;
        suspension.spring = 35000f;
        suspension.damper = 4500f;
        suspension.targetPosition = 0.5f;
        wc.suspensionSpring = suspension;

        WheelInfo info = new WheelInfo();
        info.collider = wc;
        info.visualMesh = visual;
        info.isSteering = steer;
        info.isMotor = motor;
        
        return info;
    }
}
