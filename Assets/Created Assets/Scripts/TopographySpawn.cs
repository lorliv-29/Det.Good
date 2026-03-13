using UnityEngine;

public class TopographyPlacement : MonoBehaviour
{
    public Transform spawnPoint;
    public Vector3 fixedScale = new Vector3(1f, 1f, 1f);

    void Start()
    {
        if (spawnPoint != null)
        {
            transform.position = spawnPoint.position;
        }

        transform.localScale = fixedScale;
    }
}