using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PumpkinProjectile : MonoBehaviour
{
    public PlayerStep player;
    [SerializeField] public SpriteRenderer spriteRenderer;
    [SerializeField] public Animator animator;
    [SerializeField] public AudioSource audioSource;
    [SerializeField] public AudioClip pumpkinBoom;
    [SerializeField] public AudioClip sndGLaugh1;
    [SerializeField] public AudioClip sndGLaugh2;
    [SerializeField] public AudioClip sndGLaugh3;
    [SerializeField] public bool airborne = false;
    public int dir = 1;

    // Internal state
    float i = 0;
    bool ready = false;
    int phase = 0;

    float xstart;
    float ystart;
    float targX;

    

    void Start()
    {
        player = FindObjectOfType<PlayerStep>();
        xstart = transform.position.x;
        ystart = transform.position.y;

        targX = player.transform.position.x;

        player.trigger = true;
        player.alarm4 = 60;

        transform.rotation = Quaternion.identity;
    }

    void Update()
    {
        if (phase == 0)
        {
            HandleMovement();
        }
        else if (phase == 1)
        {
            HandleExplosion();
        }
    }

    void HandleMovement()
    {
        Vector3 pos = transform.position;

        if (airborne)
        {
            if (i < 10f)
            {
                pos.x += 0.1f * dir * Time.deltaTime * 60f;
                pos.y = ystart - (0.125f * (i * i));
                i += 0.1f;

                transform.Rotate(0, 0, -2f * dir);
            }
        }
        else
        {
            if (!ready)
            {
                i = -(int)(Mathf.Abs(targX - xstart) / 2f);
                ready = true;
            }

            if (ready && i < 4.93f)
            {
                pos.y = ystart - (0.0025f * (i * i));
                i += 0.1f;

                if (xstart > targX)
                {
                    pos.x -= 0.1f * Time.deltaTime * 60f;
                    transform.Rotate(0, 0, 2f);
                }
                else
                {
                    pos.x += 0.1f * Time.deltaTime * 60f;
                    transform.Rotate(0, 0, -2f);
                }
            }
        }

        transform.position = pos;
    }

    void HandleExplosion()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.IsName("PumpkinNormal"))
        {
            transform.localScale = Vector3.one * 1.4f;
            audioSource.PlayOneShot(pumpkinBoom, 1f);
            animator.Play("PumpkinBoom");
        }

        if (stateInfo.IsName("PumpkinBoom") && stateInfo.normalizedTime >= 1f)
        {
            Destroy(gameObject);
        }
    }

    void TriggerExplosion()
    {
        if (phase != 0) return;

        phase = 1;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (phase != 0) return;

        if (other.CompareTag("Player"))
        {
            PlayerStep p = other.GetComponent<PlayerStep>();
            if (p.pState == PlayerStep.PlayerState.death) return;

/*
            if (airborne)
                p.enemyDir = dir;
            else
                p.enemyDir = (xstart > targX) ? -1 : 1;*/

            float origScale = Mathf.Abs(other.transform.localScale.x);
            other.transform.localScale = new Vector3(
                transform.position.x < other.transform.position.x ? -origScale : origScale,
                other.transform.localScale.y,
                other.transform.localScale.z
            );

            AudioClip[] clips = { sndGLaugh1, sndGLaugh2, sndGLaugh3 };
            audioSource.PlayOneShot(clips[UnityEngine.Random.Range(0, clips.Length)]);
            //p.LaunchHit(4);
            TriggerExplosion();
        }
        else if (other.CompareTag("Ground"))
        {
            TriggerExplosion();
        }
        else if (other.CompareTag("Web"))
        {
            Destroy(other.gameObject);
            TriggerExplosion();
        }
    }

    void OnBecameInvisible()
    {
        if (phase == 0)
            phase = 1;
    }
}