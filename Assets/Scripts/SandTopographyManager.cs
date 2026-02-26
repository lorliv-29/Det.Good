using UnityEngine;
using Intel.RealSense;
using System.Linq;

public class SandTopographyManager : MonoBehaviour
{
    [Header("RealSense Integration")]
    public RsFrameProvider Source;

    // The GPU buffer that will hold all our sand coordinates
    public ComputeBuffer topographyBuffer { get; private set; }

    private Vector3[] sandVertices;
    private int vertexCount;
    private FrameQueue frameQueue;

    void Start()
    {
        // Subscribe to the RealSense camera events
        Source.OnStart += OnStartStreaming;
        Source.OnStop += Dispose;
    }

    private void OnStartStreaming(PipelineProfile profile)
    {
        frameQueue = new FrameQueue(1);

        // Find the depth stream to determine our grid resolution
        using (var depth = profile.Streams.FirstOrDefault(s => s.Stream == Stream.Depth && s.Format == Format.Z16).As<VideoStreamProfile>())
        {
            vertexCount = depth.Width * depth.Height;

            // Initialize the array to hold the points on the CPU temporarily
            sandVertices = new Vector3[vertexCount];

            // Initialize the ComputeBuffer for the GPU
            // We need vertexCount elements, and each element is a Vector3 (3 floats * 4 bytes = 12 bytes)
            topographyBuffer = new ComputeBuffer(vertexCount, 12);
        }

        Source.OnNewSample += OnNewSample;
    }

    private void OnNewSample(Frame frame)
    {
        if (frameQueue == null) return;

        // Isolate the point cloud data from the incoming frame
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
            // Poll the queue for the latest depth frame
            if (frameQueue.PollForFrame<Points>(out points))
            {
                using (points)
                {
                    if (points.VertexData != System.IntPtr.Zero)
                    {
                        // 1. Copy the raw coordinates from the camera
                        points.CopyVertices(sandVertices);

                        // 2. Upload those coordinates directly to the GPU's ComputeBuffer
                        if (topographyBuffer != null)
                        {
                            topographyBuffer.SetData(sandVertices);
                        }
                    }
                }
            }
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

        // Always release the ComputeBuffer to prevent memory leaks!
        if (topographyBuffer != null)
        {
            topographyBuffer.Release();
            topographyBuffer = null;
        }
    }

    void OnDestroy()
    {
        Dispose();
    }
}