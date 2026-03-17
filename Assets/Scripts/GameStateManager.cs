using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.InputSystem;
using System.Collections;
using Oculus.Interaction;

public class GameStateManager : MonoBehaviour
{
    public enum GamePhase
    {
        Intro,
        LoadingScreen,
        Phase1_Shape,
        Phase2_Create,
        Phase3_Observe
    }

    public GamePhase currentPhase = GamePhase.Intro;

    [Header("Dependencies")]
    public SandTopographyManager topographyManager;
    public GameObject physicalSandMeshObject;
    public NavMeshSurface navMeshSurface;
    public GameObject EcosystemManager;

    [Header("Spectator / Phase 3")]
    public Transform ovrCameraRig;
    public Transform landingPoint;
    public TunnelingEffect tunnelingEffect;
    public float transitionTime = 1.5f;
    public float miniScale = 0.005f;

    [Header("Phase 3 Crowd")]
    public Transform phase3ApproachTarget;
    public float phase3WanderBeforeFacing = 6f;
    public float phase3FaceStagger = 0.35f;
    public float phase3ApproachDelayAfterFacing = 6f;

    private Vector3 godViewPosition;
    private Quaternion godViewRotation;
    private Transform godViewParent;
    private bool isTransitioning = false;

    void Update()
    {
        // DEV SHORTCUTS ONLY
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            EnterPhase2();
        }

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            ReturnToPhase1Dev();
        }

        if (Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame)
        {
            EnterPhase3();
        }
    }

    public void EnterLoadingScreen()
    {
        currentPhase = GamePhase.LoadingScreen;
    }

    public void EnterPhase1()
    {
        currentPhase = GamePhase.Phase1_Shape;
    }

    public void EnterPhase2()
    {
        if (currentPhase == GamePhase.Phase2_Create || currentPhase == GamePhase.Phase3_Observe)
            return;

        SnapBuildingsToSand();

        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();
        }

        if (EcosystemManager != null)
        {
            MonoBehaviour[] allSpawners = EcosystemManager.GetComponents<MonoBehaviour>();
            foreach (var s in allSpawners) s.enabled = true;
        }

        currentPhase = GamePhase.Phase2_Create;
    }

    public void EnterPhase3()
    {
        if (currentPhase != GamePhase.Phase2_Create || isTransitioning)
            return;

        currentPhase = GamePhase.Phase3_Observe;
        StartCoroutine(SmoothDiveTransition());
    }

    public void ReturnToPhase1Dev()
    {
        currentPhase = GamePhase.Phase1_Shape;
        ExitDiveMode();
    }

    private IEnumerator SmoothDiveTransition()
    {
        isTransitioning = true;

        godViewPosition = ovrCameraRig.position;
        godViewRotation = ovrCameraRig.rotation;
        godViewParent = ovrCameraRig.parent;

        if (tunnelingEffect != null)
        {
            tunnelingEffect.enabled = true;
            tunnelingEffect.UserFOV = 360f;
        }

        float elapsed = 0f;
        while (elapsed < transitionTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionTime;

            if (tunnelingEffect != null)
            {
                tunnelingEffect.UserFOV = Mathf.Lerp(360f, 40f, t);
            }

            ovrCameraRig.position = Vector3.Lerp(godViewPosition, landingPoint.position, t);
            ovrCameraRig.localScale = Vector3.Lerp(Vector3.one, new Vector3(miniScale, miniScale, miniScale), t);

            yield return null;
        }

        ovrCameraRig.SetParent(landingPoint.parent);
        ovrCameraRig.localPosition = landingPoint.localPosition + new Vector3(0, 0.032f, 0);
        ovrCameraRig.localRotation = landingPoint.localRotation;

        elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;

            if (tunnelingEffect != null)
            {
                tunnelingEffect.UserFOV = Mathf.Lerp(40f, 360f, elapsed / 0.5f);
            }

            yield return null;
        }

        if (tunnelingEffect != null)
        {
            tunnelingEffect.enabled = false;
        }

        DisablePeopleInteractionsForPhase3();
        StartCoroutine(Phase3CrowdSequence());

        isTransitioning = false;
    }

    IEnumerator Phase3CrowdSequence()
    {
        if (phase3ApproachTarget == null)
        {
            Debug.LogWarning("Phase 3 Approach Target is not assigned.");
            yield break;
        }

        yield return new WaitForSeconds(phase3WanderBeforeFacing);

        GameObject[] people = GameObject.FindGameObjectsWithTag("People");

        foreach (GameObject person in people)
        {
            if (person == null) continue;

            Wanderer wanderer = person.GetComponent<Wanderer>();
            if (wanderer != null)
            {
                wanderer.FreezeAndFaceTarget(phase3ApproachTarget);
            }

            yield return new WaitForSeconds(phase3FaceStagger);
        }

        yield return new WaitForSeconds(phase3ApproachDelayAfterFacing);

        TriggerPeopleApproach();
    }

    void DisablePeopleInteractionsForPhase3()
    {
        GameObject[] people = GameObject.FindGameObjectsWithTag("People");

        foreach (GameObject person in people)
        {
            if (person == null) continue;

            PersonInteraction interaction = person.GetComponent<PersonInteraction>();
            if (interaction != null)
            {
                interaction.DisableInteractionsForPhase3();
            }
        }
    }

    public void TriggerPeopleApproach()
    {
        if (phase3ApproachTarget == null)
        {
            Debug.LogWarning("Phase 3 Approach Target is missing.");
            return;
        }

        GameObject[] people = GameObject.FindGameObjectsWithTag("People");

        foreach (GameObject person in people)
        {
            if (person == null) continue;

            Wanderer wanderer = person.GetComponent<Wanderer>();
            if (wanderer != null)
            {
                wanderer.StartApproachingTarget(phase3ApproachTarget);
            }
        }
    }

    public void ExitDiveMode()
    {
        ovrCameraRig.SetParent(godViewParent);
        ovrCameraRig.localScale = Vector3.one;
        ovrCameraRig.position = godViewPosition;
        ovrCameraRig.rotation = godViewRotation;

        if (tunnelingEffect != null)
        {
            tunnelingEffect.enabled = false;
            tunnelingEffect.UserFOV = 360f;
        }
    }

    private void SnapBuildingsToSand()
    {
        SnapToSand[] allBuildings = Object.FindObjectsByType<SnapToSand>(FindObjectsSortMode.None);
        foreach (var b in allBuildings)
        {
            b.SnapToCurrentMesh();
        }
    }
}