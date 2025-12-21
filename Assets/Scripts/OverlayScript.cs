using UnityEngine;

public class OverlayScript : MonoBehaviour
{
    private SpriteRenderer sr;
    private bool fading = false;
    private float fadeSpeed = 0.25f;

    void Start() { sr = GetComponent<SpriteRenderer>(); }

    void Update()
    {
        if (fading)
        {
            Color c = sr.color;
            c.a -= Time.deltaTime / fadeSpeed;
            sr.color = c;

            if (c.a <= 0f)
            {
                c.a = 0f;
                sr.color = c;
            }
        }

        if (!fading)
        {
            Color c = sr.color;
            c.a += Time.deltaTime / fadeSpeed;
            sr.color = c;

            if (c.a >= 1f)
            {
                c.a = 1f;
                sr.color = c;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision) { if (collision.gameObject.CompareTag("Player")) { fading = true; }}
    private void OnTriggerExit2D(Collider2D collision) { if (collision.gameObject.CompareTag("Player")) { fading = false; }}
}