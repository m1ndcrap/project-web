using UnityEngine;

public class Shockwave : MonoBehaviour
{
    public int type = 0; // 0 = shockwave blast, 1 = shockwave beam

    private SpriteRenderer sr;
    private PlayerStep player;
    private Rigidbody2D rb;

    private float scaleX = 0f;
    private float scaleY = 0f;
    private float alpha = 1f;
    private int phase = 0;

    // Type 0 constants
    private const float BlastScaleMax = 6.5f;
    private const float BlastScaleSpeed = 0.325f;
    private const float BlastFadeSpeed = 0.05f;

    // Type 1 constants
    private const float BeamScaleMaxX = 1f;
    private const float BeamScaleSpeedX = 0.01f;
    private const float BeamScaleSpeedY = 0.025f;
    private const float BeamFadeSpeed = 0.025f;
    private const float BeamMoveSpeed = 10f;

    // Bounds
    private const float BoundLeft = 124.254f;
    private const float BoundTop = 9.822f;
    private const float BoundBottom = 6.831f;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        player = FindObjectOfType<PlayerStep>();
        player.trigger = true;
        player.alarm4 = 60;
        transform.localScale = new Vector3(0f, 0f, 1f);
        alpha = 1f;
    }

    void Update()
    {
        if (type == 0) UpdateBlast(); else if (type == 1) UpdateBeam();
    }

    void UpdateBlast()
    {
        if (scaleX < BlastScaleMax)
        {
            scaleX += BlastScaleSpeed;
            alpha -= BlastFadeSpeed;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        scaleY = scaleX;
        ApplyScaleAndAlpha(scaleX, scaleY);
    }

    void UpdateBeam()
    {
        if (phase == 0)
        {
            scaleX = 0.1f;
            scaleY = 0.25f;

            if (player != null)
            {
                Vector2 dir = (player.transform.position - transform.position).normalized;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
                if (rb != null) rb.velocity = dir * BeamMoveSpeed;
            }

            phase = 1;
        }

        if (phase == 1)
        {
            if (scaleX < BeamScaleMaxX)
            {
                scaleX += BeamScaleSpeedX;
                scaleY += BeamScaleSpeedY;
                alpha -= BeamFadeSpeed;
            }

            Vector3 pos = transform.position;

            if (pos.x <= BoundLeft || pos.y >= BoundTop || pos.y <= BoundBottom)
            {
                Destroy(gameObject);
                return;
            }
        }

        ApplyScaleAndAlpha(scaleX, scaleY);
    }

    void ApplyScaleAndAlpha(float sx, float sy)
    {
        transform.localScale = new Vector3(sx, sy, 1f);

        if (sr != null)
        {
            Color c = sr.color;
            c.a = Mathf.Clamp01(alpha);
            sr.color = c;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (type == 1 && phase == 1 && other.CompareTag("Player"))
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

            Destroy(gameObject);
        }

        if (type == 0 && other.CompareTag("Player"))
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
        }
    }

    void OnBecameInvisible()
    {
        Destroy(gameObject);
    }
}