using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class WildlifeSpawner : MonoBehaviour
{
    [Header("Tags")]
    public string wildlifeTag = "Wildlife";

    [Header("Wildlife Prefabs")]
    [Tooltip("Prefabs to spawn. Each prefab should have a NavMeshAgent and (optionally) a Wanderer.")]
    public GameObject[] wildlifePrefabs;

    [Header("Spawn Settings")]
    [Tooltip("How many animals spawn for this forest object.")]
    public int wildlifePerForest = 4;

    [Tooltip("Random radius around the forest object to try spawn candidates.")]
    public float spawnRadiusAroundForest = 8f;

    [Tooltip("How far we are allowed to search for a NavMesh point from the candidate. Keep this small!")]
    public float maxSpawnSearchDistance = 0.5f;

    [Tooltip("How many random attempts per animal before giving up.")]
    public int attemptsPerWildlife = 10;

    [Header("Spawn Timing")]
    [Tooltip("Delay between each spawned wildlife animal.")]
    public float delayBetweenSpawns = 0.5f;

    [Header("Ground Detection")]
    [Tooltip("Raycast height above candidate point.")]
    public float raycastHeight = 50f;

    public LayerMask groundMask = ~0;

    void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        if (wildlifePrefabs == null || wildlifePrefabs.Length == 0) yield break;

        // ONLY spawn wildlife for THIS forest's location
        for (int i = 0; i < wildlifePerForest; i++)
        {
            TrySpawnWildlife(transform.position);

            if (delayBetweenSpawns > 0f)
                yield return new WaitForSeconds(delayBetweenSpawns);
        }
    }

    void TrySpawnWildlife(Vector3 forestPos)
    {
        GameObject prefab = wildlifePrefabs[Random.Range(0, wildlifePrefabs.Length)];

        for (int attempt = 0; attempt < attemptsPerWildlife; attempt++)
        {
            Vector2 r = Random.insideUnitCircle * spawnRadiusAroundForest;

            // Search around the forest in XZ only
            Vector3 candidateXZ = new Vector3(forestPos.x + r.x, forestPos.y, forestPos.z + r.y);

            // Raycast from high above world space
            Vector3 rayStart = candidateXZ + Vector3.up * raycastHeight;
            Vector3 sampleFrom = candidateXZ;

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit groundHit, raycastHeight * 2f, groundMask))
            {
                sampleFrom = groundHit.point;
            }

            if (NavMesh.SamplePosition(sampleFrom, out NavMeshHit hit, maxSpawnSearchDistance, NavMesh.AllAreas))
            {
                Spawn(prefab, hit.position);
                return;
            }
        }

        Debug.LogWarning($"Could not find NavMesh near forest to spawn wildlife. ForestPos={forestPos}");
    }

    void Spawn(GameObject prefab, Vector3 position)
    {
        var go = Instantiate(prefab, position, Quaternion.identity);

        if (!string.IsNullOrEmpty(wildlifeTag) && !go.CompareTag(wildlifeTag))
        {
            go.tag = wildlifeTag;
        }

        NavMeshAgent agent = go.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.Warp(position);
        }
    }
}