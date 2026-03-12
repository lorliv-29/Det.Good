using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.InputSystem;

public class GameStateManager : MonoBehaviour
{
    public enum GamePhase { Build, Live }
    public GamePhase currentPhase = GamePhase.Build;

    [Header("Dependencies")]
    public SandTopographyManager topographyManager;
    public GameObject physicalSandMeshObject;
    public NavMeshSurface navMeshSurface;   // <-- assign the one on NavMesh Proxy
    public GameObject EcosystemManager;

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            if (currentPhase == GamePhase.Build)
            {
                Debug.Log("T Pressed: Awakening the Ecosystem...");
                ActivateEcosystem();
            }
        }
    }

    private void ActivateEcosystem()
    {
        // 1. Snap buildings first so they affect the bake
        SnapBuildingsToSand();

        // 2. Bake the NavMesh on the proxy surface
        if (navMeshSurface != null)
        {
            Debug.Log("T pressed -> baking NavMesh on: " + navMeshSurface.gameObject.name);
            navMeshSurface.BuildNavMesh();
            var triangulation = UnityEngine.AI.NavMesh.CalculateTriangulation();
            Debug.Log("NavMesh triangulation vertices: " + triangulation.vertices.Length);
            Debug.Log("NavMesh triangulation triangles: " + (triangulation.indices.Length / 3));
        }
        else
        {
            Debug.LogWarning("NavMeshSurface reference is missing on GameStateManager.");
        }

        // 3. Wake up the spawners
        if (EcosystemManager != null)
        {
            MonoBehaviour[] allSpawners = EcosystemManager.GetComponents<MonoBehaviour>();
            foreach (var s in allSpawners) s.enabled = true;
        }

        currentPhase = GamePhase.Live;
        Debug.Log("The World is now Alive!");
    }

    private void SnapBuildingsToSand()
    {
        SnapToSand[] allBuildings = Object.FindObjectsByType<SnapToSand>(FindObjectsSortMode.None);
        foreach (var b in allBuildings) b.SnapToCurrentMesh();
    }
}