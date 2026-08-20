using UnityEngine;


/// <summary>
/// Controls the appearance and synchronization of a dashed outline effect for a sprite in a Unity scene.
/// </summary>


public class DashedOutlineController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer mainRenderer;
    [SerializeField] private SpriteRenderer myRenderer;
    private Material dashedOutline;
    [SerializeField] private Color outlineColor = Color.white;




    void Start()
    {
        dashedOutline = myRenderer.material;
    }




    void LateUpdate()
    {
        myRenderer.sprite = mainRenderer.sprite;
        myRenderer.flipX = mainRenderer.flipX;
        dashedOutline.color = outlineColor;
    }
}