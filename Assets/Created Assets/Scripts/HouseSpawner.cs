using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class HouseSpawner : MonoBehaviour
{
    [Header("People Prefabs")]
    [Tooltip("Prefabs to spawn. Each prefab should have a NavMeshAgent and (optionally) a Wanderer.")]
    public GameObject[] peoplePrefabs;

    [Header("Spawn Settings")]
    public int peoplePerHouse = 2;

    [Tooltip("Random radius around each house to try spawn candidates.")]
    public float spawnRadiusAroundHouse = 3f;

    [Tooltip("How far we are allowed to search for a NavMesh point from the candidate. Keep this small (e.g. 0.5) so they don't spawn off the table!")]
    public float maxSpawnSearchDistance = 0.5f;

    [Tooltip("How many random attempts per person before giving up.")]
    public int attemptsPerPerson = 10;

    [Header("Spawn Timing")]
    [Tooltip("Delay between each spawned person to prevent lag spikes.")]
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
        // Do not spawn immediately! Wait until the player places it on the sand.
        StartCoroutine(WaitUntilPlaced());
    }

    IEnumerator WaitUntilPlaced()
    {
        bool isPlaced = false;
        while (!isPlaced)
        {
            // Check if this building is currently touching or very close to the baked NavMesh (the sand)
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 0.05f, NavMesh.AllAreas))
            {
                isPlaced = true; // We found the sand!
            }
            else
            {
                // We are still in the air/on the menu. Wait half a second and check again.
                yield return new WaitForSeconds(0.5f);
            }
        }

        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        if (peoplePrefabs == null || peoplePrefabs.Length == 0)
        {
            Debug.LogError("No peoplePrefabs assigned.");
            yield break;
        }

        // ONLY spawn people for THIS specific house's location
        for (int i = 0; i < peoplePerHouse; i++)
        {
            TrySpawnPerson(transform.position);

            if (delayBetweenSpawns > 0f)
                yield return new WaitForSeconds(delayBetweenSpawns);
        }
    }

    void TrySpawnPerson(Vector3 housePos)
    {
        if (housePos.y < -500f)
        {
            Debug.Log($"Skipping invalid house source at {housePos}");
            return;
        }

        GameObject prefab = peoplePrefabs[UnityEngine.Random.Range(0, peoplePrefabs.Length)];

        for (int attempt = 0; attempt < attemptsPerPerson; attempt++)
        {
            Vector2 r = UnityEngine.Random.insideUnitCircle * spawnRadiusAroundHouse;

            // Search around the house in XZ only. Do not inherit a broken house Y.
            Vector3 candidateXZ = new Vector3(housePos.x + r.x, 0f, housePos.z + r.y);

            // Raycast from high above world space so we can find the actual ground.
            Vector3 rayStart = new Vector3(candidateXZ.x, raycastHeight, candidateXZ.z);
            Vector3 sampleFrom = candidateXZ;

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit groundHit, raycastHeight * 4f, groundMask))
            {
                sampleFrom = groundHit.point;
            }

            // Probe a little above the surface to help NavMesh.SamplePosition.
            Vector3 probePos = sampleFrom + Vector3.up * 0.5f;

            if (NavMesh.SamplePosition(probePos, out NavMeshHit hit, maxSpawnSearchDistance, NavMesh.AllAreas))
            {
                Spawn(prefab, hit.position);
                return;
            }
        }

        Debug.LogWarning($"Could not find NavMesh near house to spawn person. HousePos={housePos}");
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

            // Snap once to NavMesh after instantiating.
            if (NavMesh.SamplePosition(navMeshPosition, out NavMeshHit hit, postSpawnSnapDistance, NavMesh.AllAreas))
            {
                go.transform.position = hit.position;
            }
            else
            {
                go.transform.position = navMeshPosition;
            }

            agent.enabled = wasEnabled;

            if (agent.enabled)
            {
                agent.Warp(go.transform.position);
            }
        }
    }
}