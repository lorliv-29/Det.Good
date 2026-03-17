using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class FarmSpawner : MonoBehaviour
{
    [Header("Tags")]
    public string animalTag = "Animal";

    [Header("Animal Prefabs")]
    [Tooltip("Prefabs to spawn. Each prefab should have a NavMeshAgent and (optionally) a Wanderer.")]
    public GameObject[] animalPrefabs;

    [Header("Spawn Settings")]
    public int animalsPerFarm = 3;

    [Tooltip("Random radius around each farm to try spawn candidates.")]
    public float spawnRadiusAroundFarm = 3f;

    [Tooltip("How far we are allowed to search for a NavMesh point from the candidate. Keep this small!")]
    public float maxSpawnSearchDistance = 0.5f;

    [Tooltip("How many random attempts per animal before giving up.")]
    public int attemptsPerAnimal = 10;

    [Header("Spawn Timing")]
    [Tooltip("Delay between each spawned animal.")]
    public float delayBetweenSpawns = 0.5f;

    [Header("Ground Detection")]
    [Tooltip("Raycast height above candidate point.")]
    public float raycastHeight = 50f;

    [Tooltip("Only raycast against these layers for ground. Set to your ground layer(s).")]
    public LayerMask groundMask = ~0;

    void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        if (animalPrefabs == null || animalPrefabs.Length == 0) yield break;

        // ONLY spawn animals for THIS farm's location
        for (int i = 0; i < animalsPerFarm; i++)
        {
            TrySpawnAnimal(transform.position);

            if (delayBetweenSpawns > 0f)
                yield return new WaitForSeconds(delayBetweenSpawns);
        }
    }

    void TrySpawnAnimal(Vector3 farmPos)
    {
        GameObject prefab = animalPrefabs[Random.Range(0, animalPrefabs.Length)];

        for (int attempt = 0; attempt < attemptsPerAnimal; attempt++)
        {
            Vector2 r = Random.insideUnitCircle * spawnRadiusAroundFarm;

            // Search around the farm in XZ only
            Vector3 candidateXZ = new Vector3(farmPos.x + r.x, farmPos.y, farmPos.z + r.y);

            // Raycast from high above world space
            Vector3 rayStart = candidateXZ + Vector3.up * raycastHeight;
            Vector3 sampleFrom = candidateXZ;

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit groundHit, raycastHeight * 2f, groundMask))
            {
                sampleFrom = groundHit.point;
            }

            // Sample the NavMesh using the small max search distance to prevent edge-snapping
            if (NavMesh.SamplePosition(sampleFrom, out NavMeshHit hit, maxSpawnSearchDistance, NavMesh.AllAreas))
            {
                Spawn(prefab, hit.position);
                return;
            }
        }

        Debug.LogWarning($"Could not find NavMesh near farm to spawn animal. FarmPos={farmPos}");
    }

    void Spawn(GameObject prefab, Vector3 position)
    {
        var go = Instantiate(prefab, position, Quaternion.identity);

        if (!string.IsNullOrEmpty(animalTag) && !go.CompareTag(animalTag))
            go.tag = animalTag;

        NavMeshAgent agent = go.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.Warp(position);
        }
    }
}