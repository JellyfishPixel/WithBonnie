using System.Collections.Generic;
using StarterAssets;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public struct UILockOptions
{
    public bool pauseTime;
    public bool showCursor;
    public bool lockMovement;
    public bool lockCamera;
    public bool blockWorldInput;

    public static UILockOptions Menu => new UILockOptions
    {
        pauseTime = true,
        showCursor = true,
        lockMovement = true,
        lockCamera = true,
        blockWorldInput = true
    };

    public static UILockOptions Dialogue => new UILockOptions
    {
        pauseTime = false,
        showCursor = true,
        lockMovement = true,
        lockCamera = true,
        blockWorldInput = true
    };

    public static UILockOptions Popup => new UILockOptions
    {
        pauseTime = false,
        showCursor = false,
        lockMovement = false,
        lockCamera = false,
        blockWorldInput = true
    };

    public static UILockOptions Transition => new UILockOptions
    {
        pauseTime = false,
        showCursor = false,
        lockMovement = true,
        lockCamera = true,
        blockWorldInput = true
    };
}

public class UILockManager : MonoBehaviour
{
    struct ActiveLock
    {
        public Object owner;
        public UILockOptions options;
    }

    static UILockManager instance;
    static bool isShuttingDown;

    readonly Dictionary<int, ActiveLock> activeLocks = new();

    CameraModeManager cameraModeManager;
    PlayerMovementLocker movementLocker;
    PlayerInteractionSystem interactionSystem;
    StarterAssetsInputs starterInputs;
    CinemachineInputAxisController cinemachineInput;

    public static UILockManager Instance
    {
        get
        {
            if (isShuttingDown)
                return null;

            if (instance == null)
            {
                var go = new GameObject("UILockManager");
                instance = go.AddComponent<UILockManager>();
            }

            return instance;
        }
    }

    public static bool HasInstance => instance != null;

    public static void Release(Object owner)
    {
        if (isShuttingDown || instance == null)
            return;

        instance.PopLock(owner);
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshReferences();
        ApplyLocks();
    }

    void OnDestroy()
    {
        isShuttingDown = true;

        if (instance == this)
            instance = null;

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnApplicationQuit()
    {
        isShuttingDown = true;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshReferences();
        ApplyLocks();
    }

    public void PushLock(Object owner, UILockOptions options)
    {
        if (owner == null)
        {
            Debug.LogWarning("[UILockManager] PushLock ignored because owner is null.");
            return;
        }

        activeLocks[owner.GetInstanceID()] = new ActiveLock
        {
            owner = owner,
            options = options
        };

        ApplyLocks();
    }

    public void PopLock(Object owner)
    {
        if (owner == null)
            return;

        activeLocks.Remove(owner.GetInstanceID());
        ApplyLocks();
    }

    public bool HasLock(Object owner)
    {
        if (owner == null)
            return false;

        return activeLocks.ContainsKey(owner.GetInstanceID());
    }

    void RefreshReferences()
    {
        if (cameraModeManager == null)
            cameraModeManager = CameraModeManager.Instance;

        if (movementLocker == null)
            movementLocker = FindFirstObjectByType<PlayerMovementLocker>();

        if (interactionSystem == null)
            interactionSystem = FindFirstObjectByType<PlayerInteractionSystem>();

        if (starterInputs == null)
            starterInputs = FindFirstObjectByType<StarterAssetsInputs>();

        if (cinemachineInput == null && cameraModeManager != null)
            cinemachineInput = cameraModeManager.cinemachineInput;
    }

    void CleanupDestroyedOwners()
    {
        List<int> deadKeys = null;

        foreach (var pair in activeLocks)
        {
            if (pair.Value.owner != null)
                continue;

            deadKeys ??= new List<int>();
            deadKeys.Add(pair.Key);
        }

        if (deadKeys == null)
            return;

        foreach (int key in deadKeys)
            activeLocks.Remove(key);
    }

    void ApplyLocks()
    {
        CleanupDestroyedOwners();
        RefreshReferences();

        bool pauseTime = false;
        bool showCursor = false;
        bool lockMovement = false;
        bool lockCamera = false;
        bool blockWorldInput = false;

        foreach (var pair in activeLocks.Values)
        {
            var options = pair.options;
            pauseTime |= options.pauseTime;
            showCursor |= options.showCursor;
            lockMovement |= options.lockMovement;
            lockCamera |= options.lockCamera;
            blockWorldInput |= options.blockWorldInput;
        }

        if (movementLocker != null)
        {
            if (lockMovement) movementLocker.Lock();
            else movementLocker.Unlock();
        }

        if (interactionSystem != null)
        {
            if (lockMovement) interactionSystem.LockMovement();
            else interactionSystem.UnlockMovement();
        }

        PlayerInteractionSystem.BlockWorldInput = blockWorldInput;

        if (cameraModeManager != null)
            cameraModeManager.SetCameraInputLocked(lockCamera);

        if (starterInputs != null)
        {
            bool blockCharacterInput = lockMovement || lockCamera;
            starterInputs.enabled = !blockCharacterInput;
            starterInputs.cursorLocked = !showCursor;
            starterInputs.move = Vector2.zero;
            starterInputs.look = Vector2.zero;
            starterInputs.jump = false;
            starterInputs.sprint = false;
        }

        if (cinemachineInput != null)
            cinemachineInput.enabled = !lockCamera;

        Cursor.lockState = showCursor ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = showCursor;
        Time.timeScale = pauseTime ? 0f : 1f;
    }
}
