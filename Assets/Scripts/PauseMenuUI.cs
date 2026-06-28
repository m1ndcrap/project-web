using UnityEngine;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private RawImage screenshotImage;
    [SerializeField] private float screenshotScale = 0.6f;
    [SerializeField] private Vector2 comicHoleCentre = new Vector2(400f, 300f);

    [Tooltip("0 = Continue, 1 = Restart, 2 = Quit")]
    public Text[] optionTexts = new Text[3];

    [SerializeField] private Color selectedOutlineColor = Color.yellow;
    [SerializeField] private Color defaultOutlineColor = Color.white;
    [SerializeField] private Image menuBackground;

    private void Start()
    {
        SetOption(0);
    }

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

        // We want playerInImage to coincide with comicHoleCentre on screen
        Vector2 imageOrigin = comicHoleCentre - playerInImage;
        rt.anchoredPosition = imageOrigin;

        screenshotImage.gameObject.SetActive(true);
    }

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