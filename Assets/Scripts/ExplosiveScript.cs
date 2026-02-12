using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class ExplosiveScript : MonoBehaviour
{
    public int phase = 0;
    private bool destroyed = false;
    private Animator anim;
    private AudioSource audioSrc;
    [SerializeField] private AudioClip sndExplosion1;
    [SerializeField] private AudioClip sndExplosion2;
    private int alarm1 = 0;
    [SerializeField] private bool createAnotherTrigger = false;
    [SerializeField] private GameObject nextTrigger;
    private GameObject explosion;
    private SpriteRenderer explosionSpriteRenderer;
    private Animator explosionAnimator;
    [SerializeField] private Sprite explosionSprite;
    [SerializeField] private RuntimeAnimatorController explosionAnimatorController;
    public bool bigExplosion = false;
    private float explosionScaleMultiplier = 8f;

    void Start()
    {
        anim = GetComponent<Animator>();
        audioSrc = GetComponent<AudioSource>();

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
            alarm1 -= 1;
        else
        {
            if (phase == 1)
            {
                AudioClip[] clips = { sndExplosion1, sndExplosion2 };
                audioSrc.PlayOneShot(clips[Random.Range(0, clips.Length)]);

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

        if (bigExplosion)
            phase = 4;

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
                float normalizedTime = stateInfo.normalizedTime;
                float scaleProgress = Mathf.Clamp01(normalizedTime);
                float currentScale = 1f + (explosionScaleMultiplier * scaleProgress);
                explosion.transform.localScale = Vector3.one * currentScale;

                //// Optional: Track frame changes for more granular control
                //AnimatorClipInfo[] clipInfo = explosionAnimator.GetCurrentAnimatorClipInfo(0);
                //if (clipInfo.Length > 0)
                //{
                //    float clipLength = clipInfo[0].clip.length;
                //    float frameRate = clipInfo[0].clip.frameRate;
                //    int currentFrame = Mathf.FloorToInt(normalizedTime * clipLength * frameRate);

                //    if (currentFrame != lastAnimationFrame)
                //    {
                //        lastAnimationFrame = currentFrame;
                //        // Scale increases with each frame
                //        Debug.Log($"Explosion frame: {currentFrame}, Scale: {currentScale}");
                //    }
                //}
            }
        }
    }
}