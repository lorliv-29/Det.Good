using System.Collections;
using UnityEngine;

public class InsectSpawner : MonoBehaviour
{
    [Header("Tags")]
    public string flowerTag = "Flowers";
    public string insectTag = "Insect";

    [Header("Insect Prefabs")]
    public GameObject[] insectPrefabs;

    [Header("Spawn Control")]
    [Tooltip("Chance this flower will spawn insects at all (0..1). Example 0.25 = 25% chance).")]
    [Range(0f, 1f)]
    public float spawnChanceForThisFlower = 1.0f; // Defaulted to 1 so you always get bugs for the demo!

    [Header("Spawn Settings")]
    [Tooltip("How many insects spawn for this flower.")]
    public int insectsPerFlower = 1;

    public float spawnRadiusAroundFlower = 2f;

    [Header("Spawn Timing")]
    [Tooltip("Delay between each spawned insect.")]
    public float delayBetweenSpawns = 0.25f;

    [Header("Butterfly Behaviour")]
    public float hoverHeight = 5f;
    public float scaleMultiplier = 10f;

    void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        if (insectPrefabs == null || insectPrefabs.Length == 0) yield break;

        // Roll the dice to see if this specific flower gets insects
        if (Random.value > spawnChanceForThisFlower) yield break;

        // ONLY spawn insects for THIS flower's location
        for (int i = 0; i < insectsPerFlower; i++)
        {
            SpawnInsectForFlower(transform);

            if (delayBetweenSpawns > 0f)
                yield return new WaitForSeconds(delayBetweenSpawns);
        }
    }

    void SpawnInsectForFlower(Transform flower)
    {
        var prefab = insectPrefabs[Random.Range(0, insectPrefabs.Length)];

        Vector2 r = Random.insideUnitCircle * spawnRadiusAroundFlower;
        Vector3 spawnPos = new Vector3(
            flower.position.x + r.x,
            flower.position.y + hoverHeight,
            flower.position.z + r.y
        );

        var go = Instantiate(prefab, spawnPos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));

        go.transform.localScale *= scaleMultiplier;

        if (!string.IsNullOrEmpty(insectTag) && !go.CompareTag(insectTag))
            go.tag = insectTag;

        // If you are using your custom ButterflyHover script, configure it here!
        var hover = go.GetComponent<ButterflyHoverWander>();
        if (hover != null)
        {
            hover.orbitCenter = flower;
            hover.baseHeight = hoverHeight;
            hover.flowerTag = flowerTag;
        }
    }
}