using UnityEngine;
using Meta.XR.MRUtilityKit;

public class SceneJsonLoader : MonoBehaviour
{
    public TextAsset sceneJson;

    private void Start()
    {
        MRUK.Instance.LoadSceneFromJsonString(sceneJson.text, true);
    }
}
