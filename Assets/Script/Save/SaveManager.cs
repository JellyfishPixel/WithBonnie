using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    public bool IsLoading => false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SaveGame()
    {
        Debug.Log("[SaveManager] Full game save is disabled. Economy stock still uses EconomyManager persistence.");
    }

    public void LoadGame()
    {
        Debug.Log("[SaveManager] Full game load is disabled. Start a new session instead.");
    }

    public bool HasSave()
    {
        return false;
    }
}
