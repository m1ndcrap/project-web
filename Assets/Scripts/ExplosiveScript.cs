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
    }
}