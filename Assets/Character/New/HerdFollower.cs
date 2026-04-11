using UnityEngine;
using UnityEngine.AI;

public class HerdFollower : MonoBehaviour
{
    public Transform leader;
    public float followDistance = 4f;
    public float separationDistance = 1.5f;

    NavMeshAgent agent;
    Vector3 offset;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        offset = Random.insideUnitSphere * followDistance;
        offset.y = 0;
    }

    void Update()
    {
        if (leader == null) return;

        Vector3 targetPos = leader.position + offset;

        // Separation ง่าย ๆ
        Collider[] nearby = Physics.OverlapSphere(transform.position, separationDistance);
        Vector3 push = Vector3.zero;

        foreach (var col in nearby)
        {
            if (col.gameObject != gameObject && col.GetComponent<HerdFollower>())
            {
                push += (transform.position - col.transform.position);
            }
        }

        targetPos += push;

        agent.SetDestination(targetPos);
    }
}