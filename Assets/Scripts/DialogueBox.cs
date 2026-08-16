using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class DialogueBox : MonoBehaviour
{
    private enum DialogueContext { None, Queens, BossFight, Mission }

    [Header("Scene Wiring")]
    [SerializeField] private DialogueContext context = DialogueContext.None;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private GliderScript goblinGlider;

    [Header("UI References")]
    [SerializeField] private RectTransform boxRect;
    [SerializeField] private RectTransform shadowRect;
    [SerializeField] private RectTransform borderTop;
    [SerializeField] private RectTransform borderBottom;
    [SerializeField] private RectTransform borderLeft;
    [SerializeField] private RectTransform borderRight;
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip dialogueSound;

    [Header("Player Overlap Transparency")]
    [Tooltip("Camera used to project the player's world position onto the screen. Defaults to Camera.main if left empty.")]
    [SerializeField] private Camera playerViewCamera;
    [Tooltip("Camera assigned to the Canvas rendering this UI. Leave empty if the Canvas is Screen Space - Overlay.")]
    [SerializeField] private Camera canvasCamera;
    [Range(0f, 1f)]
    [SerializeField] private float overlapAlpha = 0.5f;
    [Tooltip("How much alpha changes per frame while fading. Higher = faster fade.")]
    [SerializeField] private float fadeSpeed = 0.05f;
    private const float NORMAL_ALPHA = 1f;

    private CanvasRenderer[] fadeRenderers;
    private float currentAlpha = NORMAL_ALPHA;
    private float targetAlpha = NORMAL_ALPHA;

    private float borderThickness = 2f;
    private Vector2 shadowOffset = new Vector2(10f, 10f);
    private float uiScale = 1.81f;

    [Header("Box Open/Close Animation")]
    [SerializeField] private float boxWidth = 420f;
    [SerializeField] private float boxHeight = 50f;
    [SerializeField] private float openSpeedX = 21f;
    [SerializeField] private float openSpeedY = 2.5f;

    [Header("Text")]
    [SerializeField] private float nativeFontSize = 20f;

    // box edges
    private float x1, y1, x2, y2;

    private float targetLeft, targetTop, targetRight, targetBottom;
    private int phase = -1; // -1 closed, 0 opening, 1 showing text, 2 closing

    // text
    private readonly string[] text = new string[9];
    private int textCurrent = 0;
    private int dialogueNo = 0;
    private float charCurrent = 1f;
    private const float CHAR_SPEED = 0.5f;
    private float textX = 55f, textY = 18f;

    private float TextWidth => boxWidth - 2f * textX;

    private int alarm0 = -1;
    private int alarm1 = -1;

    private void Awake()
    {
        EnsureTopLeftPivot(boxRect);
        EnsureTopLeftPivot(shadowRect);
        EnsureTopLeftPivot(borderTop);
        EnsureTopLeftPivot(borderBottom);
        EnsureTopLeftPivot(borderLeft);
        EnsureTopLeftPivot(borderRight);

        if (dialogueText != null)
        {
            EnsureTopLeftPivot(dialogueText.rectTransform);
            dialogueText.fontSize = nativeFontSize * uiScale;
        }

        if (playerViewCamera == null) playerViewCamera = Camera.main;

        fadeRenderers = new[]
        {
            GetCanvasRenderer(boxRect),
            GetCanvasRenderer(shadowRect),
            GetCanvasRenderer(borderTop),
            GetCanvasRenderer(borderBottom),
            GetCanvasRenderer(borderLeft),
            GetCanvasRenderer(borderRight),
            dialogueText != null ? dialogueText.GetComponent<CanvasRenderer>() : null,
        };

        text[0] = "Use the WASD keys to control Spidey. Press the\nSpace Bar to jump and again to web swing.";
        text[1] = "Use the 'O' key to attack! Use the 'P' key when\nyour spider sense goes off to block!";
        text[2] = "Press the 'I' key to do a quick zip when you see\nthe yellow indicator.";
        text[3] = "Hold the 'U' key and use WASD keys to web zip to\nany surface.";
        text[4] = "Use the 'L' key to do an uppercut. Doing this\nbreaks an enemy's guard.";
        text[5] = "Use the 'U' key to shoot webs! Use your webs to\nweb up an enemy, object, and break guard!";
        text[6] = "Shocker's planted bombs throughout the bank.\nDestroy the bombs and find Shocker!";
        text[7] = "Follow Shocker!";
        text[8] = "Use the 'U' key to shoot webs! Use your webs to\nweb up an enemy, object, and break guard!";
    }

    private void Start()
    {
        SetOpenTargetCenter(260f, 40f);
        phase = -1;
        alarm1 = 5;
        alarm0 = 405;
    }

    private void Update()
    {
        TickAlarms();
        TickPhase();
        TickSceneTriggers();
        Draw();
    }

    // Alarms
    private void TickAlarms()
    {
        if (alarm0 > 0)
        {
            alarm0--;
            if (alarm0 == 0) phase = 2;
        }

        if (alarm1 > 0)
        {
            alarm1--;

            if (alarm1 == 0)
            {
                phase = 0;

                if (audioSource != null && dialogueSound != null)
                    audioSource.PlayOneShot(dialogueSound);
            }
        }
    }

    private void TickPhase()
    {
        if (phase == 0)
        {
            if (x1 > targetLeft)
            {
                x1 = Mathf.Max(x1 - openSpeedX * uiScale, targetLeft);
                x2 = Mathf.Min(x2 + openSpeedX * uiScale, targetRight);
                y1 = Mathf.Max(y1 - openSpeedY * uiScale, targetTop);
                y2 = Mathf.Min(y2 + openSpeedY * uiScale, targetBottom);
            }
            else
            {
                phase = 1;
            }
        }
        else if (phase == 2)
        {
            float centerX = (targetLeft + targetRight) * 0.5f;
            float centerY = (targetTop + targetBottom) * 0.5f;

            if (x1 < centerX)
            {
                x1 += openSpeedX * uiScale;
                x2 -= openSpeedX * uiScale;
            }
            else
            {
                phase = -1;
                dialogueNo++;
                textCurrent++;
                charCurrent = 1f;
                x1 = centerX; x2 = centerX;
                y1 = centerY; y2 = centerY;
            }
        }
    }

    private void TickSceneTriggers()
    {
        switch (context)
        {
            case DialogueContext.Queens:
                {
                    if (playerTransform != null && dialogueNo == 1 && phase == -1 && playerTransform.position.x >= 2.556f)
                    {
                        alarm1 = 5;
                        alarm0 = 405;
                        dialogueNo++;
                    }
                }
                break;

            case DialogueContext.BossFight:
                {
                    if (dialogueNo == 0 && phase == -1)
                    {
                        alarm1 = 5;
                        alarm0 = 405;
                        dialogueNo++;
                        textCurrent = 2;
                        SetOpenTargetCenter(260f, 240f);
                        textX = 55; textY = 218;
                    }

                    if (dialogueNo == 2 && phase == -1)
                    {
                        alarm1 = 5;
                        alarm0 = 405;
                        dialogueNo++;
                        textCurrent = 3;
                        SetOpenTargetCenter(260f, 240f);
                        textX = 55; textY = 218;
                    }

                    if (dialogueNo == 4 && phase == -1 && goblinGlider != null && goblinGlider.state == GliderScript.GState.GroundFight)
                    {
                        alarm1 = 5;
                        alarm0 = 405;
                        dialogueNo++;
                        textCurrent = 4;
                        SetOpenTargetCenter(260f, 240f);
                    }

                    if (dialogueNo == 6 && phase == -1 && goblinGlider != null && goblinGlider.state == GliderScript.GState.AirFight)
                    {
                        alarm1 = 5;
                        alarm0 = 405;
                        dialogueNo++;
                        textCurrent = 5;
                        SetOpenTargetCenter(260f, 240f);
                        charCurrent = 1f;
                        textX = 55; textY = 218;
                    }
                }
                break;

            case DialogueContext.Mission:
                {
                    if (dialogueNo == 0 && phase == -1)
                    {
                        alarm1 = 5;
                        alarm0 = 405;
                        dialogueNo++;
                        textCurrent = 6;
                    }

                    if (playerTransform != null && dialogueNo == 2 && phase == -1 && playerTransform.position.x >= 59.074f && playerTransform.position.y <= 9.483f)
                    {
                        alarm1 = 5;
                        alarm0 = 405;
                        dialogueNo++;
                        textCurrent = 7;
                        SetOpenTargetCenter(260f, 240f);
                        textX = 55; textY = 218;
                    }

                    if (playerTransform != null && dialogueNo == 4 && phase == -1 && playerTransform.position.x >= 98.69f && playerTransform.position.y >= 14.278f)
                    {
                        alarm1 = 5;
                        alarm0 = 405;
                        dialogueNo++;
                        textCurrent = 8;
                        charCurrent = 1f;
                        SetOpenTargetCenter(260f, 40f);
                        textX = 55; textY = 18;
                    }
                }
                break;
        }
    }

    private void SetOpenTargetCenter(float centerX, float centerY)
    {
        float scaledCenterX = centerX * uiScale;
        float scaledCenterY = centerY * uiScale;
        float scaledWidth = boxWidth * uiScale;
        float scaledHeight = boxHeight * uiScale;

        targetLeft = scaledCenterX - scaledWidth * 0.5f;
        targetRight = scaledCenterX + scaledWidth * 0.5f;
        targetTop = scaledCenterY - scaledHeight * 0.5f;
        targetBottom = scaledCenterY + scaledHeight * 0.5f;

        x1 = scaledCenterX; x2 = scaledCenterX;
        y1 = scaledCenterY; y2 = scaledCenterY;
    }

    private void Draw()
    {
        bool visible = phase > -1;

        float scaledBorder = borderThickness * uiScale;
        Vector2 scaledShadowOffset = shadowOffset * uiScale;

        SetRect(boxRect, x1, y1, x2, y2);
        SetRect(shadowRect, x1 + scaledShadowOffset.x, y1 + scaledShadowOffset.y, x2 + scaledShadowOffset.x, y2 + scaledShadowOffset.y);
        SetRect(borderTop, x1, y1 - scaledBorder, x2, y1);
        SetRect(borderBottom, x1, y2, x2, y2 + scaledBorder);
        SetRect(borderLeft, x1, y1, x1 + scaledBorder, y2);
        SetRect(borderRight, x2 - scaledBorder, y1, x2, y2);

        SetActive(boxRect, visible);
        SetActive(shadowRect, visible);
        SetActive(borderTop, visible);
        SetActive(borderBottom, visible);
        SetActive(borderLeft, visible);
        SetActive(borderRight, visible);

        if (dialogueText != null)
        {
            bool showText = phase == 1;
            dialogueText.gameObject.SetActive(showText);

            if (showText)
            {
                var rt = dialogueText.rectTransform;
                rt.anchoredPosition = new Vector2(textX * uiScale, -textY * uiScale);
                rt.sizeDelta = new Vector2(TextWidth * uiScale, rt.sizeDelta.y);

                string full = text[textCurrent];
                int len = full.Length;
                if (charCurrent < len) charCurrent += CHAR_SPEED;
                int shown = Mathf.Clamp((int)charCurrent, 0, len);
                dialogueText.text = full.Substring(0, shown);
            }
        }

        UpdateOverlapAlphaTarget(visible);
        StepFade();
    }

    private void UpdateOverlapAlphaTarget(bool visible)
    {
        if (!visible || boxRect == null || playerTransform == null || fadeRenderers == null)
        {
            targetAlpha = NORMAL_ALPHA;
            return;
        }

        Camera cam = playerViewCamera != null ? playerViewCamera : Camera.main;
        if (cam == null)
        {
            targetAlpha = NORMAL_ALPHA;
            return;
        }

        Vector2 playerScreenPos = cam.WorldToScreenPoint(playerTransform.position);
        bool overlapping = RectTransformUtility.RectangleContainsScreenPoint(boxRect, playerScreenPos, canvasCamera);

        targetAlpha = overlapping ? overlapAlpha : NORMAL_ALPHA;
    }

    private void StepFade()
    {
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed);
        SetAlpha(currentAlpha);
    }

    private void SetAlpha(float alpha)
    {
        foreach (var cr in fadeRenderers)
        {
            if (cr != null) cr.SetAlpha(alpha);
        }
    }

    private static CanvasRenderer GetCanvasRenderer(RectTransform rt)
    {
        return rt != null ? rt.GetComponent<CanvasRenderer>() : null;
    }

    private static void SetRect(RectTransform rt, float left, float top, float right, float bottom)
    {
        if (rt == null) return;
        rt.anchoredPosition = new Vector2(left, -top);
        rt.sizeDelta = new Vector2(right - left, bottom - top);
    }

    private static void SetActive(RectTransform rt, bool active)
    {
        if (rt == null) return;
        if (rt.gameObject.activeSelf != active) rt.gameObject.SetActive(active);
    }

    private static void EnsureTopLeftPivot(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
    }
}