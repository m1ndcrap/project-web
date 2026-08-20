using UnityEngine;


/// <summary>
/// Caps the game's frame rate to a specified value. Disables VSync to allow manual frame rate control.
/// </summary>


[ExecuteInEditMode]
public class FPSCap : MonoBehaviour
{
    private int fps = 60;


    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = fps;
    }
}