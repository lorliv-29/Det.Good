using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.AI.Navigation;
using UnityEngine.InputSystem;

public class SandMeshBuilder : MonoBehaviour
{
    [Header("Physical Clipping")]
    [Tooltip("The mesh will ONLY build inside this Box Collider. Leave empty to build everywhere.")]
    public BoxCollider allowedSandArea;

    [Header("NavMesh Proxy")]
    public MeshFilter navMeshProxyFilter;
    public MeshCollider navMeshProxyCollider;
    public bool buildNavMeshProxy = true;

    [Range(0f, 1f)]
    public float navMeshHeightScale = 1.0f;

    [Header("Automated AI Baking")]
    public NavMeshSurface navMeshSurface;

    [Header("Assign in Inspector")]
    public MeshFilter meshFilter;
    public MeshCollider meshCollider;
    public Transform referenceTransform;

    [Header("Grid Dimensions")]
    public int gridWidth = 160;
    public int gridHeight = 120;

    [Header("Mesh Cleanup")]
    public float invalidYThreshold = -500f;
    public float maxEdge = 0.1f;

    void Update()
    {
        // THE MANUAL OVERRIDE: Press T to force a bake anytime!
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            Debug.Log("Manual Override: T key pressed. Forcing NavMesh Bake!");
            ForceBakeNavMesh();
        }
    }

    public void GeneratePhysicalMesh(ComputeBuffer pointBuffer)
    {
        AsyncGPUReadback.Request(pointBuffer, request =>
        {
            if (request.hasError)
            {
                Debug.LogError("GPU Readback failed! The buffer might be invalid.");
                return;
            }

            var gpuData = request.GetData<Vector3>();
            Vector3[] worldVertices = gpuData.ToArray();

            List<int> triangles = new List<int>();

            for (int y = 0; y < gridHeight - 1; y++)
            {
                for (int x = 0; x < gridWidth - 1; x++)
                {
                    int i = y * gridWidth + x;
                    int iRight = i + 1;
                    int iTop = i + gridWidth;
                    int iTopRight = i + gridWidth + 1;

                    // 1. Skip if depth data is invalid
                    if (worldVertices[i].y < invalidYThreshold ||
                        worldVertices[iRight].y < invalidYThreshold ||
                        worldVertices[iTop].y < invalidYThreshold ||
                        worldVertices[iTopRight].y < invalidYThreshold)
                    {
                        continue;
                    }

                    // 2. THE PHYSICAL CLIP: Skip if the points are outside our allowed bounding box
                    if (allowedSandArea != null)
                    {
                        if (!allowedSandArea.bounds.Contains(worldVertices[i]) ||
                            !allowedSandArea.bounds.Contains(worldVertices[iRight]) ||
                            !allowedSandArea.bounds.Contains(worldVertices[iTop]) ||
                            !allowedSandArea.bounds.Contains(worldVertices[iTopRight]))
                        {
                            continue;
                        }
                    }

                    // 3. Build triangles if the points are close enough together
                    if (Vector3.Distance(worldVertices[i], worldVertices[iRight]) < maxEdge &&
                        Vector3.Distance(worldVertices[i], worldVertices[iTop]) < maxEdge)
                    {
                        triangles.Add(i);
                        triangles.Add(iTop);
                        triangles.Add(iRight);
                    }

                    if (Vector3.Distance(worldVertices[iRight], worldVertices[iTop]) < maxEdge &&
                        Vector3.Distance(worldVertices[iTop], worldVertices[iTopRight]) < maxEdge)
                    {
                        triangles.Add(iRight);
                        triangles.Add(iTop);
                        triangles.Add(iTopRight);
                    }
                }
            }

            Transform t = referenceTransform != null ? referenceTransform : transform;
            Dictionary<int, int> oldToNew = new Dictionary<int, int>();
            List<Vector3> compactVertices = new List<Vector3>();
            List<int> compactTriangles = new List<int>();

            for (int tri = 0; tri < triangles.Count; tri++)
            {
                int oldIndex = triangles[tri];

                if (!oldToNew.TryGetValue(oldIndex, out int newIndex))
                {
                    newIndex = compactVertices.Count;
                    oldToNew[oldIndex] = newIndex;

                    Vector3 localVertex = t.InverseTransformPoint(worldVertices[oldIndex]);
                    compactVertices.Add(localVertex);
                }

                compactTriangles.Add(newIndex);
            }

            Mesh solidMesh = new Mesh();
            solidMesh.indexFormat = IndexFormat.UInt32;
            solidMesh.vertices = compactVertices.ToArray();
            solidMesh.triangles = compactTriangles.ToArray();
            solidMesh.RecalculateNormals();
            solidMesh.RecalculateBounds();

            if (meshFilter != null)
                meshFilter.mesh = solidMesh;

            if (meshCollider != null)
            {
                meshCollider.sharedMesh = null;
                meshCollider.sharedMesh = solidMesh;
            }

            Debug.Log($"Physical Sand Mesh Generated Successfully! Vertices={compactVertices.Count}");

            if (buildNavMeshProxy && navMeshProxyFilter != null && navMeshProxyCollider != null)
            {
                Debug.Log("Building NavMesh proxy mesh now...");

                Mesh proxyMesh = BuildNavMeshProxyMeshFromWorld(
                    worldVertices,
                    triangles,
                    navMeshProxyFilter.transform
                );

                navMeshProxyFilter.mesh = proxyMesh;
                navMeshProxyCollider.sharedMesh = null;
                navMeshProxyCollider.sharedMesh = proxyMesh;

                Debug.Log("Proxy mesh successfully assigned!");

                if (navMeshSurface != null)
                {
                    StartCoroutine(WaitAndBakeNavMesh(1.0f));
                }
            }
            else
            {
                Debug.LogWarning("NavMesh proxy NOT built. Something is missing.");
            }
        });
    }

    private Mesh BuildNavMeshProxyMeshFromWorld(Vector3[] sourceWorldVertices, List<int> sourceTriangles, Transform proxyTransform)
    {
        Dictionary<int, int> oldToNew = new Dictionary<int, int>();
        List<Vector3> proxyVertices = new List<Vector3>();
        List<int> proxyTriangles = new List<int>();

        for (int tri = 0; tri < sourceTriangles.Count; tri++)
        {
            int oldIndex = sourceTriangles[tri];

            if (!oldToNew.TryGetValue(oldIndex, out int newIndex))
            {
                newIndex = proxyVertices.Count;
                oldToNew[oldIndex] = newIndex;

                Vector3 proxyLocalVertex = proxyTransform.InverseTransformPoint(sourceWorldVertices[oldIndex]);
                proxyVertices.Add(proxyLocalVertex);
            }

            proxyTriangles.Add(newIndex);
        }

        for (int i = 0; i < proxyTriangles.Count; i += 3)
        {
            int temp = proxyTriangles[i];
            proxyTriangles[i] = proxyTriangles[i + 1];
            proxyTriangles[i + 1] = temp;
        }

        if (navMeshHeightScale < 0.999f && proxyVertices.Count > 0)
        {
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            for (int i = 0; i < proxyVertices.Count; i++)
            {
                if (proxyVertices[i].y < minY) minY = proxyVertices[i].y;
                if (proxyVertices[i].y > maxY) maxY = proxyVertices[i].y;
            }

            float centerY = (minY + maxY) * 0.5f;

            for (int i = 0; i < proxyVertices.Count; i++)
            {
                Vector3 v = proxyVertices[i];
                float offset = v.y - centerY;
                v.y = centerY + offset * navMeshHeightScale;
                proxyVertices[i] = v;
            }
        }

        Mesh proxyMesh = new Mesh();
        proxyMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        proxyMesh.vertices = proxyVertices.ToArray();
        proxyMesh.triangles = proxyTriangles.ToArray();
        proxyMesh.RecalculateNormals();
        proxyMesh.RecalculateBounds();

        return proxyMesh;
    }

    private IEnumerator WaitAndBakeNavMesh(float delay)
    {
        Debug.Log($"Waiting {delay} seconds for physics to catch up before baking...");
        yield return new WaitForSeconds(delay);
        ForceBakeNavMesh();
    }

    private void ForceBakeNavMesh()
    {
        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();
            Debug.Log("AI NavMesh baked successfully!");
        }
        else
        {
            Debug.LogWarning("NavMeshSurface is missing in the Inspector! Cannot bake.");
        }
    }
}