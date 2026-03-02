using UnityEngine;
using UnityEngine.Rendering;

public class SandMeshBuilder : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public MeshFilter meshFilter;
    public MeshCollider meshCollider;

    [Header("Grid Dimensions")]
    [Tooltip("Camera width divided by decimation filter magnitude")]
    public int gridWidth = 160;
    [Tooltip("Camera height divided by decimation filter magnitude")]
    public int gridHeight = 120;

    // Call this method from your main script when you want to freeze the sand
    public void GeneratePhysicalMesh(ComputeBuffer pointBuffer)
    {
        // 1. The Async Request: Ask the GPU for the data without freezing the game
        AsyncGPUReadback.Request(pointBuffer, request =>
        {
            if (request.hasError)
            {
                Debug.LogError("GPU Readback failed! The buffer might be invalid.");
                return;
            }

            // 2. Extract the raw 3D coordinates (Vertices) from the GPU
            var gpuData = request.GetData<Vector3>();
            Vector3[] vertices = gpuData.ToArray();

            // 3. Prepare the Triangles list (Using a List instead of an Array so we can skip bad triangles)
            System.Collections.Generic.List<int> triangles = new System.Collections.Generic.List<int>();

            // 4. Stitch the points together (WITH ZERO-DEPTH FILTER)
            for (int y = 0; y < gridHeight - 1; y++)
            {
                for (int x = 0; x < gridWidth - 1; x++)
                {
                    int i = y * gridWidth + x;
                    int iRight = i + 1;
                    int iTop = i + gridWidth;
                    int iTopRight = i + gridWidth + 1;

                    // FILTER: Only draw triangles if ALL points are further away than 10cm (0.1f)
                    // This ignores the broken (0,0,0) points stuck in the camera lens!

                    // Triangle 1
                    if (vertices[i].z > 0.1f && vertices[iRight].z > 0.1f && vertices[iTop].z > 0.1f)
                    {
                        triangles.Add(i);
                        triangles.Add(iTop);
                        triangles.Add(iRight);
                    }

                    // Triangle 2
                    if (vertices[iRight].z > 0.1f && vertices[iTop].z > 0.1f && vertices[iTopRight].z > 0.1f)
                    {
                        triangles.Add(iRight);
                        triangles.Add(iTop);
                        triangles.Add(iTopRight);
                    }
                }
            }

            // 5. Build the Unity Mesh
            Mesh solidMesh = new Mesh();
            solidMesh.indexFormat = IndexFormat.UInt32;
            solidMesh.vertices = vertices;
            solidMesh.triangles = triangles.ToArray(); // Convert our filtered list back to an array


            // 6. Calculate lighting normals so shadows work properly
            solidMesh.RecalculateNormals();

            // 7. Apply the mesh to the visual filter and the physical collider
            if (meshFilter != null) meshFilter.mesh = solidMesh;
            if (meshCollider != null) meshCollider.sharedMesh = solidMesh;

            Debug.Log("Physical Sand Mesh Generated Successfully!");
        });
    }
}