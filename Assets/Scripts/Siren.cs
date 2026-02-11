using UnityEngine;

public class BackgroundSiren : MonoBehaviour
{
    [SerializeField] private Sprite baseSprite;
    [SerializeField] private Sprite flashSprite;
    [SerializeField] private float fadeSpeed = 0.1f;
    private float alpha = 0f;
    private int phase = 0;
    private GameObject baseObject;
    private GameObject flashObject;
    private SpriteRenderer baseSpriteRenderer;
    private SpriteRenderer flashSpriteRenderer;

    void Start()
    {
        baseObject = new GameObject("BaseSiren");
        baseObject.transform.SetParent(transform);
        baseObject.transform.localPosition = Vector3.zero;
        baseObject.transform.localRotation = Quaternion.identity;
        baseObject.transform.localScale = Vector3.one;
        baseSpriteRenderer = baseObject.AddComponent<SpriteRenderer>();
        baseSpriteRenderer.sprite = baseSprite;
        baseSpriteRenderer.sortingOrder = 0;
        Color baseColor = Color.white;
        baseColor.a = 0.8f;
        baseSpriteRenderer.color = baseColor;
        flashObject = new GameObject("FlashSiren");
        flashObject.transform.SetParent(transform);
        flashObject.transform.localPosition = Vector3.zero;
        flashObject.transform.localRotation = Quaternion.identity;
        flashObject.transform.localScale = Vector3.one;
        flashSpriteRenderer = flashObject.AddComponent<SpriteRenderer>();
        flashSpriteRenderer.sprite = flashSprite;
        flashSpriteRenderer.sortingOrder = 1;
        alpha = 0f;
        phase = 0;
    }

    void Update()
    {
        if (phase == 0)
        {
            if (alpha < 1f)
            {
                alpha += fadeSpeed;
            }
            else
            {
                alpha = 1f;
                phase = 1;
            }
        }
        else if (phase == 1)
        {
            if (alpha > 0f)
            {
                alpha -= fadeSpeed;
            }
            else
            {
                alpha = 0f;
                phase = 0;
            }
        }

        Color flashColor = Color.white;
        flashColor.a = alpha * 0.8f;
        flashSpriteRenderer.color = flashColor;
    }
}