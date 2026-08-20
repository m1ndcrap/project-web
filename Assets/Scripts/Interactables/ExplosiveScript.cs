using Framework.Core;
using UnityEngine;


/// <summary>
/// An explosive object that breaks and spawns an explosion effect once <see cref="phase"/> is
/// externally set to 1, similar to <see cref="BreakableSwitch"/> but with a spawned explosion FX
/// on top. Can optionally chain into a second, permanent "big explosion" that grows over time
/// instead of the normal small one, and can optionally activate a follow-up trigger object once
/// it breaks (for a sequence of explosions, for example).
/// </summary>


public class ExplosiveScript : MonoBehaviour
{
    /// <summary>0 = active/idle, 1 = triggered, 2 = spawning the explosion, 3 = broken/inactive with explosion playing, 4 = big explosion (set <see cref="bigExplosion"/> to reach this).</summary>
    [Tooltip("0 = active/idle. Set this to 1 from another script (e.g. on a hit) to trigger the explosion.")]
    public int phase = 0;
    private bool destroyed = false;
    private Animator anim;
    private AudioSource audioSrc;
    private AudioController audioController;
    [Header("Audio")]
    [Tooltip("One of these two clips is chosen at random when the explosion is triggered.")]
    [SerializeField] private AudioClip sndExplosion1;
    [SerializeField] private AudioClip sndExplosion2;
    [Tooltip("Played once when the big explosion (phase 4) starts.")]
    [SerializeField] private AudioClip sndBigExplosion;
    private bool bigExplosionTriggered = false;
    private int alarm1 = 0;
    [Header("Chained Trigger")]
    [Tooltip("If true, activates Next Trigger the moment this explosive breaks, useful for a sequence of explosives that go off one after another.")]
    [SerializeField] private bool createAnotherTrigger = false;
    [Tooltip("The object to activate once this explosive breaks. Only used if Create Another Trigger is true.")]
    [SerializeField] private GameObject nextTrigger;
    private GameObject explosion;
    private SpriteRenderer explosionSpriteRenderer;
    private Animator explosionAnimator;
    [Header("Explosion VFX")]
    [Tooltip("Sprite used for the spawned explosion effect.")]
    [SerializeField] private Sprite explosionSprite;
    [Tooltip("Animator Controller driving the spawned explosion sprite's animation.")]
    [SerializeField] private RuntimeAnimatorController explosionAnimatorController;
    [Header("Big Explosion")]
    [Tooltip("Set this to true (e.g. from another script) to escalate into a permanent, growing big explosion instead of the normal small one.")]
    public bool bigExplosion = false;
    [Tooltip("How much larger the big explosion grows by the time it finishes scaling up.")]
    [SerializeField] private float explosionScaleMultiplier = 8f;




    void Start()
    {
        anim = GetComponent<Animator>();
        audioSrc = GetComponent<AudioSource>();
        audioController = new AudioController(audioSrc);

        if (createAnotherTrigger)
        {
            nextTrigger.SetActive(false);
        }
    }




    void Update()
    {
        if (phase == 0) { anim.Play("ExplosiveActive"); }

        if (phase == 1 && !destroyed)
        {
            alarm1 = 10;
            destroyed = true;
        }

        if (alarm1 > 0)
        {
            alarm1 -= 1;
        }
        else
        {
            if (phase == 1)
            {
                audioController.PlayRandom(sndExplosion1, sndExplosion2);

                if (createAnotherTrigger)
                {
                    nextTrigger.SetActive(true);
                    createAnotherTrigger = false;
                }

                phase = 2;
            }
        }

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

        if (phase == 2)
        {
            explosion = new GameObject("Explosion");
            explosion.transform.SetParent(transform);
            explosion.transform.localPosition = Vector3.zero;
            explosion.transform.localRotation = Quaternion.identity;
            explosion.transform.localScale = new Vector3(1.15f, 1.15f, 1.15f);
            explosionSpriteRenderer = explosion.AddComponent<SpriteRenderer>();
            explosionSpriteRenderer.sprite = explosionSprite;
            explosionSpriteRenderer.sortingOrder = 1;
            explosionAnimator = explosion.AddComponent<Animator>();
            explosionAnimator.runtimeAnimatorController = explosionAnimatorController;
            phase = 3;
        }

        if (phase == 3)
        {
            anim.Play("ExplosiveInactive");

            if (explosionAnimator != null)
            {
                AnimatorStateInfo explosionStateInfo = explosionAnimator.GetCurrentAnimatorStateInfo(0);
                explosionAnimator.Play("Explosion");

                if (explosionStateInfo.IsName("Explosion") && explosionStateInfo.normalizedTime >= 1f)
                {
                    Destroy(explosion);
                    explosionAnimator = null;
                }
            }
        }

        if (bigExplosion) { phase = 4; }

        if (phase == 4)
        {
            if (explosion == null)
            {
                explosion = new GameObject("Explosion");
                explosion.transform.SetParent(transform);
                explosion.transform.localPosition = Vector3.zero;
                explosion.transform.localRotation = Quaternion.identity;
                explosion.transform.localScale = Vector3.one;
                explosionSpriteRenderer = explosion.AddComponent<SpriteRenderer>();
                explosionSpriteRenderer.sprite = explosionSprite;
                explosionSpriteRenderer.sortingLayerName = "Default";
                explosionSpriteRenderer.sortingOrder = 21;
                explosionAnimator = explosion.AddComponent<Animator>();
                explosionAnimator.runtimeAnimatorController = explosionAnimatorController;
            }

            if (explosionAnimator != null)
            {
                AnimatorStateInfo explosionStateInfo = explosionAnimator.GetCurrentAnimatorStateInfo(0);
                explosionAnimator.Play("ExplosionBig");

                if (!bigExplosionTriggered)
                {
                    audioSrc.PlayOneShot(sndBigExplosion);
                    bigExplosionTriggered = true;
                }

                float normalizedTime = stateInfo.normalizedTime;
                float scaleProgress = Mathf.Clamp01(normalizedTime);
                float currentScale = 1f + (explosionScaleMultiplier * scaleProgress);
                explosion.transform.localScale = Vector3.one * currentScale;
            }
        }
    }
}