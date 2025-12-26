using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PumpkinSpinner : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public SpriteRenderer spriteRenderer;
    public Animator animator;
    public AudioSource audioSource;

    [Header("Sprites")]
    public Sprite pumpkinBoomSprite;

    [Header("Sounds")]
    public AudioClip spiderSense;
    public AudioClip pumpkinBoom;
    public AudioClip[] goblinLaughs;

    // --- GameMaker variables ---
    float[] attractAcc = new float[2];
    public float hspeed;
    private float vspeed;

    int i = 0;
    public int dir = 1;
    public bool airborne = false;
    private bool ready = false;

    int phase = 0;
    float targX;

    int hit = 3;
    bool canHit = true;

    float xstart;

    void Start()
    {
        attractAcc[0] = 0.45f;
        attractAcc[1] = 0.15f;

        targX = player.position.x;
        xstart = transform.position.x;

        // Spider sense trigger
        PlayerStep p = player.GetComponent<PlayerStep>();
        p.spiderSense = true;
        audioSource.PlayOneShot(spiderSense, 1f);

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

        int playerX = Sign(player.position.x - pos.x);
        int playerY = Sign(player.position.y - pos.y);

        bool movX = Sign(hspeed) == playerX;
        bool movY = Sign(vspeed) == playerY;

        hspeed += attractAcc[movX ? 1 : 0] * playerX;
        vspeed += attractAcc[movY ? 1 : 0] * playerY;

        transform.Rotate(0, 0, 30f * dir * Time.deltaTime * 60f);
    }

    void ApplyMovement()
    {
        transform.position += new Vector3(
            hspeed,
            vspeed,
            0f
        ) * Time.deltaTime * 60f;
    }

    void Explode()
    {
        hspeed = 0;
        vspeed = 0;

        if (spriteRenderer.sprite != pumpkinBoomSprite)
        {
            spriteRenderer.sprite = pumpkinBoomSprite;
            transform.localScale = Vector3.one * 1.4f;

            audioSource.PlayOneShot(pumpkinBoom, 1f);
            animator.Play("PumpkinBoom");
        }

        if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
        {
            Destroy(gameObject);
        }
    }

    void TriggerExplosion()
    {
        if (phase != 0) return;

        phase = 1;
        audioSource.PlayOneShot(
            goblinLaughs[Random.Range(0, goblinLaughs.Length)], 0.7f
        );
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
                /*if (airborne)
                    p.enemyDir = dir;
                else
                    p.enemyDir = (xstart > targX) ? -1 : 1;*/

                float scale = Mathf.Abs(other.transform.localScale.x);
                other.transform.localScale = new Vector3(
                    transform.position.x < other.transform.position.x ? -scale : scale,
                    other.transform.localScale.y,
                    other.transform.localScale.z
                );

                //p.LaunchHit(2);
                TriggerExplosion();
            }
        }
        else if (other.CompareTag("Wall"))
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
