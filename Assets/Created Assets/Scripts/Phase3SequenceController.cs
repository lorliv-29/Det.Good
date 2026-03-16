using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Phase3SequenceController : MonoBehaviour
{
    [Header("Phase 3 Wander")]
    [Tooltip("How long all wanderers keep wandering before convergence begins.")]
    public float phase3WanderDuration = 20f;

    [Header("Player")]
    public Transform player;

    [Header("Approach Phase")]
    [Tooltip("How long the people chant and approach before the final text appears.")]
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

    [Header("Ending Text")]
    public TMP_Text endingText;

    [TextArea(2, 4)]
    public string finalMessage = "The world you shaped now looks back at you. Your people have chosen their God.";

    [Header("Optional Filtering")]
    [Tooltip("If true, only objects tagged People will be paused as wanderers.")]
    public bool onlyAffectPeopleTag = true;

    bool sequenceStarted;

    public void StartPhase3()
    {
        if (sequenceStarted)
            return;

        sequenceStarted = true;
        StartCoroutine(Phase3Routine());
    }

    IEnumerator Phase3Routine()
    {
        // Find all current wanderers
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

        // 1) Wander for 20 seconds
        yield return new WaitForSeconds(phase3WanderDuration);

        // Re-scan in case more people spawned during the wait
        Wanderer[] allWanderersNow = FindObjectsOfType<Wanderer>();

        foreach (Wanderer w in allWanderersNow)
        {
            if (w == null)
                continue;

            if (onlyAffectPeopleTag && !w.CompareTag("People"))
                continue;

            w.PauseWandering();
        }

        // 2) Start people approaching
        PeopleApproachController[] people = FindObjectsOfType<PeopleApproachController>();

        foreach (PeopleApproachController p in people)
        {
            if (p != null && p.CompareTag("People"))
            {
                p.BeginApproach(player);
            }
        }

        // 3) Start chanting
        if (chantingAudioSource != null && !chantingAudioSource.isPlaying)
            chantingAudioSource.Play();

        // 4) Show bright light immediately during the approach phase
        ShowWhiteLight();

        // 5) Let this phase run for 10 seconds
        yield return new WaitForSeconds(approachPhaseDuration);

        // 6) Then show the final text
        ShowEndingText();
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

    void ShowEndingText()
    {
        if (endingText != null)
        {
            endingText.text = finalMessage;
            endingText.gameObject.SetActive(true);
        }
    }
}