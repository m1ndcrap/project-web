using System.Collections;
using UnityEngine;

public class BubbleSpawner : MonoBehaviour
{
    [SerializeField] public Sprite[] bubbleFrames;
    [SerializeField] public GameObject bubblePrefab;
    public PlayerStep player;
    public float frameRate = 20f;
    public int alarmDelayFrames = 10;
    private BubbleParticle[] bubbles;
    private const int COUNT = 8;
    private static readonly float[] xOffsetMagnitudes = { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1.0f };
    private static readonly float[] yOffsets = { 0f, 0.0779f, 0.1559f, 0.2338f, 0.3117f };
    private static readonly float[] scaleOpts = { 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1.0f };

    void Start()
    {
        bubbles = new BubbleParticle[COUNT];

        for (int i = 0; i < COUNT; i++)
        {
            GameObject go = Instantiate(bubblePrefab, transform);
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();

            if (player != null)
            {
                SpriteRenderer playerSr = player.GetComponent<SpriteRenderer>();
                if (playerSr != null) sr.sortingOrder = playerSr.sortingOrder + 1;
            }

            bubbles[i] = new BubbleParticle(sr);
            Randomize(bubbles[i]);
            float evenSpread = (13f / COUNT) * i;
            float jitter = Random.Range(-0.5f, 0.5f);
            bubbles[i].frameIndex = Mathf.Clamp(evenSpread + jitter, 0f, 12.9f);
        }
    }

    void Update()
    {
        float frameStep = frameRate * Time.deltaTime;

        for (int i = 0; i < COUNT; i++)
        {
            BubbleParticle b = bubbles[i];

            if (!b.waiting)
            {
                b.frameIndex += frameStep * 0.5f;

                if (b.frameIndex >= 13f)
                {
                    b.frameIndex = 13f;

                    if (!b.alarmSet)
                    {
                        b.alarmSet = true;
                        StartCoroutine(AlarmRoutine(b));
                    }
                }
            }

            int frame = Mathf.Clamp(Mathf.FloorToInt(b.frameIndex), 0, bubbleFrames.Length - 1);
            b.renderer.sprite = bubbleFrames[frame];
            b.renderer.transform.position = b.position;
            b.renderer.transform.localScale = Vector3.one * b.scale;
        }
    }

    IEnumerator AlarmRoutine(BubbleParticle b)
    {
        b.waiting = true;
        float baseDelay = alarmDelayFrames / frameRate;
        float jitterDelay = Random.Range(0f, 8f / frameRate);
        yield return new WaitForSeconds(baseDelay + jitterDelay);
        Randomize(b);
        b.frameIndex = 0f;
        b.waiting = false;
        b.alarmSet = false;
    }

    void Randomize(BubbleParticle b)
    {
        float sign = Random.value > 0.5f ? 2.488f : -2.488f;
        float magnitude = xOffsetMagnitudes[Random.Range(0, xOffsetMagnitudes.Length)];
        float randX = transform.position.x + sign * magnitude;
        float randY = transform.position.y + yOffsets[Random.Range(0, yOffsets.Length)];
        b.position = new Vector3(randX, randY, 0f);
        b.frameIndex = Random.Range(0f, 13f);
        b.scale = 0.65f * scaleOpts[Random.Range(0, scaleOpts.Length)];
    }

    private class BubbleParticle
    {
        public SpriteRenderer renderer;
        public Vector3 position;
        public float frameIndex;
        public float scale;
        public bool alarmSet;
        public bool waiting;
        public BubbleParticle(SpriteRenderer sr) { renderer = sr; }
    }
}