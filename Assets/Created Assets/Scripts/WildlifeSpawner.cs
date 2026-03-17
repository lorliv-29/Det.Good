using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WildlifeSpawner : MonoBehaviour
{
    [Header("Wildlife Prefabs")]
    public GameObject[] wildlifePrefabs;

    [Header("Spawn Settings")]
    public int wildlifePerForest = 2;
    public float spawnRadiusAroundForest = 3f;
    public float maxSpawnSearchDistance = 0.5f;
    public int attemptsPerWildlife = 10;

    [Header("Spawn Timing")]
    public float delayBetweenSpawns = 0.5f;

    [Header("Ground Detection")]
    public float raycastHeight = 50f;
    public LayerMask groundMask = ~0;

    [Header("Spawn Placement Safety")]
    public float spawnHeightOffset = 0.2f;
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
        if (wildlifePrefabs == null || wildlifePrefabs.Length == 0) yield break;

        for (int i = 0; i < wildlifePerForest; i++)
        {
            TrySpawnWildlife(transform.position);
            if (delayBetweenSpawns > 0f) yield return new WaitForSeconds(delayBetweenSpawns);
        }
    }

    void TrySpawnWildlife(Vector3 forestPos)
    {
        if (forestPos.y < -500f) return;

        GameObject prefab = wildlifePrefabs[UnityEngine.Random.Range(0, wildlifePrefabs.Length)];

        for (int attempt = 0; attempt < attemptsPerWildlife; attempt++)
        {
            Vector2 r = UnityEngine.Random.insideUnitCircle * spawnRadiusAroundForest;
            Vector3 candidateXZ = new Vector3(forestPos.x + r.x, 0f, forestPos.z + r.y);

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
        Debug.LogWarning($"Could not find NavMesh near forest to spawn wildlife. ForestPos={forestPos}");
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

            if (agent.enabled) agent.Warp(go.transform.position);
        }
    }
}