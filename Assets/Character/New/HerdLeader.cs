using UnityEngine;
using UnityEngine.AI;

public class HerdLeader : MonoBehaviour
{
    public float wanderRadius = 15f;
    public float wanderDelay = 4f;

    NavMeshAgent agent;
    float timer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= wanderDelay)
        {
            Vector3 randomPos = transform.position + Random.insideUnitSphere * wanderRadius;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPos, out hit, wanderRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }

            timer = 0;
        }
    }
}