using System.Collections;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

public class PumpkinProjectile : MonoBehaviour
{
    public PlayerStep player;
    private GoblinStep goblin;
    [SerializeField] public Animator animator;
    [SerializeField] public AudioClip pumpkinBoom;
    [SerializeField] public bool airborne = false;
    public int dir = 1;
    float i = 0;
    bool ready = false;
    int phase = 0;
    float xstart;
    float ystart;
    float targX;
    float targY;

    void Start()
    {
        player = FindObjectOfType<PlayerStep>();
        goblin = FindObjectOfType<GoblinStep>();
        xstart = transform.position.x;
        ystart = transform.position.y;
        targX = player.transform.position.x;
        targY = player.transform.position.y;
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
            pos.x += 0.1f * dir * Time.deltaTime * 60f;
            pos.y = ystart - (0.125f * (i * i));
            i += 0.1f;

            transform.Rotate(0, 0, -2f * dir);
        }
        else
        {
            if (!ready)
            {
                i = -(int)(Mathf.Abs(targX - xstart) / 2f);
                ready = true;
            }

            float halfSpan = Mathf.Abs(targX - xstart) / 2f;
            float t = halfSpan > 0f ? Mathf.InverseLerp(-halfSpan, halfSpan, i) : 1f;
            float baseline = Mathf.Lerp(ystart, targY, t);
            float arcHeight = Mathf.Max(0.6f, halfSpan * 0.35f);

            pos.y = baseline + arcHeight - (arcHeight / (halfSpan * halfSpan + 0.001f)) * (i * i);
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

        transform.position = pos;
    }

    void HandleExplosion()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.IsName("PumpkinNormal"))
        {
            transform.localScale = Vector3.one * 1.4f;
            SfxPlayer.Instance.PlayClipAtPointMatched(pumpkinBoom, transform.position);
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

            AudioClip[] clips2 = { goblin.sndGLaugh1, goblin.sndGLaugh2, goblin.sndGLaugh3 };
            goblin.audioSrc.PlayOneShot(clips2[Random.Range(0, clips2.Length)]);
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