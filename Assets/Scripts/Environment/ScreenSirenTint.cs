using UnityEngine;


/// <summary>
/// A script that controls the behavior of a screen siren tint effect, including its fading effect, pause duration, and alarm sound.
/// </summary>


public class ScreenSirenTint : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float fadeSpeed = 0.025f;
    [SerializeField] private float pauseDuration = 1f;
    [SerializeField] private AudioClip alarmSound;
    [SerializeField] private AudioSource audioSource;
    private float imageAlpha = 0f;
    private int phase = 0;
    private float timer = 0f;




    void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.priority = 20;
        }

        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = 20;

        imageAlpha = 0f;
        phase = 0;

        UpdateAlpha();
    }




    void Update()
    {
        if (phase == 0)
        {
            if (imageAlpha < 0.8f)
            {
                imageAlpha += fadeSpeed;
            }
            else
            {
                imageAlpha = 0.8f;
                phase = 1;
            }
            UpdateAlpha();
        }
        else if (phase == 1)
        {
            if (imageAlpha > 0f)
            {
                imageAlpha -= fadeSpeed;
            }
            else
            {
                imageAlpha = 0f;
                phase = 2;
            }

            UpdateAlpha();
        }
        else if (phase == 2)
        {
            timer = pauseDuration;
            phase = 3;
        }
        else if (phase == 3)
        {
            timer -= Time.deltaTime;

            if (timer <= 0f)
            {
                phase = 0;
            }
        }
    }




    /// <summary>
    /// Updates the alpha value of the sprite renderer based on the current imageAlpha.
    /// </summary>
    void UpdateAlpha()
    {
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = imageAlpha;
            spriteRenderer.color = color;
        }
    }




    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayAlarmSound();
        }
    }




    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayAlarmSound();
        }
    }




    /// <summary>
    /// Plays the alarm sound if the audio source and alarm sound are available and the audio source is not already playing.
    /// </summary>
    void PlayAlarmSound()
    {
        if (audioSource != null && alarmSound != null && !audioSource.isPlaying)
        {
            audioSource.clip = alarmSound;
            audioSource.loop = false;
            audioSource.Play();
        }
    }
}