using UnityEngine;
using System.Collections;

public class UIPlatformSpawner : MonoBehaviour
{
    [Header("Core Reference")]
    public GameStateManager gameStateManager;

    [Header("Spawn Settings")]
    public Transform spawnPoint;
    public float spawnScale = 1f;

    private GameObject currentPreview;
    private GameObject currentPrefab;

    private bool previewWasInsideTrigger = false;
    private bool isRespawning = false;

    bool IsPhase2()
    {
        return gameStateManager != null &&
               gameStateManager.currentPhase == GameStateManager.GamePhase.Phase2_Create;
    }

    public void SpawnFromCategory(GameObject prefab)
    {
        if (!IsPhase2())
            return;

        currentPrefab = prefab;
        ReplacePreview();
    }

    void ReplacePreview()
    {
        if (!IsPhase2())
            return;

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
        if (!IsPhase2())
            return;

        if (currentPrefab == null || spawnPoint == null || currentPreview != null)
            return;

        currentPreview = Instantiate(currentPrefab, spawnPoint.position, spawnPoint.rotation);
        currentPreview.transform.localScale = Vector3.one * spawnScale;

        Rigidbody rb = currentPreview.GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        StartCoroutine(EnablePreviewCheckNextFrame());
    }

    IEnumerator EnablePreviewCheckNextFrame()
    {
        yield return null;
        isRespawning = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPhase2())
            return;

        if (currentPreview == null)
            return;

        GameObject root = GetRootObject(other);

        if (root == currentPreview)
        {
            previewWasInsideTrigger = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPhase2())
            return;

        if (currentPreview == null)
            return;

        if (isRespawning)
            return;

        GameObject root = GetRootObject(other);

        if (root == currentPreview && previewWasInsideTrigger)
        {
            isRespawning = true;
            previewWasInsideTrigger = false;

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

    public void ClearPreview()
    {
        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }

        previewWasInsideTrigger = false;
        isRespawning = false;
    }
}