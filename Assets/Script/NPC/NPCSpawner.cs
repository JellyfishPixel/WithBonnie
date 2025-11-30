using System.Collections;
using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    public static NPCSpawner Instance { get; private set; }

    [Header("NPC Prefabs")]
    [Tooltip("พรีแฟบลูกค้า (ต้องมี NPC + CustomerDialogueInteract อยู่ในตัว)")]
    public GameObject[] npcPrefabs;

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

    float nextSpawnTime;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        ScheduleNextSpawn();
    }

    void Update()
    {
        if (!shopIsOpen)
            return;

        if (CountAlive() >= maxAlive)
            return;

        if (Time.time >= nextSpawnTime)
        {
            SpawnNPC();
            ScheduleNextSpawn();
        }
    }

    // ================= SPAWN CONTROL =================

    void ScheduleNextSpawn()
    {
        nextSpawnTime = Time.time + Random.Range(spawnDelayRange.x, spawnDelayRange.y);
    }

    void SpawnNPC()
    {
        if (npcPrefabs == null || npcPrefabs.Length == 0)
        {
            Debug.LogWarning("[NPCSpawner] NPC Prefabs empty.");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[NPCSpawner] SpawnPoints empty.");
            return;
        }

        GameObject prefab = npcPrefabs[Random.Range(0, npcPrefabs.Length)];
        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];

        GameObject npcObj = Instantiate(prefab, point.position, point.rotation);
        Debug.Log($"[NPCSpawner] Spawned NPC: {npcObj.name}");

        // ====== 👇 ตรงนี้คือส่วนสำคัญ: ฉีดค่า Waypoints / Exit / SpawnPoint ให้ NPC 👇 ======
        var npc = npcObj.GetComponent<NPC>();
        if (npc != null)
        {
            // ถ้ามี entryWaypoints บน Spawner → ส่งให้ NPC
            if (entryWaypoints != null && entryWaypoints.Length > 0)
            {
                npc.entryWaypoints = entryWaypoints;
            }
            else
            {
                Debug.LogWarning("[NPCSpawner] entryWaypoints ยังว่างอยู่ → NPC จะเดินไม่เป็นเส้น");
            }

            // Exit point
            if (exitPoint != null)
            {
                npc.exitPoint = exitPoint;
            }
            else
            {
                Debug.LogWarning("[NPCSpawner] exitPoint ยังไม่ได้เซ็ต → NPC จะ Destroy ทันทีตอน Exiting");
            }

            // จุดวางของ (ถ้า NPC ไม่มี SpawnPoint ของตัวเอง)
            if (npc.SpawnPoint == null && defaultPackageSpawnPoint != null)
            {
                npc.SpawnPoint = defaultPackageSpawnPoint;
            }
        }
        else
        {
            Debug.LogWarning("[NPCSpawner] Prefab ไม่มีคอมโพเนนต์ NPC");
        }
    }

    int CountAlive()
    {
        return FindObjectsByType<NPC>(FindObjectsSortMode.None).Length;
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

    /// <summary>
    /// ปิดร้านและไล่ลูกค้าทั้งหมดออก
    /// </summary>
    public void CloseShopAndClearNPCs()
    {
        shopIsOpen = false;

        var npcs = FindObjectsByType<NPC>(FindObjectsSortMode.None);
        foreach (var npc in npcs)
        {
            npc.OnDeclineDelivery(); // ให้เดินออกสิ้นเชิง
        }

        Debug.Log("[NPCSpawner] Shop CLOSED → all customers leaving.");
    }
}
