using UnityEngine;

public class UIPlatformSpawner : MonoBehaviour
{
    [Header("Placement Settings")]
    public Transform previewPlatform;
    public float spawnScale = 0.15f;
    public float exitDistance = 0.1f;

    private GameObject currentPreview;
    private GameObject lastSelectedPrefab;

    public void SpawnFromCategory(GameObject prefab)
    {
        lastSelectedPrefab = prefab;
        RefreshPlatform();
    }

    void Update()
    {
        if (lastSelectedPrefab != null && currentPreview == null)
        {
            RefreshPlatform();
        }

        if (currentPreview != null && previewPlatform != null)
        {
            if (Vector3.Distance(currentPreview.transform.position, previewPlatform.position) > exitDistance)
            {
               
                currentPreview.transform.SetParent(null);

                currentPreview = null;

                Debug.Log("Object removed from platform. Spawning replacement...");
            }
        }
    }

    public void RefreshPlatform()
    {
        if (previewPlatform == null || lastSelectedPrefab == null) return;

        if (currentPreview != null) Destroy(currentPreview);

        currentPreview = Instantiate(lastSelectedPrefab, previewPlatform.position, previewPlatform.rotation);

        currentPreview.transform.SetParent(previewPlatform);
        currentPreview.transform.localScale = Vector3.one * spawnScale;

        Rigidbody rb = currentPreview.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        Debug.Log("New Preview Spawned: " + currentPreview.name);
    }
}