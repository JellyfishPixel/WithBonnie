using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VehiclePrototypeSceneSetup
{
    const string MainScenePath = "Assets/Scene/Main.unity";
    const string RootName = "Vehicle Prototype Test Objects";

    [MenuItem("Tools/WithBonnie/Vehicles/Create Prototype Vehicles In Current Scene")]
    public static void CreatePrototypeVehiclesInCurrentScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        EnsurePrototypeVehicles(scene, markDirty: true);
        EditorSceneManager.SaveScene(scene);
    }

    public static void CreatePrototypeVehiclesInMainSceneBatch()
    {
        Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        EnsurePrototypeVehicles(scene, markDirty: true);
        EditorSceneManager.SaveScene(scene);
    }

    static void EnsurePrototypeVehicles(Scene scene, bool markDirty)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.position = Vector3.zero;
        }

        EnsureVehicle(root.transform, "Prototype Car", VehicleKind.Car, new Vector3(0f, 1.2f, 8f), 9f, 100f);
        EnsureVehicle(root.transform, "Prototype Boat", VehicleKind.Boat, new Vector3(4.5f, 1.1f, 8f), 6f, 70f);
        EnsureVehicle(root.transform, "Prototype Airplane", VehicleKind.Airplane, new Vector3(-5f, 2.2f, 8f), 12f, 85f);

        if (markDirty)
            EditorSceneManager.MarkSceneDirty(scene);
    }

    static void EnsureVehicle(Transform root, string name, VehicleKind kind, Vector3 position, float speed, float turnSpeed)
    {
        Transform existing = root.Find(name);
        if (existing != null)
            return;

        GameObject vehicle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vehicle.name = name;
        vehicle.transform.SetParent(root, true);
        vehicle.transform.position = position;
        vehicle.transform.localScale = Vector3.one;

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Visual";
        visual.transform.SetParent(vehicle.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = Vector3.one;

        Object.DestroyImmediate(vehicle.GetComponent<MeshFilter>());
        Object.DestroyImmediate(vehicle.GetComponent<MeshRenderer>());

        Rigidbody rb = vehicle.AddComponent<Rigidbody>();
        rb.mass = kind == VehicleKind.Airplane ? 2f : 4f;
        rb.useGravity = kind != VehicleKind.Airplane;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        VehicleController controller = vehicle.AddComponent<VehicleController>();
        controller.kind = kind;
        controller.vehicleName = name.Replace("Prototype ", "");
        controller.moveSpeed = speed;
        controller.reverseSpeed = Mathf.Max(2f, speed * 0.45f);
        controller.turnSpeed = turnSpeed;
        controller.airplaneVerticalSpeed = 5f;
        controller.seatLocalOffset = new Vector3(0f, 1.35f, 0f);
        controller.exitLocalOffset = new Vector3(1.8f, 0.2f, 0f);

        BoxCollider collider = vehicle.GetComponent<BoxCollider>();
        collider.size = Vector3.one;
        collider.center = Vector3.zero;

        AddLabel(vehicle.transform, name);

        if (kind == VehicleKind.Airplane)
            AddAirplaneShape(visual.transform);
        else if (kind == VehicleKind.Boat)
            AddBoatShape(visual.transform);
    }

    static void AddLabel(Transform parent, string label)
    {
        GameObject textObject = new GameObject("Label");
        textObject.transform.SetParent(parent, false);
        textObject.transform.localPosition = new Vector3(0f, 1.2f, 0f);

        TextMesh text = textObject.AddComponent<TextMesh>();
        text.text = label + "\nPress E";
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.characterSize = 0.25f;
        text.fontSize = 32;
    }

    static void AddAirplaneShape(Transform parent)
    {
        GameObject wing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wing.name = "Wing";
        wing.transform.SetParent(parent, false);
        wing.transform.localPosition = Vector3.zero;
        wing.transform.localScale = new Vector3(1.6f, 0.18f, 0.18f);

        GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tail.name = "Tail";
        tail.transform.SetParent(parent, false);
        tail.transform.localPosition = new Vector3(0f, 0.55f, -0.45f);
        tail.transform.localScale = new Vector3(0.2f, 1.2f, 0.2f);
    }

    static void AddBoatShape(Transform parent)
    {
        GameObject bow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bow.name = "Bow";
        bow.transform.SetParent(parent, false);
        bow.transform.localPosition = new Vector3(0f, 0.2f, 0.6f);
        bow.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
        bow.transform.localScale = new Vector3(0.55f, 0.75f, 0.55f);
    }
}
