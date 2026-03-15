using UnityEngine;

public class PreviewPickupRespawn : MonoBehaviour
{
    private UIPlatformSpawner spawner;

    private float targetScale = 0.15f;
    private float destroyBelowY = -50f;
    private float popDuration = 0.18f;
    private AnimationCurve popCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private bool hasBeenGrabbed = false;
    private bool isAnimating = false;
    private float animStartTime;

    public void Initialize(
        UIPlatformSpawner owner,
        float finalScale,
        float abyssY,
        float animationDuration,
        AnimationCurve animationCurve)
    {
        spawner = owner;
        targetScale = finalScale;
        destroyBelowY = abyssY;
        popDuration = Mathf.Max(0.01f, animationDuration);
        popCurve = animationCurve != null ? animationCurve : AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        transform.localScale = Vector3.zero;
        animStartTime = Time.time;
        isAnimating = true;
    }

    void Update()
    {
        if (isAnimating)
        {
            float t = Mathf.Clamp01((Time.time - animStartTime) / popDuration);
            float s = popCurve.Evaluate(t) * targetScale;
            transform.localScale = Vector3.one * s;

            if (t >= 1f)
            {
                transform.localScale = Vector3.one * targetScale;
                isAnimating = false;
            }
        }

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