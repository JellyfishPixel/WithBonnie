using StarterAssets;
using UnityEngine;
using UnityEngine.AI;

public class FleeFromPlayer : MonoBehaviour
{
    ThirdPersonController player;
    public float detectRadius = 10f;
    public float runDistance = 15f;
    public float panicSpeed = 8f;
    public float normalSpeed = 3.5f;

    NavMeshAgent agent;

    //void Start()
    //{
    //    agent = GetComponent<NavMeshAgent>();
    //    ThirdPersonController player = FindFirstObjectByType<ThirdPersonController>();
    //}

    //void Update()
    //{
    //    float dist = Vector3.Distance(transform.position, player.transform.position);

    //    if (dist < detectRadius)
    //    {
    //        agent.speed = panicSpeed;

    //        Vector3 dir = (transform.position - player.transform.position).normalized;
    //        Vector3 fleePos = transform.position + dir * runDistance;

    //        NavMeshHit hit;
    //        if (NavMesh.SamplePosition(fleePos, out hit, 10f, NavMesh.AllAreas))
    //        {
    //            agent.SetDestination(hit.position);
    //        }
    //    }
    //    else
    //    {
    //        agent.speed = normalSpeed;
    //    }
    //}
}