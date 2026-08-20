using UnityEngine;


/// <summary>
/// Represents a projectile launched by a goblin that targets the player, follows an arc trajectory, and explodes
/// upon collision with the player, ground, or web objects.
/// </summary>


public class PumpkinProjectile : MonoBehaviour
{
    public PlayerStep player;
    private GoblinStep goblin;


    [SerializeField] public Animator animator;
    [SerializeField] public AudioClip pumpkinBoom;


    [Header("Glider Throw")]
    [Tooltip("Turn on for bombs thrown while the goblin is riding the glider. The arc is sized from the real distance to the player instead of a fixed width, so the bomb actually lands where the player was when it was thrown.")]
    public bool thrownFromGlider = false;


    [Tooltip("How long a glider throw takes to reach the player's position, in seconds.")]
    [SerializeField] private float gliderThrowDuration = 0.9f;


    [Tooltip("How tall the glider throw's arc is, as a fraction of the distance travelled. 0.35 gives a lob, 0 is a straight line.")]
    [SerializeField] private float gliderThrowArcScale = 0.35f;


    private bool hasDealtDamage = false;
    public int dir = 1;
    float i = 0;
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




    /// <summary>
    /// Moves the bomb along its arc toward wherever the player was standing when it was thrown.
    /// </summary>
    void HandleMovement()
    {
        if (thrownFromGlider)
        {
            transform.position = GliderThrowPosition();
            transform.Rotate(0, 0, 2f * -dir * Time.deltaTime * 60f);
            return;
        }

        Vector3 pos = transform.position;

        float dist = Mathf.Abs(targX - xstart);
        float halfSpan = dist / 2f;
        float travelSpeed = 5.4f;

        i += travelSpeed * Time.deltaTime;
        float t = dist > 0f ? i / dist : 1f;

        float arcHeight = Mathf.Max(0.6f, halfSpan * 0.35f);
        float arc = arcHeight * 4f * t * (1f - t);

        pos.x = Mathf.LerpUnclamped(xstart, targX, t);
        pos.y = Mathf.LerpUnclamped(ystart, targY, t) + arc;

        float rotDir = xstart > targX ? 1f : -1f;
        transform.Rotate(0, 0, 2f * rotDir * Time.deltaTime * 60f);

        transform.position = pos;
    }




    /// <summary>
    /// Works out where a glider-thrown bomb should be this frame.
    /// </summary>
    private Vector3 GliderThrowPosition()
    {
        i += Time.deltaTime / gliderThrowDuration;
        float t = Mathf.Clamp01(i);

        Vector2 start = new Vector2(xstart, ystart);
        Vector2 target = new Vector2(targX, targY);

        // Arc scales with the real distance covered, so a short drop stays flat and a long throw lobs.
        float arcHeight = Vector2.Distance(start, target) * gliderThrowArcScale;
        float arc = arcHeight * 4f * t * (1f - t);

        Vector2 flat = Vector2.Lerp(start, target, t);

        // Landed on the target without hitting anything on the way, so detonate here.
        if (t >= 1f) TriggerExplosion();

        return new Vector3(flat.x, flat.y + arc, transform.position.z);
    }




    /// <summary>
    /// Handles the explosion sequence for the pumpkin object, including triggering the explosion animation, playing the
    /// associated sound effect, and destroying the object when the animation completes.
    /// </summary>
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




    /// <summary>
    /// Initiates the explosion sequence if it has not already started.
    /// </summary>
    void TriggerExplosion()
    {
        if (phase != 0) return;

        phase = 1;
    }




    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (!hasDealtDamage && player.pState != PlayerStep.PlayerState.death)
            {
                hasDealtDamage = true;

                // Launch the player away from the blast
                float knockbackDir = (transform.position.x < player.transform.position.x) ? 1f : -1f;
                player.rb.velocity = new Vector2(knockbackDir * 2f, 5f);
                player.AnimationDriver.Speed = 1f;
                player.combo = 0;
                player.pState = PlayerStep.PlayerState.hurt;

                player.launchGroundGrace = 0.2f;
                player.launchTechTimer = 0f;
                player.AnimationDriver.SetMovementState((int)PlayerStep.MovementState.launched);

                player.health -= 3;
                player.healthbar.UpdateHealthBar(player.health, player.maxHealth);

                player.AudioController.PlayRandom(player.sndHurt, player.sndHurt2, player.sndHurt3);
                goblin.AudioController.PlayRandom(goblin.sndGLaugh1, goblin.sndGLaugh2, goblin.sndGLaugh3);
            }

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