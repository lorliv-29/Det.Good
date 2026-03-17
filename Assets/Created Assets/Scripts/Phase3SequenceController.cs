using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Phase3SequenceController : MonoBehaviour
{
    [Header("Phase 3 Wander")]
    [Tooltip("How long all wanderers keep wandering before convergence begins.")]
    public float phase3WanderDuration = 20f;

    [Header("Player")]
    public Transform player;

    [Header("Approach Phase")]
    [Tooltip("How long the people chant and approach before the final object appears.")]
    public float approachPhaseDuration = 10f;

    [Header("Audio")]
    [Tooltip("Optional chanting audio source. Will play when the people begin approaching.")]
    public AudioSource chantingAudioSource;

    [Header("Ending Visuals")]
    [Tooltip("White light particle system to spawn/show in front of the player.")]
    public ParticleSystem whiteLightParticle;

    [Tooltip("How far in front of the player the light should appear.")]
    public float lightDistanceInFrontOfPlayer = 1.2f;

    [Tooltip("Optional vertical offset for the light.")]
    public float lightHeightOffset = 0.15f;

    [Header("Ending Object")]
    [Tooltip("Prefab to spawn as the final ending object.")]
    public GameObject endingObjectPrefab;

    [Tooltip("How far in front of the player the ending object appears.")]
    public float endingObjectDistance = 1.2f;

    [Tooltip("Vertical offset for the ending object.")]
    public float endingObjectHeightOffset = 0.15f;

    [Tooltip("Final local scale multiplier for the ending object.")]
    public float endingObjectScale = 1f;

    [Tooltip("How long the object takes to grow in.")]
    public float endingObjectGrowDuration = 1.5f;

    [Tooltip("If true, the ending object will slowly rotate forever.")]
    public bool rotateEndingObject = true;

    [Tooltip("Rotation speed in degrees per second.")]
    public float endingObjectRotationSpeed = 25f;

    [Tooltip("If true, try to fade in renderers by material color alpha.")]
    public bool fadeInEndingObject = false;

    [Tooltip("How long fade-in should take.")]
    public float endingObjectFadeDuration = 1.2f;

    [Header("Optional Filtering")]
    [Tooltip("If true, only objects tagged People will be paused as wanderers.")]
    public bool onlyAffectPeopleTag = true;

    bool sequenceStarted;
    GameObject spawnedEndingObject;
    Coroutine endingObjectRoutine;

    public void StartPhase3()
    {
        if (sequenceStarted)
            return;

        sequenceStarted = true;
        StartCoroutine(Phase3Routine());
    }

    IEnumerator Phase3Routine()
    {
        Wanderer[] wanderers = FindObjectsOfType<Wanderer>();
        List<Wanderer> validWanderers = new List<Wanderer>();

        foreach (Wanderer w in wanderers)
        {
            if (w == null)
                continue;

            if (onlyAffectPeopleTag && !w.CompareTag("People"))
                continue;

            validWanderers.Add(w);
            w.ResumeWandering();
        }

        // 1) Wander for a while
        yield return new WaitForSeconds(phase3WanderDuration);

        // 2) Pause wanderers
        Wanderer[] allWanderersNow = FindObjectsOfType<Wanderer>();

        foreach (Wanderer w in allWanderersNow)
        {
            if (w == null)
                continue;

            if (onlyAffectPeopleTag && !w.CompareTag("People"))
                continue;

            w.PauseWandering();
        }

        // 3) Start people approaching
        PeopleApproachController[] people = FindObjectsOfType<PeopleApproachController>();

        foreach (PeopleApproachController p in people)
        {
            if (p != null && p.CompareTag("People"))
            {
                p.BeginApproach(player);
            }
        }

        // 4) Start chanting
        if (chantingAudioSource != null && !chantingAudioSource.isPlaying)
            chantingAudioSource.Play();

        // 5) Show bright light immediately during the approach phase
        ShowWhiteLight();

        // 6) Let approach phase play out
        yield return new WaitForSeconds(approachPhaseDuration);

        // 7) Reveal ending object
        ShowEndingObject();
    }

    void ShowWhiteLight()
    {
        if (whiteLightParticle != null && player != null)
        {
            Vector3 forward = player.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;

            forward.Normalize();

            Vector3 lightPos =
                player.position +
                forward * lightDistanceInFrontOfPlayer +
                Vector3.up * lightHeightOffset;

            whiteLightParticle.transform.position = lightPos;
            whiteLightParticle.gameObject.SetActive(true);
            whiteLightParticle.Play();
        }
    }

    void ShowEndingObject()
    {
        if (endingObjectPrefab == null || player == null)
        {
            Debug.LogWarning("Ending object prefab or player is missing.");
            return;
        }

        Vector3 forward = player.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        forward.Normalize();

        Vector3 spawnPos =
            player.position +
            forward * endingObjectDistance +
            Vector3.up * endingObjectHeightOffset;

        Quaternion spawnRot = Quaternion.LookRotation(-forward, Vector3.up);

        if (spawnedEndingObject == null)
        {
            spawnedEndingObject = Instantiate(endingObjectPrefab, spawnPos, spawnRot);
        }
        else
        {
            spawnedEndingObject.transform.position = spawnPos;
            spawnedEndingObject.transform.rotation = spawnRot;
            spawnedEndingObject.SetActive(true);
        }

        if (endingObjectRoutine != null)
            StopCoroutine(endingObjectRoutine);

        endingObjectRoutine = StartCoroutine(AnimateEndingObjectReveal(spawnedEndingObject));
    }

    IEnumerator AnimateEndingObjectReveal(GameObject obj)
    {
        if (obj == null)
            yield break;

        Transform t = obj.transform;
        Vector3 finalScale = Vector3.one * endingObjectScale;
        t.localScale = Vector3.zero;

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
        List<Material> fadeMaterials = new List<Material>();
        List<Color> originalColors = new List<Color>();

        if (fadeInEndingObject)
        {
            foreach (Renderer r in renderers)
            {
                if (r == null) continue;

                foreach (Material m in r.materials)
                {
                    if (m != null && m.HasProperty("_Color"))
                    {
                        Color c = m.color;
                        originalColors.Add(c);
                        c.a = 0f;
                        m.color = c;
                        fadeMaterials.Add(m);
                    }
                }
            }
        }

        float elapsed = 0f;
        float growDuration = Mathf.Max(0.01f, endingObjectGrowDuration);
        float fadeDuration = Mathf.Max(0.01f, endingObjectFadeDuration);

        while (elapsed < growDuration)
        {
            elapsed += Time.deltaTime;
            float tNorm = Mathf.Clamp01(elapsed / growDuration);

            float eased = Mathf.SmoothStep(0f, 1f, tNorm);
            t.localScale = Vector3.LerpUnclamped(Vector3.zero, finalScale, eased);

            if (fadeInEndingObject && fadeMaterials.Count > 0)
            {
                float fadeT = Mathf.Clamp01(elapsed / fadeDuration);

                for (int i = 0; i < fadeMaterials.Count; i++)
                {
                    Material m = fadeMaterials[i];
                    Color baseColor = originalColors[i];
                    Color c = baseColor;
                    c.a = Mathf.Lerp(0f, baseColor.a, fadeT);
                    m.color = c;
                }
            }

            if (rotateEndingObject)
            {
                t.Rotate(Vector3.up, endingObjectRotationSpeed * Time.deltaTime, Space.World);
            }

            yield return null;
        }

        t.localScale = finalScale;

        if (fadeInEndingObject && fadeMaterials.Count > 0)
        {
            for (int i = 0; i < fadeMaterials.Count; i++)
            {
                Material m = fadeMaterials[i];
                Color baseColor = originalColors[i];
                m.color = baseColor;
            }
        }

        while (rotateEndingObject && obj != null && obj.activeInHierarchy)
        {
            t.Rotate(Vector3.up, endingObjectRotationSpeed * Time.deltaTime, Space.World);
            yield return null;
        }
    }
}