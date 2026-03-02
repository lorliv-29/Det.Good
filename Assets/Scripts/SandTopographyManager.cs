using UnityEngine;
using Intel.RealSense;
using System.Linq;

public class SandTopographyManager : MonoBehaviour
{
    public SandMeshBuilder meshBuilder;

    [Header("Sandbox Cropping (World Space)")]
    public float minX = -0.5f;
    public float maxX = 0.5f;
    public float minZ = -0.5f;
    public float maxZ = 0.5f;

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
        calibrateKernel = sandCalibrator.FindKernel("CalibrateSand");
        Source.OnNewSample += OnNewSample;
    }

    private void OnNewSample(Frame frame)
    {
        // === THE SOFTWARE PAUSE ===
        // If we are in Play Mode, instantly drop the camera frame to save CPU power!
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
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isEditMode = !isEditMode;

            if (!isEditMode)
            {
                Debug.Log("Switched to PLAY MODE. Freezing sand & pausing camera...");
                if (meshBuilder != null && calibratedBuffer != null)
                {
                    meshBuilder.gameObject.SetActive(true);
                    meshBuilder.GeneratePhysicalMesh(calibratedBuffer);
                }
            }
            else
            {
                Debug.Log("Switched to EDIT MODE. Hologram & camera active...");
                if (meshBuilder != null)
                {
                    meshBuilder.gameObject.SetActive(false);
                }
            }
        }

        // === ONLY PROCESS HEAVY MATH IN EDIT MODE ===
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

                            if (rawBuffer != null) rawBuffer.Release();
                            if (calibratedBuffer != null) calibratedBuffer.Release();

                            rawBuffer = new ComputeBuffer(vertexCount, 12);
                            calibratedBuffer = new ComputeBuffer(vertexCount, 12);
                        }

                        points.CopyVertices(sandVertices);
                        rawBuffer.SetData(sandVertices);

                        RunComputeShader(); // This heavy GPU call now only runs in Edit Mode!
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

    private void RunComputeShader()
    {
        if (sandCalibrator == null || rawBuffer == null || calibratedBuffer == null) return;

        sandCalibrator.SetBuffer(calibrateKernel, "RawPoints", rawBuffer);
        sandCalibrator.SetBuffer(calibrateKernel, "CalibratedPoints", calibratedBuffer);

        sandCalibrator.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);
        sandCalibrator.SetInt("_VertexCount", vertexCount);

        // SEND THE BOUNDING BOX LIMITS TO THE GPU
        sandCalibrator.SetFloat("_MinX", minX);
        sandCalibrator.SetFloat("_MaxX", maxX);
        sandCalibrator.SetFloat("_MinZ", minZ);
        sandCalibrator.SetFloat("_MaxZ", maxZ);

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