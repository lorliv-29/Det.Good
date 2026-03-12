using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.InputSystem;
using System.Collections;
using Oculus.Interaction;

public class GameStateManager : MonoBehaviour
{
    public enum GamePhase { Build, Live }
    public GamePhase currentPhase = GamePhase.Build;

    [Header("Dependencies")]
    public SandTopographyManager topographyManager;
    public GameObject physicalSandMeshObject;
    public NavMeshSurface navMeshSurface;
    public GameObject EcosystemManager;

    [Header("Phase 2 Config")]
    public Transform ovrCameraRig;
    public Transform landingPoint;
    public TunnelingEffect tunnelingEffect;
    public float transitionTime = 1.5f;
    public float miniScale = 0.02f;

    private Vector3 godViewPosition;
    private Quaternion godViewRotation;
    private Transform godViewParent;
    private bool isDiving = false;

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            if (currentPhase == GamePhase.Build)
            {
                ActivateEcosystem();
            }
        }

        if (Keyboard.current.kKey.wasPressedThisFrame && !isDiving)
        {
            StartCoroutine(SmoothDiveTransition());
        }

        if (Keyboard.current.jKey.wasPressedThisFrame)
        {
            ExitDiveMode();
        }
    }

    private IEnumerator SmoothDiveTransition()
    {
        isDiving = true;

        godViewPosition = ovrCameraRig.position;
        godViewRotation = ovrCameraRig.rotation;
        godViewParent = ovrCameraRig.parent;

        if (tunnelingEffect != null)
        {
            tunnelingEffect.enabled = true;
            tunnelingEffect.UserFOV = 360f;
        }

        float elapsed = 0;
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

        elapsed = 0;
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

        isDiving = false;
    }

    private void ExitDiveMode()
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

    private void ActivateEcosystem()
    {
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

        currentPhase = GamePhase.Live;
    }

    private void SnapBuildingsToSand()
    {
        SnapToSand[] allBuildings = Object.FindObjectsByType<SnapToSand>(FindObjectsSortMode.None);
        foreach (var b in allBuildings) b.SnapToCurrentMesh();
    }
}