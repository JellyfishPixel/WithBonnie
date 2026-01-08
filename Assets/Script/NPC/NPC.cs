using System;
using UnityEngine;

public class NPC : MonoBehaviour, IInteractable
{
    public NPCData data;

    [Header("Move")]
    public float moveSpeed = 3f;
    public float reachThreshold = 0.2f;

    [Header("Path In (waypoints 1→2→3...)")]
    public Transform[] entryWaypoints;
    public Transform SpawnPoint;

    [Header("Exit")]
    public Transform exitPoint;

    protected int entryIndex = 0;
    protected bool hasSpawnedPackage = false;

    protected enum State { Entering, Waiting, Exiting, Done }
    protected State state = State.Entering;


    protected GameObject spawnedPackageRef;
    public ItemDialogueManager itemDialogueManager;

    protected Animator Animation;
    

    protected virtual void Start()
    {
        Animation = GetComponent<Animator>();
        itemDialogueManager = FindFirstObjectByType<ItemDialogueManager>();
    }

    protected virtual void OnDestroy()
    {
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DialogTable"))
        {
            Debug.Log("[NPC] Reached table");

            if (Animation)
                Animation.SetBool("TableCollision", true);
        }
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("DialogTable"))
        {
            Debug.Log("[NPC] Left table");

            if (Animation)
                Animation.SetBool("TableCollision", false);
        }
    }

    public void Interact(PlayerInteractionSystem interactor,
                         PlayerInteractionSystem.InteractionType type)
    {
        // E เท่านั้น
        if (type != PlayerInteractionSystem.InteractionType.Secondary)
            return;

        TryTalk(interactor);
    }


    public void HandleBoxStored()
    {
        if (hasSpawnedPackage && state == State.Waiting)
        {
            state = State.Exiting;
        }
    }


    protected virtual void Update()
    {
        if (state == State.Done) return;

        switch (state)
        {
            case State.Entering:
                UpdateEntering();
                break;
            case State.Waiting:
                // ถ้าของ/กล่องที่ NPC spawn หายไป (ถูก Destroy) → ให้ออก
                if (hasSpawnedPackage && spawnedPackageRef == null)
                {
                    state = State.Exiting;
                    itemDialogueManager?.Close();
                }
                break;
            case State.Exiting:
                UpdateExiting();
                break;
        }
    }

    protected virtual void UpdateEntering()
    {
        // เดินตาม waypoints ก่อน
        if (entryWaypoints != null && entryWaypoints.Length > 0 && entryIndex < entryWaypoints.Length)
        {
            MoveTowards(entryWaypoints[entryIndex].position);
            if (IsReached(entryWaypoints[entryIndex].position))
                entryIndex++;
            return;
        }

        // เดินครบทุก waypoint แล้ว → ยืนรอหน้าโต๊ะ (โต๊ะควรอยู่แถว waypoint สุดท้าย)
        SpawnPackageAndWait();
    }

    protected virtual void SpawnPackageAndWait()
    {
        state = State.Waiting;
    }

    protected virtual void UpdateExiting()
    {
        if (exitPoint == null)
        {
            // ถ้าลูกค้าคนนี้เป็น currentCustomer อยู่ → เคลียร์ออก
            if (GameManager.Instance != null && GameManager.Instance.currentCustomer == this)
                GameManager.Instance.currentCustomer = null;

            Destroy(gameObject);
            state = State.Done;
            return;
        }

        MoveTowards(exitPoint.position);
        if (IsReached(exitPoint.position))
        {
            // ถึงจุดออกแล้ว → ไม่ถือว่าเป็น currentCustomer อีกต่อไป
            if (GameManager.Instance != null && GameManager.Instance.currentCustomer == this)
                GameManager.Instance.currentCustomer = null;

            Destroy(gameObject);
            state = State.Done;
        }
    }


    protected void MoveTowards(Vector3 target)
    {
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
        Vector3 dir = (target - transform.position);
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion look = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 10f * Time.deltaTime);
        }
    }

    protected bool IsReached(Vector3 target)
    {
        return Vector3.Distance(transform.position, target) <= reachThreshold;
    }

    public NPCData GetData() => data;

    public void ForceExitAndClearItem(GameObject itemOnTable = null)
    {
        if (state == State.Done) return;

        if (itemOnTable) Destroy(itemOnTable);
        else if (spawnedPackageRef) Destroy(spawnedPackageRef);

        state = State.Exiting;
        itemDialogueManager?.Close();
    }

    public void OnAcceptDelivery()
    {
        if (hasSpawnedPackage) return;

        Debug.Log("[NPC] Accepted");

        // แจ้ง GameManager ว่าตอนนี้ลูกค้าคนนี้คือ currentCustomer
        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentCustomer = this;
        }

        // spawn ของออกจากตัว NPC
        if (data != null && data.package != null)
        {
            spawnedPackageRef = Instantiate(
                data.package,
                SpawnPoint ? SpawnPoint.position : transform.position,
                Quaternion.identity
            );

            var box = spawnedPackageRef.GetComponent<BoxCore>();
            if (box != null)
                box.ownerNPC = this;
        }

        hasSpawnedPackage = true;

        // ยืนรอแพ็ค
        state = State.Waiting;
    }


    public void OnDeclineDelivery()
    {
        Debug.Log("[NPC] Declined");
        ForceExitAndClearItem();
    }
    void TryTalk(PlayerInteractionSystem player)
    {


        if (itemDialogueManager == null || data == null || data.package == null)
        {
            Debug.LogWarning("[NPC] DialogueManager or data missing");
            return;
        }

        // 📌 ให้ ItemDialogueData เก็บอยู่ใน "ของ" (package)
        var dialogue = data.package.GetComponent<DeliveryItemInstance>()?.data?.dialogueData;

        if (dialogue == null)
        {
            Debug.LogWarning("[NPC] No dialogueData in item");
            return;
        }

        Debug.Log("[NPC] Start Dialogue");

        itemDialogueManager.Show(
            actorOwner: gameObject,
            flow: dialogue,
            onChoice: OnDialogueChoice,
            onFinished: null
        );
    }
    void OnDialogueChoice(int choiceIndex)
    {
        if (choiceIndex == 0)
            OnAcceptDelivery();
        else if (choiceIndex == 1)
            OnDeclineDelivery();
    }




    protected State GetStateWaiting() => State.Waiting;
}
