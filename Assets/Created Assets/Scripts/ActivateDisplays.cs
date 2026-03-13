using UnityEngine;

public class ActivateDisplays : MonoBehaviour
{
    void Start()
    {
        if (Display.displays.Length > 1)
        {
            Display.displays[1].Activate(); // activates Display 2
        }
    }
}