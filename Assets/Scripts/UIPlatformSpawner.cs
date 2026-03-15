using UnityEngine;

public class UIPlatformSpawner : MonoBehaviour
{
    [Header("Placement Settings")]
    public Transform previewPlatform;

    [Header("Spawn Scale")]
    public float spawnScale = 0.15f;

    [Header("Abyss Cleanup")]
    public float destroyBelowY = -50f;

    private GameObject currentPreview;
    private GameObject lastSelectedPrefab;

    public void SpawnFromCategory(GameObject prefab)
    {
        lastSelectedPrefab = prefab;
        ReplacePreview();
    }

    public void ReplacePreview()
    {
        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }

        SpawnPreview();
    }

    public void SpawnPreview()
    {
        if (lastSelectedPrefab == null || previewPlatform == null || currentPreview != null)
            return;

        currentPreview = Instantiate(lastSelectedPrefab, previewPlatform.position, previewPlatform.rotation);

        currentPreview.transform.localScale = Vector3.one * spawnScale;

        Rigidbody rb = currentPreview.GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        PreviewItem previewItem = currentPreview.GetComponent<PreviewItem>();
        if (previewItem == null)
            previewItem = currentPreview.AddComponent<PreviewItem>();

        previewItem.Initialize(this, destroyBelowY);
    }

    public void NotifyPreviewGrabbed(GameObject grabbedObject)
    {
        if (grabbedObject != currentPreview)
            return;

        Rigidbody rb = currentPreview.GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = false;

        currentPreview = null;

        SpawnPreview();
    }

    public void NotifyPreviewDestroyed(GameObject destroyedObject)
    {
        if (destroyedObject == currentPreview)
            currentPreview = null;
    }
}