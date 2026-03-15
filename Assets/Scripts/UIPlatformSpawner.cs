using UnityEngine;

public class UIPlatformSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Transform spawnPoint;
    public float spawnScale = 1f;

    private GameObject currentPreview;
    private GameObject currentPrefab;

    private bool previewWasInsideTrigger = false;
    private bool isRespawning = false;

    public void SpawnFromCategory(GameObject prefab)
    {
        currentPrefab = prefab;
        ReplacePreview();
    }

    void ReplacePreview()
    {
        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }

        previewWasInsideTrigger = false;
        isRespawning = false;

        SpawnPreview();
    }

    void SpawnPreview()
    {
        if (currentPrefab == null || spawnPoint == null || currentPreview != null)
            return;

        currentPreview = Instantiate(currentPrefab, spawnPoint.position, spawnPoint.rotation);
        currentPreview.transform.localScale = Vector3.one * spawnScale;

        Rigidbody rb = currentPreview.GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        // Give physics one frame to settle before we allow respawn logic
        StartCoroutine(EnablePreviewCheckNextFrame());
    }

    System.Collections.IEnumerator EnablePreviewCheckNextFrame()
    {
        yield return null;
        isRespawning = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (currentPreview == null) return;

        GameObject root = GetRootObject(other);

        if (root == currentPreview)
        {
            previewWasInsideTrigger = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (currentPreview == null) return;
        if (isRespawning) return;

        GameObject root = GetRootObject(other);

        if (root == currentPreview && previewWasInsideTrigger)
        {
            isRespawning = true;
            previewWasInsideTrigger = false;

            // The old object stays in the world
            Rigidbody rb = currentPreview.GetComponent<Rigidbody>();
            if (rb != null)
                rb.isKinematic = false;

            currentPreview = null;
            SpawnPreview();
        }
    }

    GameObject GetRootObject(Collider col)
    {
        if (col.attachedRigidbody != null)
            return col.attachedRigidbody.gameObject;

        return col.transform.root.gameObject;
    }
}