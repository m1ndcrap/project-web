using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PumpkinSpinner : MonoBehaviour
{
    public PlayerStep player;
    public Animator animator;
    public AudioSource audioSource;
    public AudioClip pumpkinBoom;
    [SerializeField] public AudioClip sndGLaugh1;
    [SerializeField] public AudioClip sndGLaugh2;
    [SerializeField] public AudioClip sndGLaugh3;
    float[] attractAcc = new float[2];
    public float hspeed;
    private float vspeed;
    public int dir = 1;
    public bool airborne = false;
    int phase = 0;
    int hit = 3;
    bool canHit = true;
    float xstart;
    float targX;

    void Start()
    {
        player = FindObjectOfType<PlayerStep>();
        attractAcc[0] = 0.45f;
        attractAcc[1] = 0.15f;
        xstart = transform.position.x;
        targX = player.transform.position.x;
        player.spiderSense = true;
        player.trigger = true;
        player.alarm4 = 60;
        transform.rotation = Quaternion.identity;
    }

    void Update()
    {
        if (hit < 0) hit = 0;

        if (phase == 0)
        {
            HandleHoming();
            ApplyMovement();
        }
        else if (phase == 1)
        {
            Explode();
        }
    }

    void HandleHoming()
    {
        Vector2 pos = transform.position;

        int playerX = Sign(player.transform.position.x - pos.x);
        int playerY = Sign(player.transform.position.y - pos.y);

        bool movX = Sign(hspeed) == playerX;
        bool movY = Sign(vspeed) == playerY;

        hspeed += attractAcc[movX ? 1 : 0] * playerX;
        vspeed += attractAcc[movY ? 1 : 0] * playerY;

        transform.Rotate(0, 0, 30f * dir * Time.deltaTime * 60f);
    }

    void ApplyMovement()
    {
        transform.position += new Vector3(0.02f * hspeed, 0.02f * vspeed, 0f) * Time.deltaTime * 60f;
    }

    void Explode()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        hspeed = 0;
        vspeed = 0;

        if (stateInfo.IsName("SpinnerNormal"))
        {
            transform.localScale = Vector3.one * 1.4f;
            audioSource.PlayOneShot(pumpkinBoom, 1f);
            animator.Play("SpinnerBoom");
        }

        if (stateInfo.IsName("SpinnerBoom") && stateInfo.normalizedTime >= 1f)
        {
            Destroy(gameObject);
        }
    }

    void TriggerExplosion()
    {
        if (phase != 0) return;
        phase = 1;
        AudioClip[] clips = { sndGLaugh1, sndGLaugh2, sndGLaugh3 };
        audioSource.PlayOneShot(clips[UnityEngine.Random.Range(0, clips.Length)]);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStep p = other.GetComponent<PlayerStep>();

            if (hit > 0 && canHit)
            {
                hit--;
                canHit = false;
                Invoke(nameof(ResetHit), 5f / 60f);
            }

            if (p.pState != PlayerStep.PlayerState.death && phase == 0 && hit == 0)
            {
                if (player.pState == PlayerStep.PlayerState.death) return;

                float dir = 0;
                dir = (transform.position.x < player.transform.position.x) ? 1f : -1f;
                /*
                if (airborne)
                    dir = player.sprite.flipX ? 1f : -1f;
                else
                    dir = (xstart > targX) ? -1 : 1;*/

                player.rb.velocity = new Vector2(dir * 2f, 5f);
                player.anim.speed = 1f;
                player.combo = 0;
                player.pState = PlayerStep.PlayerState.hurt;

                PlayerStep.MovementState mstate = PlayerStep.MovementState.launched;
                player.anim.SetInteger("mstate", (int)mstate);

                player.health -= 4;
                player.healthbar.UpdateHealthBar(player.health, player.maxHealth);

                AudioClip[] clips = { player.sndHurt, player.sndHurt2, player.sndHurt3 };
                player.audioSrc.PlayOneShot(clips[Random.Range(0, clips.Length)]);

                AudioClip[] clips2 = { sndGLaugh1, sndGLaugh2, sndGLaugh3 };
                audioSource.PlayOneShot(clips2[Random.Range(0, clips2.Length)]);
                TriggerExplosion();
            }
        }
        else if (other.CompareTag("Ground"))
        {
            if (hit > 0 && canHit)
            {
                hit -= 1;
                canHit = false;
                Invoke(nameof(ResetHit), 5f / 60f);
            }

            if (phase == 0 && hit == 0)
                phase = 1;
        }
        else if (other.CompareTag("Web"))
        {
            if (phase == 0)
            {
                Destroy(other.gameObject);
                phase = 1;
            }
        }
    }

    void ResetHit()
    {
        canHit = true;
    }

    int Sign(float v)
    {
        if (v > 0) return 1;
        if (v < 0) return -1;
        return 0;
    }
}