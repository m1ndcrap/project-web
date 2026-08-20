using UnityEngine;


/// <summary>
/// Represents a projectile fired by the glider. Handles movement, collision detection, and visual
/// effects such as dissolving and drawing a circle upon impact or disappearance.
/// </summary>


public class GliderBullet : MonoBehaviour
{
    private PlayerStep player;
    public SpriteRenderer spriteRenderer;
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
        direction = Mathf.Atan2(transform.position.y - player.transform.position.y, player.transform.position.x - transform.position.x) * Mathf.Rad2Deg;
        direction2 = (player.transform.position - transform.position).normalized;
        float angle = Mathf.Atan2(direction2.y, direction2.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        SfxPlayer.Instance.PlayClipAtPointMatched(bulletSound, transform.position);
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




    /// <summary>
    /// Updates the object's position based on its current direction and speed.
    /// </summary>
    void Move()
    {
        Vector2 dirVec = AngleToVector(direction);
        transform.position += (Vector3)(dirVec * speed * Time.deltaTime * 60f);
    }




    /// <summary>
    /// Performs a dissolve effect on the current object, gradually increasing transparency and radius until the object
    /// is destroyed.
    /// </summary>
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




    /// <summary>
    /// Updates the drawing position based on the specified distance and the current direction and transform state.
    /// </summary>
    /// <param name="distance">The distance from the current position at which to calculate the new drawing position. Must be a non-negative
    /// value.</param>
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
            player.AnimationDriver.Speed = 1f;
            player.combo = 0;
            player.pState = PlayerStep.PlayerState.hurt;

            // Pick one of the two hurt animations at random
            PlayerStep.MovementState mstate = Random.Range(0, 2) == 0
                ? PlayerStep.MovementState.hurt1
                : PlayerStep.MovementState.hurt2;

            player.AnimationDriver.SetMovementState((int)mstate);
            player.AudioController.PlayRandom(player.sndQuickHit, player.sndQuickHit2);

            player.health -= 2;
            player.healthbar.UpdateHealthBar(player.health, player.maxHealth);

            player.AudioController.PlayRandom(player.sndHurt, player.sndHurt2, player.sndHurt3);

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




    /// <summary>
    /// Converts an angle in degrees to a 2D unit vector pointing in the corresponding direction.
    /// </summary>
    /// <param name="angle">The angle, in degrees, measured clockwise from the positive X-axis.</param>
    /// <returns>A <see cref="Vector2"/> representing the unit vector in the direction of the specified angle.</returns>
    Vector2 AngleToVector(float angle)
    {
        float rad = angle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), -Mathf.Sin(rad));
    }




    /// <summary>
    /// Draws a circle at the specified position with the given radius and transparency.
    /// </summary>
    /// <param name="center">The center position of the circle in world coordinates.</param>
    /// <param name="radius">The radius of the circle. Must be a non-negative value.</param>
    /// <param name="alpha">The transparency of the circle, where 0 is fully transparent and 1 is fully opaque. Values outside the range [0,
    /// 1] may produce undefined results.</param>
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
            Vector2 pos = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            circleRenderer.SetPosition(i, center + pos);
        }
    }
}