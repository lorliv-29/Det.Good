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
        if (frameQueue != null)
        {
            Points points;
            if (frameQueue.PollForFrame<Points>(out points))
            {
                using (points)
                {
                    if (points.VertexData != System.IntPtr.Zero)
                    {
                        // 1. Copy raw data
                        points.CopyVertices(sandVertices);

                        // 2. Upload raw data to the first GPU buffer
                        if (rawBuffer != null)
                        {
                            rawBuffer.SetData(sandVertices);
                        }

                        // 3. Run the Compute Shader to calibrate the sand
                        RunComputeShader();
                    }
                }
            }
        }
    }

    private void RunComputeShader()
    {
        if (sandCalibrator == null || rawBuffer == null || calibratedBuffer == null) return;

        // Link our C# buffers to the HLSL variables
        sandCalibrator.SetBuffer(calibrateKernel, "RawPoints", rawBuffer);
        sandCalibrator.SetBuffer(calibrateKernel, "CalibratedPoints", calibratedBuffer);

        // Send our inspector calibration settings to the GPU
        sandCalibrator.SetVector("_Offset", sandboxOffset);
        sandCalibrator.SetVector("_Scale", sandboxScale);
        sandCalibrator.SetInt("_VertexCount", vertexCount);

        // Calculate how many thread groups we need (total vertices divided by our batch size of 64)
        int threadGroups = Mathf.CeilToInt(vertexCount / 64f);

        // Dispatch (execute) the shader on the GPU
        sandCalibrator.Dispatch(calibrateKernel, threadGroups, 1, 1);

        // --- THE PROCEDURAL RENDERING COMMAND ---
        if (sandGrainMesh != null && sandMaterial != null)
        {
            // Give our material access to the calibrated buffer
            sandMaterial.SetBuffer("CalibratedPoints", calibratedBuffer);

            // Define a large bounding box so Unity doesn't accidentally cull (hide) our sand
            Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 10f);

            // Tell the GPU to draw the mesh for every single point in our buffer!
            Graphics.DrawMeshInstancedProcedural(sandGrainMesh, 0, sandMaterial, bounds, vertexCount);
        }
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