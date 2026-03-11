using System.IO;
using UnityEngine;
using Meta.XR.MRUtilityKit;

public class SceneJsonExporter : MonoBehaviour
{
    bool exported = false;

    void Start()
    {
        MRUK.Instance.SceneLoadedEvent.AddListener(OnSceneLoaded);

        // Load the room scan stored on the headset
        MRUK.Instance.LoadSceneFromDevice();
    }

    void OnDestroy()
    {
        if (MRUK.Instance != null)
            MRUK.Instance.SceneLoadedEvent.RemoveListener(OnSceneLoaded);
    }

    void OnSceneLoaded()
    {
        if (exported)
            return;
        exported = true;

        // Export scene JSON
        string json = MRUK.Instance.SaveSceneToJsonString(true, null);

        string path = Path.Combine(Application.persistentDataPath, "my_room.json");
        File.WriteAllText(path, json);

        Debug.Log("Room JSON saved to: " + path);
    }
}
