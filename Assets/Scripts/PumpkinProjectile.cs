using System.Collections;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

public class PumpkinProjectile : MonoBehaviour
{
    public PlayerStep player;
    [SerializeField] public Animator animator;
    [SerializeField] public AudioSource audioSource;
    [SerializeField] public AudioClip pumpkinBoom;
    [SerializeField] public AudioClip sndGLaugh1;
    [SerializeField] public AudioClip sndGLaugh2;
    [SerializeField] public AudioClip sndGLaugh3;
    [SerializeField] public bool airborne = false;
    public int dir = 1;
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
            if (player.pState == PlayerStep.PlayerState.death) return;

            float dir = 0;
            dir = (transform.position.x < player.transform.position.x) ? 1f : -1f;

            /*
            if (airborne)
                dir = (transform.position.x < player.transform.position.x) ? 1f : -1f;
            else
                dir = (xstart > targX) ? -1 : 1;*/

            player.rb.velocity = new Vector2(dir * 2f, 5f);
            player.anim.speed = 1f;
            player.combo = 0;
            player.pState = PlayerStep.PlayerState.hurt;

            PlayerStep.MovementState mstate = PlayerStep.MovementState.launched;
            player.anim.SetInteger("mstate", (int)mstate);

            player.health -= 3;
            player.healthbar.UpdateHealthBar(player.health, player.maxHealth);

            AudioClip[] clips = { player.sndHurt, player.sndHurt2, player.sndHurt3 };
            player.audioSrc.PlayOneShot(clips[Random.Range(0, clips.Length)]);

            AudioClip[] clips2 = { sndGLaugh1, sndGLaugh2, sndGLaugh3 };
            audioSource.PlayOneShot(clips2[Random.Range(0, clips2.Length)]);
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