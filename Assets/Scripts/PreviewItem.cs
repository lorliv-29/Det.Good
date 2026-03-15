using UnityEngine;

public class PreviewItem : MonoBehaviour
{
    private UIPlatformSpawner spawner;
    private float destroyBelowY = -50f;
    private bool hasBeenGrabbed = false;

    public void Initialize(UIPlatformSpawner owner, float abyssY)
    {
        spawner = owner;
        destroyBelowY = abyssY;
    }

    void Update()
    {
        if (transform.position.y < destroyBelowY)
        {
            if (spawner != null)
                spawner.NotifyPreviewDestroyed(gameObject);

            Destroy(gameObject);
        }
    }

    public void NotifyGrabbed()
    {
        if (hasBeenGrabbed)
            return;

        hasBeenGrabbed = true;

        if (spawner != null)
            spawner.NotifyPreviewGrabbed(gameObject);

        Destroy(this);
    }
}