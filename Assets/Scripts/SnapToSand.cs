/*
using UnityEngine;

public class SnapToSand : MonoBehaviour
{
    public LayerMask sandLayer;

    public void SnapToCurrentMesh()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 10f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 20f, sandLayer))
        {
            transform.position = hit.point;
            transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
        }
    }
}
*/

//NEW ANTI-FALL CODE
using UnityEngine;

public class SnapToSand : MonoBehaviour
{
    [Header("Sand Detection")]
    public LayerMask sandLayer;
    public float rayStartHeight = 10f;
    public float rayDistance = 30f;

    [Header("Rotation")]
    public bool forceYRotationToZero = false;

    public void SnapToCurrentMesh()
    {
        Vector3 rayStart = transform.position + Vector3.up * rayStartHeight;
        Ray ray = new Ray(rayStart, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, sandLayer))
        {
            // Snap to sand position
            transform.position = hit.point;

            // Keep upright so object does not tilt/fall over
            float yRotation = forceYRotationToZero ? 0f : transform.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
        }
    }

    // Optional helper for quick testing in editor/runtime
    [ContextMenu("Snap To Current Mesh")]
    private void TestSnap()
    {
        SnapToCurrentMesh();
    }
}