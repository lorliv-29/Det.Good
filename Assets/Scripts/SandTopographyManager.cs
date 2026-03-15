using UnityEngine;
using UnityEngine.InputSystem;
using Intel.RealSense;
using System.Linq;

public class SandTopographyManager : MonoBehaviour
{
    [Header("References")]
    public SandMeshBuilder meshBuilder;

    [Header("RealSense Integration")]
    public RsFrameProvider Source;

    [Header("GPU Compute Shader")]
    public ComputeShader sandCalibrator;

    [Header("Rendering")]
    public Mesh sandGrainMesh;
    public Material sandMaterial;

    [Header("Sandbox Cropping (World Space)")]
    [Tooltip("Left edge of the sandbox in world space.")]
    public float minX = -0.5f;

    [Tooltip("Right edge of the sandbox in world space.")]
    public float maxX = 0.5f;

    [Tooltip("Back edge of the sandbox in world space.")]
    public float minZ = -0.15f;

    [Tooltip("Front edge of the sandbox in world space.")]
    public float maxZ = 0.5f;

    [Header("Vertical Crop (World Space)")]
    public float bottomClipY = 0.0f;

    [Header("Perimeter Protection")]
    [Tooltip("Inner border around the sandbox where points are frozen to prevent hand junk near the edges.")]
    public float perimeterMargin = 0.08f;

    [Header("Height Controls")]
    [Tooltip("Above this world-space height, terrain stops updating and keeps the previous stable terrain.")]
    public float occlusionFreezeY = 1.95f;

    [Tooltip("Above this world-space height, points are deleted completely.")]
    public float hardDeleteY = 2.15f;

    [Header("Temporal Stability")]
    [Range(0.0f, 0.99f)]
    [Tooltip("How much previous terrain is blended into tiny jitter changes.")]
    public float smoothingFactor = 0.9f;

    [Range(0.0f, 0.05f)]
    [Tooltip("Only changes smaller than this are smoothed. Larger changes update directly to avoid ripple effects.")]
    public float jitterThreshold = 0.01f;

    private ComputeBuffer rawBuffer;
    private ComputeBuffer _calibratedBuffer;
    public ComputeBuffer calibratedBuffer => _calibratedBuffer;
    private ComputeBuffer smoothedPreviousBuffer;

    private Vector3[] sandVertices;
    private int vertexCount = 0;
    private FrameQueue frameQueue;

    private int calibrateKernel;
    private int smoothKernel;

    public bool isEditMode = true;

    private void Start()
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
                if (points != null)
                {
                    frameQueue.Enqueue(points);
                }
            }
        }
        else if (frame.Is(Extension.Points))
        {
            frameQueue.Enqueue(frame);
        }
    }

    private void LateUpdate()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            ToggleMeshMode();
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
                        EnsureBuffers(points.Count);

                        points.CopyVertices(sandVertices);
                        rawBuffer.SetData(sandVertices);

                        RunComputeShaderSequentially();
                    }
                }
            }
        }

        if (isEditMode &&
            _calibratedBuffer != null &&
            sandGrainMesh != null &&
            sandMaterial != null &&
            vertexCount > 0)
        {
            sandMaterial.SetBuffer("CalibratedPoints", _calibratedBuffer);

            Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 1000f);
            Graphics.DrawMeshInstancedProcedural(
                sandGrainMesh,
                0,
                sandMaterial,
                bounds,
                vertexCount
            );
        }
    }

    private void EnsureBuffers(int newVertexCount)
    {
        if (newVertexCount == vertexCount && rawBuffer != null)
            return;

        vertexCount = newVertexCount;
        sandVertices = new Vector3[vertexCount];

        ReleaseBuffer(ref rawBuffer);
        ReleaseBuffer(ref _calibratedBuffer);
        ReleaseBuffer(ref smoothedPreviousBuffer);

        rawBuffer = new ComputeBuffer(vertexCount, sizeof(float) * 3);
        _calibratedBuffer = new ComputeBuffer(vertexCount, sizeof(float) * 3);
        smoothedPreviousBuffer = new ComputeBuffer(vertexCount, sizeof(float) * 3);
    }

    private void RunComputeShaderSequentially()
    {
        if (sandCalibrator == null ||
            rawBuffer == null ||
            _calibratedBuffer == null ||
            smoothedPreviousBuffer == null)
        {
            return;
        }

        int threadGroups = Mathf.CeilToInt(vertexCount / 64f);

        // Pass 1: calibration + crop classification
        sandCalibrator.SetBuffer(calibrateKernel, "RawPoints", rawBuffer);
        sandCalibrator.SetBuffer(calibrateKernel, "CalibratedPoints", _calibratedBuffer);
        sandCalibrator.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);
        sandCalibrator.SetInt("_VertexCount", vertexCount);

        sandCalibrator.SetFloat("_MinX", minX);
        sandCalibrator.SetFloat("_MaxX", maxX);
        sandCalibrator.SetFloat("_MinZ", minZ);
        sandCalibrator.SetFloat("_MaxZ", maxZ);
        sandCalibrator.SetFloat("_PerimeterMargin", perimeterMargin);
        sandCalibrator.SetFloat("_HardDeleteY", hardDeleteY);
        sandCalibrator.SetFloat("_BottomClipY", bottomClipY);

        sandCalibrator.Dispatch(calibrateKernel, threadGroups, 1, 1);

        // Pass 2: temporal persistence + occlusion freeze + jitter smoothing
        sandCalibrator.SetBuffer(smoothKernel, "CalibratedPoints", _calibratedBuffer);
        sandCalibrator.SetBuffer(smoothKernel, "SmoothedPointsPrevious", smoothedPreviousBuffer);
        sandCalibrator.SetFloat("_SmoothingFactor", smoothingFactor);
        sandCalibrator.SetFloat("_JitterThreshold", jitterThreshold);
        sandCalibrator.SetFloat("_OcclusionFreezeY", occlusionFreezeY);
        sandCalibrator.SetFloat("_HardDeleteY", hardDeleteY);
        sandCalibrator.SetFloat("_PerimeterMargin", perimeterMargin);
        sandCalibrator.SetInt("_VertexCount", vertexCount);

        sandCalibrator.Dispatch(smoothKernel, threadGroups, 1, 1);
    }

    public void ToggleMeshMode()
    {
        isEditMode = !isEditMode;

        if (!isEditMode)
        {
            if (meshBuilder != null && _calibratedBuffer != null)
            {
                meshBuilder.gameObject.SetActive(true);
                meshBuilder.GeneratePhysicalMesh(_calibratedBuffer);
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

    private void ReleaseBuffer(ref ComputeBuffer buffer)
    {
        if (buffer != null)
        {
            buffer.Release();
            buffer = null;
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

        ReleaseBuffer(ref rawBuffer);
        ReleaseBuffer(ref _calibratedBuffer);
        ReleaseBuffer(ref smoothedPreviousBuffer);
    }

    private void OnDestroy()
    {
        Dispose();
    }
}