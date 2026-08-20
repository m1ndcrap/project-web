using UnityEngine;


/// <summary>
/// Represents a homing pumpkin projectile that tracks the player, interacts with the environment, and triggers visual
/// and audio effects upon collision or explosion.
/// </summary>


public class PumpkinSpinner : MonoBehaviour
{
    public PlayerStep player;
    public GoblinStep goblin;
    public Animator animator;
    public AudioClip pumpkinLaunch;
    public AudioClip pumpkinFly;
    public AudioClip pumpkinBoom;
    float[] attractAcc = new float[2];
    public float hspeed;
    private float vspeed;
    public int dir = 1;
    int phase = 0;
    int hit = 2;
    bool canHit = true;
    float xstart;
    float targX;
    int prevHSign;
    int prevVSign;




    void Start()
    {
        player = FindObjectOfType<PlayerStep>();
        goblin = FindObjectOfType<GoblinStep>();
        attractAcc[0] = 0.45f;
        attractAcc[1] = 0.15f;
        xstart = transform.position.x;
        targX = player.transform.position.x;
        player.spiderSense = true;
        player.trigger = true;
        player.alarm4 = 60;
        transform.rotation = Quaternion.identity;

        prevHSign = Sign(hspeed);
        prevVSign = Sign(vspeed);
        SfxPlayer.Instance.PlayClipAtPointMatched(pumpkinLaunch, transform.position, 0.25f);
        SfxPlayer.Instance.PlayClipAtPointMatched(pumpkinFly, transform.position, 0.25f);
    }




    void Update()
    {
        if (phase == 0)
        {
            HandleHoming();
            transform.position += new Vector3(0.02f * hspeed, 0.02f * vspeed, 0f) * Time.deltaTime * 60f;
        }
        else if (phase == 1)
        {
            Explode();
        }
    }




    /// <summary>
    /// Updates the object's velocity and orientation to home toward the player's position, adjusting movement direction
    /// and triggering sound effects when direction reversals occur.
    /// </summary>
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


        int currentHSign = Sign(hspeed);
        int currentVSign = Sign(vspeed);
        bool hFlipped = currentHSign != 0 && prevHSign != 0 && currentHSign != prevHSign;
        bool vFlipped = currentVSign != 0 && prevVSign != 0 && currentVSign != prevVSign;

        if (hFlipped || vFlipped)
        {
            SfxPlayer.Instance.PlayClipAtPointMatched(pumpkinFly, transform.position, 0.25f);
        }

        prevHSign = currentHSign;
        prevVSign = currentVSign;
    }




    /// <summary>
    /// Triggers the explosion sequence for the object, updating its state and playing the associated animation and
    /// sound effects.
    /// </summary>
    void Explode()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        hspeed = 0;
        vspeed = 0;

        if (stateInfo.IsName("SpinnerNormal"))
        {
            transform.localScale = Vector3.one * 1.4f;
            SfxPlayer.Instance.PlayClipAtPointMatched(pumpkinBoom, transform.position);
            animator.Play("SpinnerBoom");
        }

        if (stateInfo.IsName("SpinnerBoom") && stateInfo.normalizedTime >= 1f)
        {
            Destroy(gameObject);
        }
    }
    



    /// <summary>
    /// Triggers the explosion sequence for the goblin if it has not already started.
    /// </summary>
    void TriggerExplosion()
    {
        if (phase != 0) return;
        phase = 1;
        goblin.AudioController.PlayRandom(goblin.sndGLaugh1, goblin.sndGLaugh2, goblin.sndGLaugh3);
    }




    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (hit > 0 && canHit)
            {
                hit--;
                canHit = false;
                Invoke(nameof(ResetHit), 5f / 60f);
            }

            if (player.pState != PlayerStep.PlayerState.death && phase == 0 && hit == 0)
            {
                // Push the player away from wherever this spinner hit them
                float knockbackDir = (transform.position.x < player.transform.position.x) ? 1f : -1f;
                player.rb.velocity = new Vector2(knockbackDir * 1.5f, 0f);
                player.AnimationDriver.Speed = 1f;
                player.combo = 0;
                player.pState = PlayerStep.PlayerState.hurt;

                // Pick one of the two hurt animations at random
                PlayerStep.MovementState mstate = Random.Range(0, 2) == 0
                    ? PlayerStep.MovementState.hurt1
                    : PlayerStep.MovementState.hurt2;

                player.AnimationDriver.SetMovementState((int)mstate);

                player.health -= 2;
                player.healthbar.UpdateHealthBar(player.health, player.maxHealth);

                player.AudioController.PlayRandom(player.sndHurt, player.sndHurt2, player.sndHurt3);
                goblin.AudioController.PlayRandom(goblin.sndGLaugh1, goblin.sndGLaugh2, goblin.sndGLaugh3);
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




    /// <summary>
    /// Resets the hit state, allowing the object to be hit again.
    /// </summary>
    void ResetHit()
    {
        canHit = true;
    }




    /// <summary>
    /// Determines the sign of the specified single-precision floating-point value.
    /// </summary>
    /// <param name="v">The value to evaluate.</param>
    /// <returns>1 if the value is greater than zero; -1 if the value is less than zero; 0 if the value is zero.</returns>
    int Sign(float v)
    {
        if (v > 0) return 1;
        if (v < 0) return -1;
        return 0;
    }
}