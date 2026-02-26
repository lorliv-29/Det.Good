using UnityEngine;
using Intel.RealSense;
using System.Linq;

public class SandTopographyManager : MonoBehaviour
{
    [Header("RealSense Integration")]
    public RsFrameProvider Source;

    [Header("GPU Compute Shader")]
    public ComputeShader sandCalibrator;

    [Header("VR Sandbox Calibration")]
    public Vector3 sandboxOffset = new Vector3(0, 0, 0);
    public Vector3 sandboxScale = new Vector3(1, 1, 1);

    [Header("Rendering")]
    public Mesh sandGrainMesh;
    public Material sandMaterial;

    // We now need TWO buffers: one for the raw camera data, one for the final VR data
    private ComputeBuffer rawBuffer;
    public ComputeBuffer calibratedBuffer { get; private set; }

    private Vector3[] sandVertices;
    private int vertexCount;
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

        using (var depth = profile.Streams.FirstOrDefault(s => s.Stream == Stream.Depth && s.Format == Format.Z16).As<VideoStreamProfile>())
        {
            vertexCount = depth.Width * depth.Height;
            sandVertices = new Vector3[vertexCount];

            // Initialize both ComputeBuffers
            rawBuffer = new ComputeBuffer(vertexCount, 12);
            calibratedBuffer = new ComputeBuffer(vertexCount, 12);
        }

        // Find the ID of our specific function inside the Compute Shader
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
        // 1. UPDATE THE MATH (Happens ~30 times a second)
        if (frameQueue != null)
        {
            Points points;
            // Only update if the camera actually gave us a new frame
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

                        // Run the math only when new data arrives
                        RunComputeShader();
                    }
                }
            }
        }

        // 2. DRAW THE SAND (Happens ~90+ times a second, EVERY frame)
        if (calibratedBuffer != null && sandGrainMesh != null && sandMaterial != null && vertexCount > 0)
        {
            sandMaterial.SetBuffer("CalibratedPoints", calibratedBuffer);

            // Increased to a massive 1000m to prevent Unity from hiding the sand when you turn your head
            Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 1000f);

            // Draw the sand continuously using the last known coordinates
            Graphics.DrawMeshInstancedProcedural(sandGrainMesh, 0, sandMaterial, bounds, vertexCount);
        }
    }

    private void RunComputeShader()
    {
        if (sandCalibrator == null || rawBuffer == null || calibratedBuffer == null) return;

        sandCalibrator.SetBuffer(calibrateKernel, "RawPoints", rawBuffer);
        sandCalibrator.SetBuffer(calibrateKernel, "CalibratedPoints", calibratedBuffer);
        sandCalibrator.SetVector("_Offset", sandboxOffset);
        sandCalibrator.SetVector("_Scale", sandboxScale);
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

        // Release BOTH buffers to prevent memory leaks
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