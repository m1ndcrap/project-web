using Unity.VisualScripting;
using UnityEngine;

public class GliderBullet : MonoBehaviour
{
    private PlayerStep player;
    public SpriteRenderer spriteRenderer;
    public AudioSource audioSource;

    public AudioClip bulletSound;

    float direction;
    Vector2 direction2;
    float speed = 0.1f;
    int phase = 0;

    float alpha = 0f;
    float rad = 0f;

    Vector2 drawPos;

    public LineRenderer circleRenderer;
    public int circleSegments = 32;

    void Start()
    {
        player = FindObjectOfType<PlayerStep>();
        direction = PointDirection(transform.position, player.transform.position);
        direction2 = (player.transform.position - transform.position).normalized;
        float angle = Mathf.Atan2(direction2.y, direction2.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        //transform.rotation = Quaternion.Euler(0, 0, direction);
        audioSource.PlayOneShot(bulletSound, 1f);
        player.trigger = true;
        player.alarm4 = 60;
        circleRenderer.positionCount = circleSegments;
        circleRenderer.loop = true;
        circleRenderer.enabled = false;
    }

    void Update()
    {
        if (phase == 0)
        {
            Move();
        }
        else if (phase == 1)
        {
            Dissolve();
        }
    }

    void Move()
    {
        Vector2 dirVec = AngleToVector(direction);
        transform.position += (Vector3)(dirVec * speed * Time.deltaTime * 60f);
    }

    void Dissolve()
    {
        speed = 0f;

        if (alpha < 1f)
        {
            alpha += 0.1f;
            rad += 0.01f;
        }
        else
        {
            Destroy(gameObject);
        }

        if (phase == 1)
        {
            DrawCircle(drawPos, rad, alpha);
        }
    }

    void UpdateDrawPos(float distance)
    {
        float rad = direction * Mathf.Deg2Rad;
        float xOff = distance * Mathf.Cos(rad) * transform.localScale.x;
        float yOff = distance * Mathf.Sin(rad) * transform.localScale.x;
        drawPos = new Vector2(transform.position.x + xOff, transform.position.y - yOff);
    }

    void LateUpdate()
    {
        Color c = spriteRenderer.color;

        if (phase == 0)
            c.a = 1f;
        else
            c.a = 0f;

        spriteRenderer.color = c;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (phase != 0) return;

        if (other.CompareTag("Player"))
        {
            if (player.pState == PlayerStep.PlayerState.death) return;

            float dir = (transform.position.x > player.transform.position.x) ? -1 : 1;

            player.rb.velocity = new Vector2(dir, 0f);
            player.anim.speed = 1f;
            player.combo = 0;
            player.pState = PlayerStep.PlayerState.hurt;

            PlayerStep.MovementState mstate;
            int hitIndex = UnityEngine.Random.Range(0, 2);

            if (hitIndex == 0)
                mstate = PlayerStep.MovementState.hurt1;
            else
                mstate = PlayerStep.MovementState.hurt2;

            player.anim.SetInteger("mstate", (int)mstate);

            AudioClip[] clips2 = { player.sndQuickHit, player.sndQuickHit2 };
            int index2 = Random.Range(0, clips2.Length);
            if (index2 < clips2.Length) { player.audioSrc.PlayOneShot(clips2[index2]); }

            player.health -= 2;
            player.healthbar.UpdateHealthBar(player.health, player.maxHealth);

            AudioClip[] clips = { player.sndHurt, player.sndHurt2, player.sndHurt3 };
            player.audioSrc.PlayOneShot(clips[Random.Range(0, clips.Length)]);

            UpdateDrawPos(1f);
            phase = 1;
        }
        else if (other.CompareTag("Ground"))
        {
            UpdateDrawPos(1f);
            phase = 1;
        }
    }

    void OnBecameInvisible()
    {
        phase = 1;
    }

    float PointDirection(Vector2 from, Vector2 to)
    {
        return Mathf.Atan2(from.y - to.y, to.x - from.x) * Mathf.Rad2Deg;
    }

    Vector2 AngleToVector(float angle)
    {
        float rad = angle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), -Mathf.Sin(rad));
    }

    void DrawCircle(Vector2 center, float radius, float alpha)
    {
        circleRenderer.enabled = true;

        Color c = Color.white;
        c.a = alpha;
        circleRenderer.startColor = c;
        circleRenderer.endColor = c;

        for (int i = 0; i < circleSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / circleSegments;
            Vector2 pos = new Vector2(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius
            );

            circleRenderer.SetPosition(i, center + pos);
        }
    }
}