using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// A reusable health bar UI widget: fills a Slider based on current/max health, optionally follows
/// a target, and optionally fades out after a few seconds of inactivity. Covers two common setups:
/// <list type="bullet">
/// <item><description><b>Floating enemy/player bar</b>: assign <see cref="target"/> so the bar follows that character, and turn on <see cref="fadeWhenIdle"/> so it hides itself between hits.</description></item>
/// <item><description><b>Fixed boss bar</b>: leave <see cref="target"/> empty, position the bar directly in the Canvas, and turn off <see cref="fadeWhenIdle"/> so it stays visible for the whole fight.</description></item>
/// </list>
/// </summary>
/// 


public class HealthBar : MonoBehaviour
{
    [Header("Bar")]
    [Tooltip("The Slider whose value (0-1) represents the current health fraction.")]
    [SerializeField] private Slider slider;


    [Header("Follow Target (optional)")]
    [Tooltip("If assigned, this health bar follows the target's position every frame. Use this for a floating bar over an enemy or the player. Leave empty for a fixed-position bar, like a boss health bar anchored to the screen.")]
    [SerializeField] private Transform target;


    [Tooltip("Position offset from the target. Only used when Target is assigned.")]
    [SerializeField] private Vector3 offset;


    [Header("Auto-Fade")]
    [Tooltip("Turn this ON for bars that should hide themselves between hits, like enemy and player bars. Turn it OFF for bars that must stay visible the whole time, like a boss bar.")]
    [SerializeField] private bool fadeWhenIdle = true;


    [Tooltip("The CanvasGroup used to fade the bar. Required for fading, ignored otherwise.")]
    [SerializeField] private CanvasGroup canvasGroup;


    [Tooltip("How long the bar stays fully visible after a health update, before it starts fading out.")]
    public float visibleDuration = 3f;


    [Tooltip("How quickly the bar fades out once Visible Duration has elapsed.")]
    public float fadeSpeed = 6f;


    private float visibleTimer = 0f;




    /// <summary>
    /// Updates the health bar display to reflect the specified current and maximum health values.
    /// </summary>
    /// <param name="currentHealth">The current health value to display. Must be greater than or equal to 0 and less than or equal to <paramref name="maxHealth"/>.</param>
    /// <param name="maxHealth">The maximum possible health value. Must be greater than 0.</param>
    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        slider.value = currentHealth / maxHealth;

        // Any health change makes the bar fully visible again and restarts the fade countdown.
        if (fadeWhenIdle && canvasGroup != null)
        {
            visibleTimer = visibleDuration;
            canvasGroup.alpha = 1f;
        }
    }




    private void Update()
    {
        if (target != null)
            transform.position = target.position + offset;


        if (Camera.main != null)
            transform.rotation = Camera.main.transform.rotation;


        if (canvasGroup == null)
            return;


        // Fading is opt-in. A bar with a CanvasGroup attached but fading turned off just stays
        // fully visible, which is what a boss bar wants.
        if (!fadeWhenIdle)
        {
            canvasGroup.alpha = 1f;
            return;
        }


        if (visibleTimer > 0f)
        {
            visibleTimer -= Time.deltaTime;
            canvasGroup.alpha = 1f;
        }
        else
        {
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 0f, Time.deltaTime * fadeSpeed);
        }
    }
}