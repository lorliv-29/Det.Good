using UnityEngine;
using UnityEngine.InputSystem;
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

    [Header("Sandbox Cropping (World Space)")]
    public float minX = -0.5f;
    public float maxX = 0.5f;
    public float minZ = -0.5f;
    public float maxZ = 0.5f;

    [Tooltip("The 'Ceiling'. Anything higher than this is instantly deleted. Perfect for hiding arms!")]
    public float maxY = 0.9f;

    // === STABILITY CONTROLS ===
    [Header("Mesh Stability (Temporal Smoothing)")]
    [Range(0.8f, 0.99f)]
    [Tooltip("A higher number blends more frames together, making it more stable but creating a slight 'ghosting' delay. (0.95 is ideal)")]
    public float smoothingFactor = 0.95f;
    [Range(0.005f, 0.1f)]
    [Tooltip("If a point moves further than this distance (in meters) in one frame, it snaps instantly. 0.02 is 2cm.")]
    public float movementThreshold = 0.02f;
    [Tooltip("If an object is moving fast AND is higher than this Y value, the shader ignores it (filters hands).")]
    public float handHeightMin = 0.3f;

    private ComputeBuffer rawBuffer;
    public ComputeBuffer calibratedBuffer { get; private set; }
    private ComputeBuffer smoothedPreviousBuffer; // The GPU memory

    private Vector3[] sandVertices;
    private int vertexCount = 0;
    private FrameQueue frameQueue;

    // Kernels
    private int calibrateKernel;
    private int smoothKernel;

    private bool isEditMode = true;

    void Start()
    {
        Source.OnStart += OnStartStreaming;
        Source.OnStop += Dispose;

        if (meshBuilder != null)
        {
            meshBuilder.gameObject.SetActive(false);
        }
    }

    private void OnStartStreaming(PipelineProfile profile)
    {
        frameQueue = new FrameQueue(1);

        // Find both kernels now
        calibrateKernel = sandCalibrator.FindKernel("CalibrateSand");
        smoothKernel = sandCalibrator.FindKernel("TemporalSmooth");

        Source.OnNewSample += OnNewSample;
    }

    private void OnNewSample(Frame frame)
    {
        if (frameQueue == null || !isEditMode) return;

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
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            isEditMode = !isEditMode;

            if (!isEditMode)
            {
                if (meshBuilder != null && calibratedBuffer != null)
                {
                    meshBuilder.gameObject.SetActive(true);
                    meshBuilder.GeneratePhysicalMesh(calibratedBuffer);
                }
            }
            else
            {
                if (meshBuilder != null)
                {
                    meshBuilder.gameObject.SetActive(false);
                }
            }
        }

        if (isEditMode && frameQueue != null)
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

                            // Release old buffers
                            if (rawBuffer != null) rawBuffer.Release();
                            if (calibratedBuffer != null) calibratedBuffer.Release();
                            if (smoothedPreviousBuffer != null) smoothedPreviousBuffer.Release();

                            // ALLOCATE THE NEW MEMORY BUFFER (12 bytes per Vector3)
                            rawBuffer = new ComputeBuffer(vertexCount, 12);
                            calibratedBuffer = new ComputeBuffer(vertexCount, 12);
                            smoothedPreviousBuffer = new ComputeBuffer(vertexCount, 12);
                        }

                        points.CopyVertices(sandVertices);
                        rawBuffer.SetData(sandVertices);

                        RunComputeShaderSequentially();
                    }
                }
            }
        }

        if (isEditMode && calibratedBuffer != null && sandGrainMesh != null && sandMaterial != null && vertexCount > 0)
        {
            sandMaterial.SetBuffer("CalibratedPoints", calibratedBuffer);
            Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 1000f);
            Graphics.DrawMeshInstancedProcedural(sandGrainMesh, 0, sandMaterial, bounds, vertexCount);
        }
    }

    private void RunComputeShaderSequentially()
    {
        if (sandCalibrator == null || rawBuffer == null || calibratedBuffer == null || smoothedPreviousBuffer == null) return;

        // 1. Prepare KERNEL 1 (Transformation and Cropping)
        sandCalibrator.SetBuffer(calibrateKernel, "RawPoints", rawBuffer);
        sandCalibrator.SetBuffer(calibrateKernel, "CalibratedPoints", calibratedBuffer);
        sandCalibrator.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);
        sandCalibrator.SetInt("_VertexCount", vertexCount);

        sandCalibrator.SetFloat("_MinX", minX);
        sandCalibrator.SetFloat("_MaxX", maxX);
        sandCalibrator.SetFloat("_MinZ", minZ);
        sandCalibrator.SetFloat("_MaxZ", maxZ);
        // === SEND CEILING TO GPU ===
        sandCalibrator.SetFloat("_MaxY", maxY);

        // Run Kernel 1 (Writes transform points to calibratedBuffer)
        int threadGroups = Mathf.CeilToInt(vertexCount / 64f);
        sandCalibrator.Dispatch(calibrateKernel, threadGroups, 1, 1);

        // 2. Prepare KERNEL 2 (The Stabilizer)
        sandCalibrator.SetBuffer(smoothKernel, "CalibratedPoints", calibratedBuffer);
        sandCalibrator.SetBuffer(smoothKernel, "SmoothedPointsPrevious", smoothedPreviousBuffer);
        sandCalibrator.SetFloat("_SmoothingFactor", smoothingFactor);
        sandCalibrator.SetFloat("_MovementThreshold", movementThreshold);
        // === SEND HYBRID HAND FILTER TO GPU ===
        sandCalibrator.SetFloat("_HandHeightMin", handHeightMin);

        sandCalibrator.SetInt("_VertexCount", vertexCount);

        // Run Kernel 2 (Writes stabilized points to calibratedBuffer)
        sandCalibrator.Dispatch(smoothKernel, threadGroups, 1, 1);
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
        if (smoothedPreviousBuffer != null)
        {
            smoothedPreviousBuffer.Release();
            smoothedPreviousBuffer = null;
        }
    }

    void OnDestroy()
    {
        Dispose();
    }
}