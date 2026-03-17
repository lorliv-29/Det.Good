using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class GodboxUIFlowController : MonoBehaviour
{
    public enum UIFlowState
    {
        IntroVideo,
        Phase1,
        Phase2,
        Phase3,
        Ended
    }

    [Header("Core References")]
    public GameStateManager gameStateManager;
    public Phase3SequenceController phase3SequenceController;
    public Transform headOrCamera;

    [Header("Intro UI")]
    public GameObject introVideoPanel;
    public VideoPlayer introVideoPlayer;
    public GameObject beginCreatingButton;
    public CanvasGroup introVideoPanelCanvasGroup;

    [Header("Intro Timing")]
    [Tooltip("How long the video panel takes to fade in.")]
    public float introPanelFadeDuration = 1.0f;

    [Tooltip("How long to wait after the panel fades in before starting the video.")]
    public float preVideoDelay = 0.75f;

    [Tooltip("If true, prepares the video before the panel fade starts to avoid a rogue first frame.")]
    public bool prepareVideoBeforeFade = true;

    [Header("Objects Hidden During Intro")]
    public GameObject[] hideDuringIntro;

    [Header("Phase Buttons")]
    public GameObject goToPhase2Button;
    public GameObject goToPhase3Button;

    [Header("Phase Text Roots")]
    public GameObject phase1TextRoot;
    public GameObject phase2TextRoot;
    public GameObject phase3TextRoot;

    [Header("Creation UI")]
    public GameObject creationCatalogue;

    [Header("Optional Ending Visuals")]
    public GameObject endingTextObject;
    public GameObject whiteLightObject;

    [Header("Text Animation")]
    public float textSpawnDistance = 0.8f;
    public float textMoveDistance = 2f;
    public float textMoveDuration = 2.5f;

    public UIFlowState currentState = UIFlowState.IntroVideo;

    private Coroutine introSequenceCoroutine;

    void Start()
    {
        EnterIntroState();
    }

    void OnDestroy()
    {
        if (introVideoPlayer != null)
        {
            introVideoPlayer.loopPointReached -= OnIntroVideoFinished;
        }
    }

    void EnterIntroState()
    {
        currentState = UIFlowState.IntroVideo;

        // Hide all gameplay objects
        SetObjectsActive(hideDuringIntro, false);

        if (phase1TextRoot != null) phase1TextRoot.SetActive(false);
        if (phase2TextRoot != null) phase2TextRoot.SetActive(false);
        if (phase3TextRoot != null) phase3TextRoot.SetActive(false);

        if (goToPhase2Button != null) goToPhase2Button.SetActive(false);
        if (goToPhase3Button != null) goToPhase3Button.SetActive(false);

        if (creationCatalogue != null) creationCatalogue.SetActive(false);

        if (endingTextObject != null) endingTextObject.SetActive(false);
        if (whiteLightObject != null) whiteLightObject.SetActive(false);

        // Show only intro panel
        if (introVideoPanel != null) introVideoPanel.SetActive(true);
        if (beginCreatingButton != null) beginCreatingButton.SetActive(false);

        if (introVideoPlayer != null)
        {
            introVideoPlayer.loopPointReached -= OnIntroVideoFinished;
            introVideoPlayer.loopPointReached += OnIntroVideoFinished;
            introVideoPlayer.playOnAwake = false;
            introVideoPlayer.Stop();
        }

        if (introVideoPanelCanvasGroup != null)
        {
            introVideoPanelCanvasGroup.alpha = 0f;
            introVideoPanelCanvasGroup.interactable = false;
            introVideoPanelCanvasGroup.blocksRaycasts = false;
        }

        if (introSequenceCoroutine != null)
        {
            StopCoroutine(introSequenceCoroutine);
        }

        introSequenceCoroutine = StartCoroutine(IntroSequence());
    }

    IEnumerator IntroSequence()
    {
        // Optional video prepare so the first frame doesn't flash badly
        if (introVideoPlayer != null && prepareVideoBeforeFade)
        {
            introVideoPlayer.Prepare();

            while (!introVideoPlayer.isPrepared)
            {
                yield return null;
            }
        }

        // Make sure panel is active before fading
        if (introVideoPanel != null)
            introVideoPanel.SetActive(true);

        // Fade in the panel
        if (introVideoPanelCanvasGroup != null)
        {
            float elapsed = 0f;

            while (elapsed < introPanelFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / introPanelFadeDuration);
                introVideoPanelCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }

            introVideoPanelCanvasGroup.alpha = 1f;
        }

        // Small pause so player can orient themselves
        if (preVideoDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(preVideoDelay);
        }

        // Start video only after panel is visible
        if (introVideoPlayer != null)
        {
            introVideoPlayer.Play();
        }

        introSequenceCoroutine = null;
    }

    void OnIntroVideoFinished(VideoPlayer vp)
    {
        if (beginCreatingButton != null)
            beginCreatingButton.SetActive(true);
    }

    public void BeginCreating()
    {
        if (currentState != UIFlowState.IntroVideo)
            return;

        currentState = UIFlowState.Phase1;

        if (introSequenceCoroutine != null)
        {
            StopCoroutine(introSequenceCoroutine);
            introSequenceCoroutine = null;
        }

        if (introVideoPlayer != null)
        {
            introVideoPlayer.Stop();
        }

        if (introVideoPanel != null) introVideoPanel.SetActive(false);
        if (beginCreatingButton != null) beginCreatingButton.SetActive(false);

        gameStateManager.EnterPhase1();

        ShowPhase1();
    }

    void ShowPhase1()
    {
        if (phase2TextRoot != null) phase2TextRoot.SetActive(false);
        if (phase3TextRoot != null) phase3TextRoot.SetActive(false);

        if (creationCatalogue != null) creationCatalogue.SetActive(false);
        if (goToPhase3Button != null) goToPhase3Button.SetActive(false);

        // Reveal all intro-hidden scene objects now
        SetObjectsActive(hideDuringIntro, true);

        if (phase1TextRoot != null)
        {
            PlaceTextInFrontOfPlayer(phase1TextRoot);
            phase1TextRoot.SetActive(true);
            StartCoroutine(AnimateTextBackwards(phase1TextRoot.transform));
        }

        if (goToPhase2Button != null)
            goToPhase2Button.SetActive(true);
    }

    public void GoToPhase2()
    {
        if (currentState != UIFlowState.Phase1)
            return;

        currentState = UIFlowState.Phase2;

        if (phase1TextRoot != null) phase1TextRoot.SetActive(false);
        if (goToPhase2Button != null) goToPhase2Button.SetActive(false);

        gameStateManager.EnterPhase2();

        if (phase2TextRoot != null)
        {
            PlaceTextInFrontOfPlayer(phase2TextRoot);
            phase2TextRoot.SetActive(true);
            StartCoroutine(AnimateTextBackwards(phase2TextRoot.transform));
        }

        if (creationCatalogue != null)
            creationCatalogue.SetActive(true);

        if (goToPhase3Button != null)
            goToPhase3Button.SetActive(true);
    }

    public void GoToPhase3()
    {
        if (currentState != UIFlowState.Phase2)
            return;

        currentState = UIFlowState.Phase3;

        if (phase2TextRoot != null) phase2TextRoot.SetActive(false);
        if (goToPhase3Button != null) goToPhase3Button.SetActive(false);
        if (creationCatalogue != null) creationCatalogue.SetActive(false);

        if (phase3TextRoot != null)
        {
            PlaceTextInFrontOfPlayer(phase3TextRoot);
            phase3TextRoot.SetActive(true);
            StartCoroutine(AnimateTextBackwards(phase3TextRoot.transform));
        }

        gameStateManager.EnterPhase3();
        StartCoroutine(BeginPhase3SequenceAfterDelay(1.8f));
    }

    IEnumerator BeginPhase3SequenceAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (phase3SequenceController != null)
            phase3SequenceController.StartPhase3();
    }

    void SetObjectsActive(GameObject[] objects, bool state)
    {
        if (objects == null) return;

        foreach (GameObject obj in objects)
        {
            if (obj != null)
                obj.SetActive(state);
        }
    }

    void PlaceTextInFrontOfPlayer(GameObject textRoot)
    {
        if (headOrCamera == null || textRoot == null)
            return;

        Vector3 forward = headOrCamera.forward;
        forward.y = 0f;
        forward.Normalize();

        textRoot.transform.position = headOrCamera.position + forward * textSpawnDistance;

        Vector3 lookTarget = new Vector3(
            headOrCamera.position.x,
            textRoot.transform.position.y,
            headOrCamera.position.z
        );

        textRoot.transform.LookAt(lookTarget);
        textRoot.transform.Rotate(0f, 180f, 0f);
    }

    IEnumerator AnimateTextBackwards(Transform target)
    {
        if (target == null || headOrCamera == null)
            yield break;

        Vector3 startPos = target.position;

        Vector3 forward = headOrCamera.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 endPos = startPos + forward * textMoveDistance;

        float elapsed = 0f;

        while (elapsed < textMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / textMoveDuration;
            target.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        target.position = endPos;
    }
}