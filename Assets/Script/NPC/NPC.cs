using System;
using UnityEngine;

public class NPC : MonoBehaviour, IInteractable
{
    public NPCData data;
    [Header("Static NPC")]
    public bool isStaticNPC = false;
    public bool IsStatic => isStaticNPC;


    [Header("Move")]
    public float moveSpeed = 3f;
    public float reachThreshold = 0.2f;
    static readonly int AnimSpeed = Animator.StringToHash("Speed");
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
    [Header("NPC SFX")]
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private AudioClip spawnItemSound;
    [SerializeField] private AudioClip interactSound;

    [Range(0, 1)]
    [SerializeField] private float footstepVolume = 1f;

    [SerializeField] private float footstepRate = 0.5f;

    float footstepTimer;
    bool hasTalked = false;
    protected virtual void Start()
    {
        Animation = GetComponentInChildren<Animator>();

        if (!Animation)
        {
            Debug.LogError($"[NPC] Animator not found in children of {name}");
            return;
        }

        itemDialogueManager = FindFirstObjectByType<ItemDialogueManager>();

        if (isStaticNPC)
        {
            state = State.Waiting;
            SetAnimSpeed(0f);
        }
    }

    void SetAnimSpeed(float value)
    {
        if (Animation)
            Animation.SetFloat(AnimSpeed, value);
    }
    protected virtual void OnDestroy()
    {
    }
    public void Interact(PlayerInteractionSystem interactor,
                         PlayerInteractionSystem.InteractionType type)
    {
        if (hasTalked) return; // ❌ คุยแล้วห้ามกดอีก

        if (isStaticNPC)
        {
            PlayInteractSound();
            TryTalk(interactor);
            hasTalked = true; // 🔒 ล็อกหลังคุย
            GuideArrowManager.Instance?.NextTarget();
            return;
        }

        if (state == State.Entering || state == State.Waiting)
        {
            PlayInteractSound();
            TryTalk(interactor);
            hasTalked = true; // 🔒 ล็อกหลังคุย
            GuideArrowManager.Instance?.NextTarget();
        }
    }
    void HandleFootstep()
    {
        if (Animation == null) return;

        float speed = Animation.GetFloat(AnimSpeed);

        if (speed < 0.1f)
            return;

        footstepTimer -= Time.deltaTime;

        if (footstepTimer <= 0f)
        {
            if (footstepClips.Length > 0)
            {
                int index = UnityEngine.Random.Range(0, footstepClips.Length);

                AudioManager.Instance.PlaySFX(
                    footstepClips[index],
                    transform.position
                );
            }

            footstepTimer = footstepRate;
        }
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

        if (isStaticNPC)
            return; 


        if (state == State.Done) return;

        switch (state)
        {
            case State.Entering:
                SetAnimSpeed(moveSpeed);
                UpdateEntering();
                break;

            case State.Waiting:
                SetAnimSpeed(0f);
                break;

            case State.Exiting:
                SetAnimSpeed(moveSpeed);
                UpdateExiting();
                break;
        }
        HandleFootstep();

    }


    protected virtual void UpdateEntering()
    {
        if (entryWaypoints == null || entryWaypoints.Length == 0)
            return;

        if (entryIndex < entryWaypoints.Length)
        {
            SetAnimSpeed(moveSpeed);
            MoveTowards(entryWaypoints[entryIndex].position);

            if (IsReached(entryWaypoints[entryIndex].position))
            {
                entryIndex++;

               
                if (entryIndex >= entryWaypoints.Length)
                {
                    SetAnimSpeed(0f);
                    SpawnPackageAndWait();
                    return; 
                }
            }

        }
        else
        {
            SpawnPackageAndWait();
        }
    }



    protected virtual void SpawnPackageAndWait()
    {
        SetAnimSpeed(0f);
        state = State.Waiting;
    }



    protected virtual void UpdateExiting()
    {
        if (exitPoint == null)
        {
            
            if (GameManager.Instance != null && GameManager.Instance.currentCustomer == this)
                GameManager.Instance.currentCustomer = null;

            Destroy(gameObject);
            state = State.Done;
            return;
        }

        SetAnimSpeed(moveSpeed);
        MoveTowards(exitPoint.position);

        if (IsReached(exitPoint.position))
        {
            
            if (GameManager.Instance != null && GameManager.Instance.currentCustomer == this)
                GameManager.Instance.currentCustomer = null;

            Destroy(gameObject);
            state = State.Done;
        }
    }


    protected void MoveTowards(Vector3 target)
    {
        float step = moveSpeed * Time.deltaTime;
        if (step <= 0f) return;

        transform.position = Vector3.MoveTowards(transform.position, target, step);

        Vector3 dir = target - transform.position;
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
        if (isStaticNPC) return;

        if (state == State.Done) return;

        if (itemOnTable) Destroy(itemOnTable);
        else if (spawnedPackageRef) Destroy(spawnedPackageRef);

        state = State.Exiting;
        itemDialogueManager?.Close();
    }

    public void OnAcceptDelivery()
    {
        if (isStaticNPC)
            return;
        if (hasSpawnedPackage) return;

        Debug.Log("[NPC] Accepted");

        if (!isStaticNPC && GameManager.Instance != null)
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
            // 🔊 Spawn Item SFX
            AudioManager.Instance.PlaySFX(
                spawnItemSound,
                transform.position
            );
            // 🔥 NPC ผูกกับ ITEM เท่านั้น
            var item = spawnedPackageRef.GetComponent<DeliveryItemInstance>();
            if (item != null)
            {
                item.ownerNPC = this;
            }
            else
            {
                Debug.LogError("[NPC] Spawned package has no DeliveryItemInstance");
            }
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
    void PlayInteractSound()
    {
        if (interactSound == null) return;

        AudioManager.Instance.PlaySFX(
            interactSound,
            transform.position
        );
    }
    void TryTalk(PlayerInteractionSystem player)
    {
        if (itemDialogueManager == null || data == null)
        {
            Debug.LogWarning("[NPC] DialogueManager or data missing");
            return;
        }

        ItemDialogueData dialogue = null;

        // 🔹 Static NPC → ใช้ dialogue จาก NPCData
        if (isStaticNPC)
        {
            dialogue = data.staticDialogue;
        }
        // 🔹 Dynamic NPC → ใช้ dialogue จาก Item
        else if (data.package != null)
        {
            dialogue = data.package
                .GetComponent<DeliveryItemInstance>()
                ?.data
                ?.dialogueData;
        }

        if (dialogue == null)
        {
            Debug.LogWarning("[NPC] No dialogueData found");
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
        if (isStaticNPC)
            return;

        if (state != State.Waiting)
            return; // ❗ รับของได้เฉพาะตอนยืนแล้ว

        if (choiceIndex == 0)
            OnAcceptDelivery();
        else if (choiceIndex == 1)
            OnDeclineDelivery();
    }


    protected State GetStateWaiting() => State.Waiting;
}
