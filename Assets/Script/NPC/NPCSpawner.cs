using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    public static NPCSpawner Instance { get; private set; }


    [Header("NPC Prefabs By Map")]
    public GameObject[] mapAPrefabs;
    public GameObject[] mapBPrefabs;
    private List<GameObject> currentRoundPool = new List<GameObject>();
    private bool roundInitialized = false;

    private int currentIndex = 0;
    [Header("Spawn Points (ตำแหน่งที่ลูกค้าเกิด)")]
    public Transform[] spawnPoints;

    [Header("Path / Waypoints (แชร์ให้ทุก NPC ที่ spawn จากตัวนี้)")]
    [Tooltip("จุดเดินเข้าร้านเรียงลำดับ 1 → 2 → 3 → ...")]
    public Transform[] entryWaypoints;

    [Tooltip("จุดวางของ / หน้าโต๊ะ ถ้า NPC มี SpawnPoint เป็น null จะใช้ค่านี้แทน")]
    public Transform defaultPackageSpawnPoint;

    [Tooltip("จุดเดินออกจากร้าน")]
    public Transform exitPoint;

    [Header("Spawn Control")]
    [Tooltip("ช่วงเวลาหน่วงก่อนเกิดลูกค้าคนถัดไป")]
    public Vector2 spawnDelayRange = new Vector2(3f, 6f);

    [Tooltip("จำนวนลูกค้าที่อยู่ในร้านพร้อมกัน (ระบบนี้แนะนำ = 1)")]
    public int maxAlive = 1;

    [Header("Shop State")]
    [Tooltip("ร้านเปิดอยู่หรือไม่")]
    public bool shopIsOpen = true;
    [Header("Inventory Lock")]
    [Tooltip("ล็อกการ spawn NPC ถ้า inventory เคยเต็มจนกว่าจะเคลียร์กล่องหมด")]
    [SerializeField] private bool inventoryLocked = false;

    float nextSpawnTime;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        ScheduleNextSpawn();
    }
    void InitializeNewRound()
    {
        currentRoundPool.Clear();

        // รวม prefab จากทั้งสองแมพ
        List<GameObject> combined = new List<GameObject>();
        combined.AddRange(mapAPrefabs);
        combined.AddRange(mapBPrefabs);

        // สุ่ม 3 ตัวไม่ซ้ำ
        int targetCount = Mathf.Min(3, combined.Count);

        for (int i = 0; i < targetCount; i++)
        {
            int randIndex = Random.Range(0, combined.Count);
            currentRoundPool.Add(combined[randIndex]);
            combined.RemoveAt(randIndex);
        }

        roundInitialized = true;

        Debug.Log("[NPCSpawner] New Round Initialized");
    }
    void Update()
    {
        if (!shopIsOpen)
            return;

        // ===== เช็คสภาพ inventory ก่อน =====
        var inv = BoxInventory.Instance;
        if (inv != null)
        {
            int used = inv.GetUsedSlotCount();   // ใช้ method ที่เราเพิ่งเพิ่ม

            // ถ้ามีของครบ maxSlots → เข้าสู่โหมดล็อก
            if (used >= inv.maxSlots)
            {
                inventoryLocked = true;
            }

            if (inventoryLocked)
            {
                // ถ้ายังเหลือกล่องใน inventory (1–maxSlots) → ห้าม spawn
                if (used > 0)
                    return;

                if (used == 0)
                {
                    inventoryLocked = false;
                    roundInitialized = false; // เริ่มรอบใหม่
                }
            }
        }

        // ===== เช็คจำนวน NPC ที่มีอยู่ในร้านตอนนี้ =====
        if (CountAlive() >= maxAlive)
            return;

        // ===== เวลา spawn NPC ตาม timer เดิม =====
        if (Time.time >= nextSpawnTime)
        {
            SpawnNPC();
            ScheduleNextSpawn();
        }
    }

    void ScheduleNextSpawn()
    {
        nextSpawnTime = Time.time + Random.Range(spawnDelayRange.x, spawnDelayRange.y);
    }

    void SpawnNPC()
    {
        if (!roundInitialized || currentRoundPool.Count == 0)
        {
            InitializeNewRound();
        }

        if (currentRoundPool.Count == 0)
        {
            Debug.Log("[NPCSpawner] No NPC left in this round.");
            return;
        }

        GameObject prefab = currentRoundPool[0];
        currentRoundPool.RemoveAt(0);

        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject npcObj = Instantiate(prefab, point.position, point.rotation);

        Debug.Log($"[NPCSpawner] Spawned NPC: {npcObj.name}");

        var npc = npcObj.GetComponent<NPC>();
        if (npc != null)
        {
            if (entryWaypoints != null && entryWaypoints.Length > 0)
                npc.entryWaypoints = entryWaypoints;

            if (exitPoint != null)
                npc.exitPoint = exitPoint;

            if (npc.SpawnPoint == null && defaultPackageSpawnPoint != null)
                npc.SpawnPoint = defaultPackageSpawnPoint;
        }
    }
    int CountAlive()
    {
        var npcs = FindObjectsByType<NPC>(FindObjectsSortMode.None);

        int count = 0;
        foreach (var npc in npcs)
        {
            if (!npc.isStaticNPC)
                count++;
        }

        return count;
    }


    // ================= EXTERNAL CALL =================

    /// <summary>
    /// ใช้จากป้าย OPEN / CLOSED
    /// </summary>
    public void SetSpawningEnabled(bool enable)
    {
        shopIsOpen = enable;

        if (shopIsOpen)
        {
            ScheduleNextSpawn();
            Debug.Log("[NPCSpawner] Shop OPEN → start spawning customers.");
        }
        else
        {
            Debug.Log("[NPCSpawner] Shop CLOSED → stop spawning customers.");
        }
    }

    public void CloseShopAndClearNPCs()
    {
        shopIsOpen = false;

        var npcs = FindObjectsByType<NPC>(FindObjectsSortMode.None);
        foreach (var npc in npcs)
        {
            if (!npc.isStaticNPC)
                npc.OnDeclineDelivery();
        }


        Debug.Log("[NPCSpawner] Shop CLOSED → all customers leaving.");
    }

}
