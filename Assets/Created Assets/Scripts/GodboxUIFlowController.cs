using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using TMPro;

public class GodboxUIFlowController : MonoBehaviour
{
    public enum UIFlowState
    {
        IntroVideo,
        LoadingPhase1,
        Phase1,
        Phase2,
        Phase3,
        Ended
    }

    [Header("Black Transition")]
    public CanvasGroup transitionCanvasGroup;
    public float fadeToBlackDuration = 0.4f;
    public float fadeFromBlackDuration = 0.6f;

    [Header("Core References")]
    public GameStateManager gameStateManager;
    public Phase3SequenceController phase3SequenceController;
    public Transform headOrCamera;
    public SandTopographyManager sandManager;

    [Header("Intro UI")]
    public GameObject introVideoPanel;
    public VideoPlayer introVideoPlayer;
    public GameObject beginCreatingButton;
    public CanvasGroup introVideoPanelCanvasGroup;

    [Header("Transition UI")]
    public GameObject introTransitionPanel;
    public TMP_Text introTransitionText;
    public string introTransitionMessage = "Preparing your sandbox...";
    public float minimumTransitionTime = 1.5f;

    [Header("Intro Timing")]
    public float introPanelFadeDuration = 1.0f;
    public float preVideoDelay = 0.75f;
    public bool prepareVideoBeforeFade = true;

    [Header("Objects Hidden During Intro")]
    public GameObject[] hideDuringIntro;

    [Header("Objects Hidden During Phase 1")]
    public GameObject[] hideDuringPhase1;

    [Header("Objects Hidden During Phase 3")]
    public GameObject[] hideDuringPhase3;

    [Header("Problem Objects To Force Off In Phase 1")]
    public GameObject spawnPlatform;
    public GameObject phaseMenu;

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

    [Header("Debug")]
    public bool enableDebugLogs = true;

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

        SetObjectsActive(hideDuringIntro, false);
        SetObjectsActive(hideDuringPhase1, false);
        SetObjectsActive(hideDuringPhase3, true);

        if (phase1TextRoot != null) phase1TextRoot.SetActive(false);
        if (phase2TextRoot != null) phase2TextRoot.SetActive(false);
        if (phase3TextRoot != null) phase3TextRoot.SetActive(false);

        if (goToPhase2Button != null) goToPhase2Button.SetActive(false);
        if (goToPhase3Button != null) goToPhase3Button.SetActive(false);

        if (creationCatalogue != null) creationCatalogue.SetActive(false);

        if (endingTextObject != null) endingTextObject.SetActive(false);
        if (whiteLightObject != null) whiteLightObject.SetActive(false);

        if (introTransitionPanel != null) introTransitionPanel.SetActive(false);

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
        if (introVideoPlayer != null && prepareVideoBeforeFade)
        {
            introVideoPlayer.Prepare();
            while (!introVideoPlayer.isPrepared)
            {
                yield return null;
            }
        }

        if (introVideoPanel != null)
            introVideoPanel.SetActive(true);

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
            introVideoPanelCanvasGroup.interactable = true;
            introVideoPanelCanvasGroup.blocksRaycasts = true;
        }

        if (preVideoDelay > 0f)
            yield return new WaitForSecondsRealtime(preVideoDelay);

        if (introVideoPlayer != null)
            introVideoPlayer.Play();

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

        StartCoroutine(BeginCreatingTransition());
    }

    IEnumerator BeginCreatingTransition()
    {
        currentState = UIFlowState.LoadingPhase1;

        if (gameStateManager != null)
            gameStateManager.EnterLoadingScreen();

        if (introSequenceCoroutine != null)
        {
            StopCoroutine(introSequenceCoroutine);
            introSequenceCoroutine = null;
        }

        if (introVideoPlayer != null)
            introVideoPlayer.Stop();

        if (beginCreatingButton != null)
            beginCreatingButton.SetActive(false);

        if (transitionCanvasGroup != null)
        {
            transitionCanvasGroup.gameObject.SetActive(true);
            transitionCanvasGroup.alpha = 1f;
            transitionCanvasGroup.blocksRaycasts = true;
            transitionCanvasGroup.interactable = true;
        }

        yield return null;
        yield return new WaitForEndOfFrame();

        if (introVideoPanel != null)
            introVideoPanel.SetActive(false);

        if (introTransitionPanel != null)
            introTransitionPanel.SetActive(false);

        float startTime = Time.realtimeSinceStartup;

        SetObjectsActive(hideDuringIntro, true);
        yield return null;

        if (gameStateManager != null)
            gameStateManager.EnterPhase1();
        yield return null;

        currentState = UIFlowState.Phase1;
        ShowPhase1();
        yield return null;

        while (Time.realtimeSinceStartup - startTime < minimumTransitionTime)
        {
            yield return null;
        }

        yield return StartCoroutine(FadeCanvasGroup(transitionCanvasGroup, 1f, 0f, fadeFromBlackDuration));

        if (transitionCanvasGroup != null)
        {
            transitionCanvasGroup.blocksRaycasts = false;
            transitionCanvasGroup.interactable = false;
        }
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null)
            yield break;

        float elapsed = 0f;
        cg.alpha = from;
        cg.blocksRaycasts = true;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            cg.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        cg.alpha = to;
        cg.blocksRaycasts = (to > 0.9f);
    }

    void ShowPhase1()
    {
        if (enableDebugLogs) Debug.Log("=== ShowPhase1 START ===");

        if (phase2TextRoot != null) phase2TextRoot.SetActive(false);
        if (phase3TextRoot != null) phase3TextRoot.SetActive(false);

        if (creationCatalogue != null) creationCatalogue.SetActive(false);
        if (goToPhase3Button != null) goToPhase3Button.SetActive(false);

        SetObjectsActive(hideDuringPhase1, false);
        SetObjectsActive(hideDuringPhase3, true);

        // Force problem objects off again after all other phase setup.
        if (spawnPlatform != null)
        {
            spawnPlatform.SetActive(false);
            LogObjectState("Forced OFF spawnPlatform in Phase1", spawnPlatform);
        }

        if (phaseMenu != null)
        {
            phaseMenu.SetActive(false);
            LogObjectState("Forced OFF phaseMenu in Phase1", phaseMenu);
        }

        if (phase1TextRoot != null)
        {
            PlaceTextInFrontOfPlayer(phase1TextRoot);
            phase1TextRoot.SetActive(true);
            StartCoroutine(AnimateTextBackwards(phase1TextRoot.transform));
        }

        if (goToPhase2Button != null)
            goToPhase2Button.SetActive(true);

        StartCoroutine(CheckProblemObjects());

        if (enableDebugLogs) Debug.Log("=== ShowPhase1 END ===");
    }

    public void GoToPhase2()
    {
        if (currentState != UIFlowState.Phase1)
            return;

        currentState = UIFlowState.Phase2;

        if (phase1TextRoot != null) phase1TextRoot.SetActive(false);
        if (goToPhase2Button != null) goToPhase2Button.SetActive(false);

        SetObjectsActive(hideDuringPhase1, true);
        SetObjectsActive(hideDuringPhase3, true);

        if (gameStateManager != null)
            gameStateManager.EnterPhase2();

        // TRIGGER THE SAND BAKE 
        if (sandManager != null) sandManager.ForceBakeMesh();

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

        SetObjectsActive(hideDuringPhase3, false);

        if (phase3TextRoot != null)
        {
            PlaceTextInFrontOfPlayer(phase3TextRoot);
            phase3TextRoot.SetActive(true);
            StartCoroutine(AnimateTextBackwards(phase3TextRoot.transform));
        }

        if (gameStateManager != null)
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
            if (obj == null) continue;

            obj.SetActive(state);
            LogObjectState("SetObjectsActive", obj);
        }
    }

    void LogObjectState(string label, GameObject obj)
    {
        if (!enableDebugLogs) return;

        if (obj == null)
        {
            Debug.Log(label + " -> NULL");
            return;
        }

        Debug.Log(
            label + " -> " + obj.name +
            " | activeSelf=" + obj.activeSelf +
            " | activeInHierarchy=" + obj.activeInHierarchy
        );
    }

    IEnumerator CheckProblemObjects()
    {
        yield return null;

        if (spawnPlatform != null)
            LogObjectState("1 frame later spawnPlatform", spawnPlatform);

        if (phaseMenu != null)
            LogObjectState("1 frame later phaseMenu", phaseMenu);

        yield return new WaitForSeconds(0.2f);

        if (spawnPlatform != null)
            LogObjectState("0.2s later spawnPlatform", spawnPlatform);

        if (phaseMenu != null)
            LogObjectState("0.2s later phaseMenu", phaseMenu);
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