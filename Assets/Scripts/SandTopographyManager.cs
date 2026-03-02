using UnityEngine;
using Intel.RealSense;
using System.Linq;

public class SandTopographyManager : MonoBehaviour
{
    public SandMeshBuilder meshBuilder;

    [Header("RealSense Integration")]
    public RsFrameProvider Source;

    [Header("GPU Compute Shader")]
    public ComputeShader sandCalibrator;

    [Header("Rendering")]
    public Mesh sandGrainMesh;
    public Material sandMaterial;

    private ComputeBuffer rawBuffer;
    public ComputeBuffer calibratedBuffer { get; private set; }

    private Vector3[] sandVertices;
    private int vertexCount = 0;
    private FrameQueue frameQueue;
    private int calibrateKernel;

    void Start()
    {
        Source.OnStart += OnStartStreaming;
        Source.OnStop += Dispose;
    }

    private void OnStartStreaming(PipelineProfile profile)
    {
        frameQueue = new FrameQueue(1);
        calibrateKernel = sandCalibrator.FindKernel("CalibrateSand");
        Source.OnNewSample += OnNewSample;
    }

    private void OnNewSample(Frame frame)
    {
        if (frameQueue == null) return;

        if (frame.IsComposite)
        {
            using (var fs = frame.As<FrameSet>())
            using (var points = fs.FirstOrDefault<Points>(Stream.Depth, Format.Xyz32f))
            {
                if (points != null) frameQueue.Enqueue(points);
            }
        }
        else if (frame.Is(Extension.Points))
        {
            frameQueue.Enqueue(frame);
        }
    }

    void LateUpdate()
    {
        // === TRIGGER MESH GENERATION ===
        // Press the Spacebar to freeze the sand into a physical collider!
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (meshBuilder != null && calibratedBuffer != null)
            {
                meshBuilder.GeneratePhysicalMesh(calibratedBuffer);
            }
            else
            {
                Debug.LogWarning("Cannot build mesh! Either the MeshBuilder is missing or the camera hasn't generated points yet.");
            }
        }
        // ===============================

        if (frameQueue != null)
        {
            Points points;
            if (frameQueue.PollForFrame<Points>(out points))
            {
                using (points)
                {
                    if (points.VertexData != System.IntPtr.Zero)
                    {
                        if (points.Count != vertexCount || rawBuffer == null)
                        {
                            vertexCount = points.Count;
                            sandVertices = new Vector3[vertexCount];

                            if (rawBuffer != null) rawBuffer.Release();
                            if (calibratedBuffer != null) calibratedBuffer.Release();

                            rawBuffer = new ComputeBuffer(vertexCount, 12);
                            calibratedBuffer = new ComputeBuffer(vertexCount, 12);
                        }

                        points.CopyVertices(sandVertices);
                        rawBuffer.SetData(sandVertices);

                        RunComputeShader();
                    }
                }
            }
        }

        if (calibratedBuffer != null && sandGrainMesh != null && sandMaterial != null && vertexCount > 0)
        {
            sandMaterial.SetBuffer("CalibratedPoints", calibratedBuffer);
            Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 1000f);
            Graphics.DrawMeshInstancedProcedural(sandGrainMesh, 0, sandMaterial, bounds, vertexCount);
        }
    }

    private void RunComputeShader()
    {
        if (sandCalibrator == null || rawBuffer == null || calibratedBuffer == null) return;

        sandCalibrator.SetBuffer(calibrateKernel, "RawPoints", rawBuffer);
        sandCalibrator.SetBuffer(calibrateKernel, "CalibratedPoints", calibratedBuffer);

        // SEND THE GAMEOBJECT TRANSFORM TO THE GPU
        sandCalibrator.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);
        sandCalibrator.SetInt("_VertexCount", vertexCount);

        int threadGroups = Mathf.CeilToInt(vertexCount / 64f);
        sandCalibrator.Dispatch(calibrateKernel, threadGroups, 1, 1);
    }

    private void Dispose()
    {
        Source.OnNewSample -= OnNewSample;

        if (frameQueue != null)
        {
            frameQueue.Dispose();
            frameQueue = null;
        }

        if (rawBuffer != null)
        {
            rawBuffer.Release();
            rawBuffer = null;
        }
        if (calibratedBuffer != null)
        {
            calibratedBuffer.Release();
            calibratedBuffer = null;
        }
    }

    void OnDestroy()
    {
        Dispose();
    }
}