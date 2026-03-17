using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FarmSpawner : MonoBehaviour
{
    [Header("Animal Prefabs")]
    [Tooltip("Prefabs to spawn. Each prefab should have a NavMeshAgent and (optionally) a Wanderer.")]
    public GameObject[] animalPrefabs;

    [Header("Spawn Settings")]
    public int animalsPerFarm = 3;

    [Tooltip("Random radius around each farm to try spawn candidates.")]
    public float spawnRadiusAroundFarm = 3f;

    [Tooltip("How far we are allowed to search for a NavMesh point from the candidate.")]
    public float maxSpawnSearchDistance = 0.5f;

    [Tooltip("How many random attempts per animal before giving up.")]
    public int attemptsPerAnimal = 10;

    [Header("Spawn Timing")]
    [Tooltip("Delay between each spawned animal to prevent lag spikes.")]
    public float delayBetweenSpawns = 0.5f;

    [Header("Ground Detection")]
    [Tooltip("Raycast height above candidate point.")]
    public float raycastHeight = 50f;

    [Tooltip("Only raycast against these layers for ground. Set to your ground layer(s).")]
    public LayerMask groundMask = ~0;

    [Header("Spawn Placement Safety")]
    [Tooltip("Spawn slightly above the navmesh point to help the agent bind cleanly.")]
    public float spawnHeightOffset = 0.2f;

    [Tooltip("Extra navmesh snap radius after instantiating.")]
    public float postSpawnSnapDistance = 2f;

    void Start()
    {
        StartCoroutine(WaitUntilPlaced());
    }

    IEnumerator WaitUntilPlaced()
    {
        bool isPlaced = false;
        while (!isPlaced)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 0.05f, NavMesh.AllAreas))
            {
                isPlaced = true;
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        if (animalPrefabs == null || animalPrefabs.Length == 0)
        {
            Debug.LogError("No animalPrefabs assigned.");
            yield break;
        }

        for (int i = 0; i < animalsPerFarm; i++)
        {
            TrySpawnAnimal(transform.position);

            if (delayBetweenSpawns > 0f)
                yield return new WaitForSeconds(delayBetweenSpawns);
        }
    }

    void TrySpawnAnimal(Vector3 farmPos)
    {
        if (farmPos.y < -500f) return;

        GameObject prefab = animalPrefabs[UnityEngine.Random.Range(0, animalPrefabs.Length)];

        for (int attempt = 0; attempt < attemptsPerAnimal; attempt++)
        {
            Vector2 r = UnityEngine.Random.insideUnitCircle * spawnRadiusAroundFarm;
            Vector3 candidateXZ = new Vector3(farmPos.x + r.x, 0f, farmPos.z + r.y);

            Vector3 rayStart = new Vector3(candidateXZ.x, raycastHeight, candidateXZ.z);
            Vector3 sampleFrom = candidateXZ;

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit groundHit, raycastHeight * 4f, groundMask))
            {
                sampleFrom = groundHit.point;
            }

            Vector3 probePos = sampleFrom + Vector3.up * 0.5f;

            if (NavMesh.SamplePosition(probePos, out NavMeshHit hit, maxSpawnSearchDistance, NavMesh.AllAreas))
            {
                Spawn(prefab, hit.position);
                return;
            }
        }

        Debug.LogWarning($"Could not find NavMesh near farm to spawn animal. FarmPos={farmPos}");
    }

    void Spawn(GameObject prefab, Vector3 navMeshPosition)
    {
        Vector3 spawnPos = navMeshPosition + Vector3.up * spawnHeightOffset;
        GameObject go = Instantiate(prefab, spawnPos, Quaternion.identity);

        NavMeshAgent agent = go.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            bool wasEnabled = agent.enabled;
            agent.enabled = false;

            if (NavMesh.SamplePosition(navMeshPosition, out NavMeshHit hit, postSpawnSnapDistance, NavMesh.AllAreas))
                go.transform.position = hit.position;
            else
                go.transform.position = navMeshPosition;

            agent.enabled = wasEnabled;

            if (agent.enabled)
                agent.Warp(go.transform.position);
        }
    }
}