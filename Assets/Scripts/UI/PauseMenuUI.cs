using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// Pure display logic for the pause menu screen: shows the gameplay screenshot in a comic
/// panel centered on where the player was, and highlights whichever option is currently
/// selected. Has no logic of its own for deciding what's selected or what happens on confirm;
/// that's all driven externally by <see cref="PauseManager"/>.
/// </summary>


public class PauseMenuUI : MonoBehaviour
{
    [Tooltip("The UI image the gameplay screenshot is displayed on.")]
    [SerializeField] private RawImage screenshotImage;
    [Tooltip("How much to scale the screenshot down before displaying it.")]
    [SerializeField] private float screenshotScale = 0.6f;
    [Tooltip("The screen-space point the comic panel is centered on. The screenshot is positioned so the player's captured location lines up with this point.")]
    [SerializeField] private Vector2 comicPanel = new Vector2(374.6f, 332.9f);


    [Tooltip("0 = Continue, 1 = Restart, 2 = Quit")]
    public Text[] optionTexts = new Text[3];


    [SerializeField] private Color selectedOutlineColor = Color.yellow;
    [SerializeField] private Color defaultOutlineColor = Color.white;


    private void Start()
    {
        SetOption(0);
    }




    /// <summary>
    /// Sets the screenshot image to the specified texture and positions it so that the given player screen position
    /// aligns with the comic panel.
    /// </summary>
    /// <param name="texture">The texture to display as the screenshot. Cannot be null.</param>
    /// <param name="playerScreenPos">The position of the player within the screenshot, in screen coordinates. This position will be aligned with the
    /// comic panel.</param>
    public void SetScreenshot(Texture2D texture, Vector2 playerScreenPos)
    {
        if (screenshotImage == null || texture == null) return;

        screenshotImage.texture = texture;
        screenshotImage.color = Color.white;

        // Scale the image down
        float w = texture.width * screenshotScale;
        float h = texture.height * screenshotScale;
        RectTransform rt = screenshotImage.rectTransform;
        rt.sizeDelta = new Vector2(w, h);

        Vector2 playerInImage = playerScreenPos * screenshotScale;

        // We want playerInImage to coincide with comicPanel on screen
        Vector2 imageOrigin = comicPanel - playerInImage;
        rt.anchoredPosition = imageOrigin;

        screenshotImage.gameObject.SetActive(true);
    }




    /// <summary>
    /// Sets the currently selected option by updating the outline color of each option text element.
    /// </summary>
    /// <param name="option">The zero-based index of the option to select. Must be within the bounds of the option text elements array.</param>
    public void SetOption(int option)
    {
        for (int i = 0; i < optionTexts.Length; i++)
        {
            if (optionTexts[i] == null) continue;
            Outline outline = optionTexts[i].GetComponent<Outline>();
            if (outline != null) outline.effectColor = (i == option) ? selectedOutlineColor : defaultOutlineColor;
        }
    }
}