using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SandMeshBuilder : MonoBehaviour
{
    [Header("NavMesh Proxy")]
    public MeshFilter navMeshProxyFilter;
    public MeshCollider navMeshProxyCollider;
    public bool buildNavMeshProxy = true;

    [Range(0f, 1f)]
    public float navMeshHeightScale = 1.0f;

    [Header("Assign in Inspector")]
    public MeshFilter meshFilter;
    public MeshCollider meshCollider;
    public Transform referenceTransform;

    [Header("Grid Dimensions")]
    [Tooltip("Camera width divided by decimation filter magnitude")]
    public int gridWidth = 160;

    [Tooltip("Camera height divided by decimation filter magnitude")]
    public int gridHeight = 120;

    [Header("Mesh Cleanup")]
    [Tooltip("Anything below this is considered invalid/hidden terrain.")]
    public float invalidYThreshold = -500f;

    [Tooltip("Only stitch points together if edges are shorter than this.")]
    public float maxEdge = 0.1f;

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

            // Build triangles only from valid points
            for (int y = 0; y < gridHeight - 1; y++)
            {
                for (int x = 0; x < gridWidth - 1; x++)
                {
                    int i = y * gridWidth + x;
                    int iRight = i + 1;
                    int iTop = i + gridWidth;
                    int iTopRight = i + gridWidth + 1;

                    // Skip any quad touching invalid underground points
                    if (worldVertices[i].y < invalidYThreshold ||
                        worldVertices[iRight].y < invalidYThreshold ||
                        worldVertices[iTop].y < invalidYThreshold ||
                        worldVertices[iTopRight].y < invalidYThreshold)
                    {
                        continue;
                    }

                    // Triangle 1
                    if (Vector3.Distance(worldVertices[i], worldVertices[iRight]) < maxEdge &&
                        Vector3.Distance(worldVertices[i], worldVertices[iTop]) < maxEdge)
                    {
                        triangles.Add(i);
                        triangles.Add(iTop);
                        triangles.Add(iRight);
                    }

                    // Triangle 2
                    if (Vector3.Distance(worldVertices[iRight], worldVertices[iTop]) < maxEdge &&
                        Vector3.Distance(worldVertices[iTop], worldVertices[iTopRight]) < maxEdge)
                    {
                        triangles.Add(iRight);
                        triangles.Add(iTop);
                        triangles.Add(iTopRight);
                    }
                }
            }

            // Convert into local space of the chosen reference
            Transform t = referenceTransform != null ? referenceTransform : transform;

            // Compact the mesh so only used vertices survive
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
           
            Debug.Log($"Physical Sand Mesh Generated Successfully! Vertices={compactVertices.Count}, Triangles={compactTriangles.Count / 3}");

            Debug.Log("Attempting to build NavMesh Proxy...");
            Debug.Log("buildNavMeshProxy = " + buildNavMeshProxy);
            Debug.Log("navMeshProxyFilter assigned = " + (navMeshProxyFilter != null));
            Debug.Log("navMeshProxyCollider assigned = " + (navMeshProxyCollider != null));
            Debug.Log("worldVertices count = " + worldVertices.Length);
            Debug.Log("triangle count = " + triangles.Count);

            if (buildNavMeshProxy && navMeshProxyFilter != null && navMeshProxyCollider != null)
            {
                Debug.Log("Building NavMesh proxy mesh now...");

                Mesh proxyMesh = BuildNavMeshProxyMeshFromWorld(
                    worldVertices,
                    triangles,
                    navMeshProxyFilter.transform
                );

                navMeshProxyFilter.mesh = proxyMesh; // use .mesh for runtime
                navMeshProxyCollider.sharedMesh = null;
                navMeshProxyCollider.sharedMesh = proxyMesh;

                Debug.Log("Proxy mesh successfully assigned!");
            }
            else
            {
                Debug.LogWarning("NavMesh proxy NOT built. Something is missing.");
            }

        });
    }

    private Mesh BuildNavMeshProxyMeshFromWorld(
    Vector3[] sourceWorldVertices,
    List<int> sourceTriangles,
    Transform proxyTransform)
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

                // Convert ORIGINAL WORLD point into PROXY LOCAL space
                Vector3 proxyLocalVertex = proxyTransform.InverseTransformPoint(sourceWorldVertices[oldIndex]);
                proxyVertices.Add(proxyLocalVertex);
            }

            proxyTriangles.Add(newIndex);
        }
        // Reverse triangle winding so the proxy normals face the correct way
        for (int i = 0; i < proxyTriangles.Count; i += 3)
        {
            int temp = proxyTriangles[i];
            proxyTriangles[i] = proxyTriangles[i + 1];
            proxyTriangles[i + 1] = temp;
        }

        // slightly flatten height differences for easier NavMesh baking
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
}