using System.Collections;
using UnityEngine;
using TMPro;

public class Phase3SequenceController : MonoBehaviour
{
    [Header("Phase 3 Start")]
    [Tooltip("The main little dude who should keep wandering for a while first.")]
    public Wanderer littleDudeWanderer;

    [Tooltip("How long the little dude keeps wandering before the next phase begins.")]
    public float phase3WanderDuration = 20f;

    [Header("Player")]
    public Transform player;

    [Header("People Converge")]
    [Tooltip("How long to wait for the people to gather before triggering the ending anyway.")]
    public float peopleGatherTimeout = 12f;

    [Tooltip("Distance at which we consider a person to have reached the player.")]
    public float gatherDistance = 1.8f;

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

    [Tooltip("Delay after the people gather before showing the light and text.")]
    public float endingDelay = 1.0f;

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
        // Let the little dude keep wandering for a while
        if (littleDudeWanderer != null)
            littleDudeWanderer.ResumeWandering();

        yield return new WaitForSeconds(phase3WanderDuration);

        // Stop the little dude
        if (littleDudeWanderer != null)
            littleDudeWanderer.PauseWandering();

        // Start the people approach phase
        PeopleApproachController[] people = FindObjectsOfType<PeopleApproachController>();

        foreach (var p in people)
        {
            if (p != null && p.CompareTag("People"))
            {
                p.BeginApproach(player);
            }
        }

        if (chantingAudioSource != null && !chantingAudioSource.isPlaying)
            chantingAudioSource.Play();

        float timer = 0f;

        while (timer < peopleGatherTimeout)
        {
            if (AllPeopleReachedPlayer(people))
                break;

            timer += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(endingDelay);

        TriggerEnding();
    }

    bool AllPeopleReachedPlayer(PeopleApproachController[] people)
    {
        if (player == null)
            return false;

        bool foundAny = false;

        foreach (var p in people)
        {
            if (p == null || !p.CompareTag("People"))
                continue;

            foundAny = true;

            float dist = Vector3.Distance(
                new Vector3(p.transform.position.x, 0f, p.transform.position.z),
                new Vector3(player.position.x, 0f, player.position.z)
            );

            if (dist > gatherDistance)
                return false;
        }

        return foundAny;
    }

    void TriggerEnding()
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

        if (endingText != null)
        {
            endingText.text = finalMessage;
            endingText.gameObject.SetActive(true);
        }
    }
}