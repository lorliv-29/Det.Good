using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PeopleApproachController : MonoBehaviour
{
    [Header("Approach Settings")]
    public float approachSpeed = 0.5f;
    public float approachAcceleration = 1.5f;
    public float approachAngularSpeed = 120f;
    public float stoppingDistance = 1.25f;
    public float repathInterval = 0.5f;

    NavMeshAgent agent;
    Transform targetPlayer;
    bool isApproaching;
    float nextRepathTime;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        if (agent != null)
        {
            agent.speed = approachSpeed;
            agent.acceleration = approachAcceleration;
            agent.angularSpeed = approachAngularSpeed;
            agent.stoppingDistance = stoppingDistance;
            agent.autoBraking = true;
        }
    }

    void Update()
    {
        if (!isApproaching || targetPlayer == null || agent == null || !agent.isOnNavMesh)
            return;

        if (Time.time >= nextRepathTime)
        {
            agent.SetDestination(targetPlayer.position);
            nextRepathTime = Time.time + repathInterval;
        }
    }

    public void BeginApproach(Transform player)
    {
        targetPlayer = player;
        isApproaching = true;

        Wanderer wanderer = GetComponent<Wanderer>();
        if (wanderer != null)
            wanderer.PauseWandering();

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = approachSpeed;
            agent.acceleration = approachAcceleration;
            agent.angularSpeed = approachAngularSpeed;
            agent.stoppingDistance = stoppingDistance;
            agent.SetDestination(player.position);
            nextRepathTime = Time.time + repathInterval;
        }
    }

    public void StopApproach()
    {
        isApproaching = false;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }
}