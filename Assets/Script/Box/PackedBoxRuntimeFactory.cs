using UnityEngine;

public static class PackedBoxRuntimeFactory
{
    public static BoxCore Spawn(GameObject boxPrefab, PackedBoxData package, Vector3 position, Quaternion rotation)
    {
        if (boxPrefab == null || package == null)
            return null;

        GameObject instance = Object.Instantiate(boxPrefab, position, rotation);
        if (instance == null)
            return null;

        if (HasMissingScripts(instance))
        {
            Debug.LogError($"[PackedBoxRuntimeFactory] Prefab '{boxPrefab.name}' has missing scripts. Spawn cancelled.");
            Object.Destroy(instance);
            return null;
        }

        BoxCore box = instance.GetComponent<BoxCore>();

        if (box == null)
        {
            Object.Destroy(instance);
            Debug.LogError("[PackedBoxRuntimeFactory] Spawned prefab has no BoxCore.");
            return null;
        }

        DeliveryItemInstance runtimeItem = instance.GetComponentInChildren<DeliveryItemInstance>(true);
        box.ApplyPackedData(package, runtimeItem);
        return box;
    }

    static bool HasMissingScripts(GameObject root)
    {
        if (root == null)
            return true;

        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            var behaviours = transforms[i].GetComponents<MonoBehaviour>();
            for (int j = 0; j < behaviours.Length; j++)
            {
                if (behaviours[j] == null)
                    return true;
            }
        }

        return false;
    }
}
