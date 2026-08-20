using System;
using System.Collections;
using Framework.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using static GliderScript;


/// <summary>
/// Represents the Goblin boss enemy, managing its AI state machine, combat interactions, animation, and sound effects
/// during the boss fight. Handles transitions between aerial and ground phases, attacks, evasion, blocking, and
/// responses to player actions.
/// </summary>


public class GoblinStep : MonoBehaviour, IEnemyBarrier
{
    public Rigidbody2D rb;
    [SerializeField] public Animator anim;
    [SerializeField] public SpriteRenderer sprite;
    private BoxCollider2D coll;
    [SerializeField] private float dirX = 0f;
    [SerializeField] private bool setCustomStartingDir = false;
    [SerializeField] private LayerMask jumpableGround;
    [SerializeField] private LayerMask playerMask;




    /// <summary>Drives which Animator clip is playing, via the shared "mstate" integer parameter. Set through <see cref="AnimationDriver.SetMovementState"/>, not directly.</summary>
    public enum MovementState { crouching, throwing, jump, idle, falling, hurt1, hurt2, sprinting, punch1, punch2, blocking, death, breakweb1, breakweb2 }




    /// <summary>
    /// This boss's high-level AI state, driving the switch statement in <see cref="Update"/>.
    /// <list type="bullet">
    /// <item><description><b>on_glider</b>: riding the glider, throwing pumpkin bombs/spinners at the player from above.</description></item>
    /// <item><description><b>jump_to_platform</b>: leaping from the glider down onto a fixed platform to fight the player directly.</description></item>
    /// <item><description><b>engaged</b>: on the ground, closing distance, throwing bombs/spinners, or watching for an attack opening.</description></item>
    /// <item><description><b>attack</b>: mid-punch, closing the last bit of distance and dealing damage on the animation's impact frame.</description></item>
    /// <item><description><b>getting_hit</b>: playing a hit/break-free reaction before returning to engaged.</description></item>
    /// <item><description><b>blocking</b>: guarding, can't be hit normally, but can be countered. Returns to engaged once the block animation finishes.</description></item>
    /// <item><description><b>evade</b>: jumping a short distance away, possibly rushing back into an attack afterward.</description></item>
    /// <item><description><b>jump_to_glider</b>: leaping back up onto the glider to resume the aerial phase.</description></item>
    /// <item><description><b>death</b>: defeated, stops colliding with the player and settles under gravity (or stays glider-relative if defeated mid-flight).</description></item>
    /// </list>
    /// </summary>
    public enum GoblinState { on_glider, engaged, attack, getting_hit, death, jump_to_platform, blocking, evade, jump_to_glider }
    public GoblinState gState;




    // Narrows Animator access down to a small, documented API. Built in Start() around the existing 'anim' reference. See AnimationDriver.
    private AnimationDriver animDriver;


    // Plays one-shot sound effects without re-declaring clip arrays and Random.Range logic at every call site. Built in Start(). See AudioController.
    private AudioController audioController;




    /// <summary>Lets the boss's projectiles play sounds through this boss's own AudioSource.</summary>
    public AudioController AudioController => audioController;




    // Sound Files
    [SerializeField] public AudioSource audioSrc;
    [SerializeField] private AudioClip sndHit;
    [SerializeField] private AudioClip sndHit2;
    [SerializeField] private AudioClip sndHit3;
    [SerializeField] private AudioClip sndLand;
    [SerializeField] private AudioClip sndStep;
    [SerializeField] private AudioClip sndStep2;
    [SerializeField] private AudioClip sndGSpinner1;
    [SerializeField] private AudioClip sndGSpinner2;
    [SerializeField] public AudioClip sndGLaugh1;
    [SerializeField] public AudioClip sndGLaugh2;
    [SerializeField] public AudioClip sndGLaugh3;
    [SerializeField] private AudioClip sndGAction1;
    [SerializeField] private AudioClip sndGAction2;
    [SerializeField] private AudioClip sndGWin1;
    [SerializeField] private AudioClip sndGWin2;
    [SerializeField] private AudioClip sndGWin3;

    [SerializeField] private AudioClip sndGCounter;
    private float counterSoundTimer = 0f;

    [SerializeField] private AudioClip sndBlock;
    [SerializeField] private AudioClip sndIntro;
    [SerializeField] private AudioClip sndIntro2;

    private AudioClip sndQuickHit;
    private AudioClip sndQuickHit2;
    private AudioClip sndStrongHit;
    private AudioClip sndStrongHit2;
    private bool wasGrounded = false;
    private bool hasPlayedStep1;
    private bool hasPlayedStep2;
    private bool win = false;




    // Alarms
    [SerializeField] public int alarm4 = 0;
    [SerializeField] private float distanceFromPlayer = 0f;
    public int alarm7 = 0;
    [SerializeField] private int alarm11 = 0;
    [SerializeField] private bool startAlarm11 = false;
    private int alarm12 = 0;
    [SerializeField] private int alarm6 = 300;
    private bool startAlarm2 = false;
    private int alarm2 = 0;




    // Combat
    private Material outline;
    [SerializeField] private PlayerStep player;
    public UnityEvent<PlayerStep> OnAttack;
    public bool attacking = false;
    public bool collidedWithPlayer = false;
    [SerializeField] private GameObject hitParticlePrefab;
    private bool hurtOnGlider = false;
    private bool blockOnGlider = false;
    public float swingKickHitCooldown = 0f;
    private bool counterTriggered = false;
    [SerializeField] private float evadeJumpDuration = 0.5f;
    [SerializeField] private float evadeJumpHeight = 1.5f;
    [SerializeField] private float evadeJumpMinDist = 2f;
    [SerializeField] private float evadeJumpMaxDist = 3.5f;
    private Vector2 evadeJumpTarget;
    private float evadeDir = 1f;
    private bool evadeWillRush = false;
    private float evadeRushDelay = 0f;
    private int hitStreak = 0;
    private float arenaLeftBound = -10.27f;
    private float arenaRightBound = -1.27f;
    [SerializeField] public bool blocking = false;
    private bool canAttack = true;




    // health bar
    [SerializeField] public int health = 300;
    [SerializeField] private int maxHealth = 300;
    [Tooltip("The boss's own health bar UI. Assign the boss health bar in the scene directly, now that HealthBar is a shared, general-purpose widget, an automatic scene search could just as easily find the player's health bar instead.")]
    [SerializeField] private HealthBar healthbar;




    [SerializeField] public GliderScript glider;
    private bool gliderActive = false;
    private int platDir = 0;




    // Projectile stuff
    [SerializeField] private bool throwing = false;
    [SerializeField] private bool threw = false;
    [SerializeField] private GameObject goblinBombPrefab;
    private bool canThrow = true;
    [SerializeField] private bool spinners = false;
    [SerializeField] private GameObject goblinSpinnerPrefab;
    



    private Vector2 jumpStartPos;
    private float jumpT = 0f;
    [SerializeField] private float jumpDuration = 0.8f; // seconds to complete arc
    [SerializeField] private float jumpArcHeight = 2.5f; // peak height above start/end
    private bool jumpInitialized = false;




    public bool IsSolidToPlayer => gState == GoblinState.engaged;




    public Collider2D BarrierCollider => coll;




    /// <summary>
    /// Nudges this enemy a small step sideways, unless something solid is immediately in the way.
    /// </summary>
    /// <param name="dir">Which way to push: negative nudges left, positive nudges right.</param>
    public void NudgeAway(float dir)
    {
        CharacterPhysics2D.NudgeAwayFromOverlap(rb, dir, jumpableGround);
    }




    [SerializeField] private string titleSceneName = "Title Screen";




    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<BoxCollider2D>();
        animDriver = new AnimationDriver(anim);
        audioController = new AudioController(audioSrc);
        gState = GoblinState.on_glider;
        if (!setCustomStartingDir) { dirX = UnityEngine.Random.Range(0, 2) == 0 ? 1 : -1; }
        outline = sprite.material;
        player.OnHitG.AddListener((x) => OnPlayerHit(x));
        sndQuickHit = player.sndQuickHit;
        sndQuickHit2 = player.sndQuickHit2;
        sndStrongHit = player.sndStrongHit;
        sndStrongHit2 = player.sndStrongHit2;
        healthbar.UpdateHealthBar(health, maxHealth);

        audioController.PlayRandom(sndIntro, sndIntro2);
    }




    void Update()
    {
        if (glider.state != GState.GroundFight && glider.state != GState.AirFight && glider.state != GState.Zooming) { blocking = false; }
        if ((glider.state == GState.GroundFight || glider.state == GState.Zooming) && player.uppercut && Vector3.Distance(player.transform.position, transform.position) <= 1f) { blocking = false; }

        if (swingKickHitCooldown > 0f)
            swingKickHitCooldown -= Time.deltaTime;

        distanceFromPlayer = Vector3.Distance(player.transform.position, transform.position);
        bool noHitWall = !Physics2D.Raycast(transform.position, (player.transform.position - transform.position).normalized, distanceFromPlayer, jumpableGround);

        // Outline Shader Color Control
        if (gState == GoblinState.attack) { outline.color = Color.red; }
        else if (Math.Abs(transform.position.x - player.transform.position.x) <= 5f && noHitWall && (gState == GoblinState.engaged || gState == GoblinState.blocking || gState == GoblinState.getting_hit || glider.state == GState.AirFight)) { outline.color = Color.white; }
        else { outline.color = Color.black; }

        collidedWithPlayer = Physics2D.Raycast(transform.position, transform.right * -dirX, 0.65f, playerMask);

        AnimatorStateInfo stateInfo = animDriver.CurrentState;


        bool legitZeroAnimSpeed = (gState == GoblinState.death && stateInfo.normalizedTime >= 0.99f) || (gState == GoblinState.jump_to_platform && jumpT >= 0.45f && jumpT < 0.85f && stateInfo.IsName("Goblin_Jump")) || (gState == GoblinState.evade && jumpT < 1f && jumpT >= 0.45f && jumpT < 0.85f && stateInfo.IsName("Goblin_Jump")) || (gState == GoblinState.jump_to_glider && jumpT >= 0.45f && jumpT < 0.85f && stateInfo.IsName("Goblin_Jump"));

        if (animDriver.Speed == 0f && !legitZeroAnimSpeed) { animDriver.Speed = 1f; }


        if (glider.state != GState.Throwing && glider.state != GState.AirFight && gState != GoblinState.engaged)
        {
            if (throwing && !threw)
            {
                canThrow = true;
                startAlarm11 = false;
                spinners = false;
            }

            throwing = false;
        }

        if (glider.state == GState.AirFight) { gliderActive = true; } else { gliderActive = false; }
        if (gliderActive && health == 0) { transform.position = new Vector2(glider.transform.position.x, glider.transform.position.y + 1.86f); rb.velocity = new Vector2(0f, 0f); }

        Vector2 start = transform.position;
        Vector2 end = player.transform.position;
        RaycastHit2D[] hits = Physics2D.LinecastAll(start, end);

        if (counterSoundTimer > 0f) { counterSoundTimer -= Time.deltaTime; }

        if (alarm4 > 0)
        {
            alarm4 -= 4;
        }
        else
        {
            canAttack = true;
        }

        if (alarm6 > 0)
        {
            alarm6 -= 1;
        }
        else
        {
            if (!blocking)
            {
                blocking = true;
                alarm7 = 200;
            }

            alarm6 = 400;
        }

        if (startAlarm2)
        {
            if (alarm2 > 0)
            {
                alarm2 -= 1;
            }
            else
            {
                if (gState == GoblinState.death)
                {
                    SceneManager.LoadScene(titleSceneName);
                }
            }
        }

        if (alarm7 > 0)
        {
            alarm7 -= 1;
        }
        else
        {
            if ((glider.state == GState.GroundFight || glider.state == GState.Zooming) && gState != GoblinState.on_glider && gState != GoblinState.death)
            {
                if (player.pState == PlayerStep.PlayerState.dashenemy && !counterTriggered && Vector3.Distance(player.transform.position, transform.position) <= 0.4f)
                {
                    int hitIndex = UnityEngine.Random.Range(0, 2);
                    MovementState mstate = MovementState.idle;

                    switch (hitIndex)
                    {
                        case 0: { mstate = MovementState.punch1; animDriver.Speed = 1f; } break;
                        case 1: { mstate = MovementState.punch2; animDriver.Speed = 1f; } break;
                    }

                    animDriver.SetMovementState((int)mstate);

                    dirX = 0;
                    gState = GoblinState.attack;

                    if (!win && counterSoundTimer <= 0f)
                    {
                        audioController.Play(sndGCounter);
                        counterSoundTimer = sndGCounter.length;
                    }

                    player.trigger = true;
                    player.alarm4 = 60;
                    counterTriggered = true;
                    canAttack = false;
                    rb.gravityScale = 0;
                    player.isEnemyAttacking = true;
                }

                if (player.pState != PlayerStep.PlayerState.dashenemy) { counterTriggered = false; }

                blocking = false;
            }
            else
            {
                if (player.pState != PlayerStep.PlayerState.dashenemy) { blocking = false; }
            }
        }

        if (startAlarm11)
        {
            if (alarm11 > 0)
            {
                alarm11 -= 1;
            }
            else
            {
                if (glider.state == GState.Throwing || gState == GoblinState.engaged || glider.state == GState.AirFight) { if (throwing) { threw = true; } }
                if (player.isEnemyAttacking) { player.isEnemyAttacking = false; }
                startAlarm11 = false;
            }
        }

        if (alarm12 != -1)
        {
            if (alarm12 > 0)
            {
                alarm12 -= 1;
            }
            else
            {
                canThrow = true;
                alarm12 = -1;
            }
        }

        if (health <= 0)
        {
            gState = GoblinState.death;
        }

        if (gState != GoblinState.death)
        {
            if (player.pState == PlayerStep.PlayerState.death && !win)
            {
                audioController.PlayRandom(sndGWin1, sndGWin2, sndGWin3);
                win = true;
            }
        }


        switch (gState)
        {
            // Aerial phase: riding the glider, throwing bombs (ground-facing) or spinners (in an air fight) at the player. Transitions to jump_to_platform once the glider settles into a ground fight.
            case GoblinState.on_glider:
                {
                    if (glider.state != GState.AirFight)
                    {
                        Vector2 pos = glider.transform.position;
                        transform.position = new Vector2(pos.x, pos.y + 0.54f);
                    }
                    else if (!player.attacking)
                    {
                        Vector2 pos = glider.transform.position;
                        transform.position = new Vector2(pos.x, pos.y + 0.54f);
                    }
                    else
                    {
                        rb.gravityScale = 0;
                        rb.velocity = Vector2.zero;
                    }

                    float normalizedTime = stateInfo.normalizedTime % 1f;

                    if (glider.state == GState.GroundFight && gState != GoblinState.jump_to_platform)
                    {
                        float distLeft = Vector2.Distance(transform.position, new Vector2(-10.27f, 4.24f));
                        float distRight = Vector2.Distance(transform.position, new Vector2(-1.27f, 4.24f));

                        platDir = distLeft < distRight ? -1 : 1;
                        gState = GoblinState.jump_to_platform;
                    }

                    if (glider.state == GState.Throwing)
                    {
                        if (!throwing)
                        {
                            if (!startAlarm11)
                            {
                                alarm11 = 60;
                                startAlarm11 = true;
                            }

                            throwing = true;
                        }

                        if (threw)
                        {
                            if (stateInfo.IsName("Goblin_Throw") && normalizedTime >= 0.5f && normalizedTime <= 0.53f)
                            {
                                if (!noHitWall && FindObjectsOfType<PumpkinSpinner>().Length < 3)
                                {
                                    int dir = sprite.flipX ? -1 : 1;
                                    SpawnSpinner(dir, RandomChoice(2, 4, 6, 8));
                                    SpawnSpinner(dir, RandomChoice(4, 8, 12, 16));
                                    SpawnSpinner(dir, RandomChoice(3, 6, 9, 12));
                                    audioController.PlayRandom(sndGAction1, sndGAction2);
                                }
                                else if (noHitWall && FindObjectsOfType<PumpkinProjectile>().Length == 0)
                                {
                                    GameObject bomb = Instantiate(goblinBombPrefab, transform.position + Vector3.up * 0.48f, Quaternion.identity);
                                    int dir = sprite.flipX ? -1 : 1;
                                    bomb.GetComponent<PumpkinProjectile>().dir = dir;
                                    audioController.PlayRandom(sndGAction1, sndGAction2);
                                }
                            }
                        }
                    }

                    if (glider.state == GState.AirFight)
                    {
                        if (!throwing)
                        {
                            StartCoroutine(ThrowCooldown());
                            throwing = true;
                        }

                        if (threw)
                        {
                            if (stateInfo.IsName("Goblin_Throw") && normalizedTime >= 0.5f && normalizedTime <= 0.53f && FindObjectsOfType<PumpkinSpinner>().Length < 3)
                            {
                                int dir = sprite.flipX ? -1 : 1;

                                SpawnSpinner(dir, RandomChoice(2, 4, 6, 8));
                                SpawnSpinner(dir, RandomChoice(4, 8, 12, 16));
                                SpawnSpinner(dir, RandomChoice(3, 6, 9, 12));

                                audioController.PlayRandom(sndGAction1, sndGAction2);
                            }
                        }
                    }

                    if (threw)
                    {
                        if (stateInfo.IsName("Goblin_Throw") && stateInfo.normalizedTime >= 1f)
                        {
                            if (!startAlarm11)
                            {
                                int alarmIndex = UnityEngine.Random.Range(0, 3);

                                if (glider.state != GState.AirFight)
                                {
                                    switch (alarmIndex)
                                    {
                                        case 0: { alarm11 = 60; startAlarm11 = true; } break;
                                        case 1: { alarm11 = 120; startAlarm11 = true; } break;
                                        case 2: { alarm11 = 180; startAlarm11 = true; } break;
                                    }
                                }
                                else
                                {
                                    switch (alarmIndex)
                                    {
                                        case 0: { alarm11 = 240; startAlarm11 = true; } break;
                                        case 1: { alarm11 = 300; startAlarm11 = true; } break;
                                        case 2: { alarm11 = 360; startAlarm11 = true; } break;
                                    }
                                }
                            }

                            threw = false;
                        }
                    }

                    if ((stateInfo.IsName("Goblin_Hit1") && stateInfo.normalizedTime >= 1f) || (stateInfo.IsName("Goblin_Hit2") && stateInfo.normalizedTime >= 1f))
                    {
                        blockOnGlider = false;
                        hurtOnGlider = false;
                    }

                    if (stateInfo.IsName("Goblin_Block") && stateInfo.normalizedTime >= 1f)
                    {
                        canAttack = true; dirX = 0;
                        blockOnGlider = false;
                        hurtOnGlider = false;
                    }
                }
                break;




            // A scripted arc jump (see DriveJumpAnim) from the glider down onto a fixed platform, chosen by whichever platform is closer. Becomes engaged on landing.
            case GoblinState.jump_to_platform:
                {
                    Vector2 targetPos = platDir == -1 ? new Vector2(-9.79f, 4.44f) : new Vector2(-1.73f, 4.44f);

                    sprite.flipX = platDir != -1;
                    rb.gravityScale = 0;
                    rb.velocity = Vector2.zero;

                    // Initialize jump on first frame of this state
                    if (!jumpInitialized)
                    {
                        jumpStartPos = transform.position;
                        jumpT = 0f;
                        jumpInitialized = true;
                        animDriver.Speed = 1f;
                    }

                    jumpT += Time.deltaTime / jumpDuration;
                    jumpT = Mathf.Clamp01(jumpT);

                    // Linear X/Y interpolation between start and target
                    Vector2 linearPos = Vector2.Lerp(jumpStartPos, targetPos, jumpT);

                    // Parabolic arc
                    float arcOffset = Mathf.Sin(jumpT * Mathf.PI) * jumpArcHeight;
                    transform.position = new Vector2(linearPos.x, linearPos.y + arcOffset);

                    DriveJumpAnim(jumpT);

                    if (jumpT >= 1f)
                    {
                        transform.position = targetPos; // snap to exact landing point
                        rb.gravityScale = 1;
                        jumpInitialized = false;       // reset for next use
                        gState = GoblinState.engaged;
                    }
                }
                break;




            // Ground combat: paces within the arena bounds, throws bombs/spinners on a cooldown, and punches once close enough and canAttack allows it. The bulk of this boss's ground behavior lives in this one case.
            case GoblinState.engaged:
                {
                    float engagedVelX = dirX * 2.5f;

                    bool movingTowardPlayer = Mathf.Sign(dirX) == Mathf.Sign(player.transform.position.x - transform.position.x);

                    if (collidedWithPlayer && movingTowardPlayer && !player.IsPhysicallyPassable()) { engagedVelX = 0f; }

                    rb.velocity = new Vector2(engagedVelX, rb.velocity.y);

                    if ((Vector3.Distance(player.transform.position, transform.position) <= 0.8f) && !player.isEnemyAttacking && Grounded() && ((!sprite.flipX && transform.position.x < player.transform.position.x) || (sprite.flipX && transform.position.x > player.transform.position.x)) && canAttack)
                    {
                        gState = GoblinState.attack;
                        audioController.PlayRandom(sndGAction1, sndGAction2);
                        rb.gravityScale = 0;
                        int hitIndex = UnityEngine.Random.Range(0, 2);
                        MovementState mstate = MovementState.idle;

                        switch (hitIndex)
                        {
                            case 0: { mstate = MovementState.punch1; animDriver.Speed = 1f; } break;
                            case 1: { mstate = MovementState.punch2; animDriver.Speed = 1f; } break;
                        }

                        animDriver.SetMovementState((int)mstate);
                        canAttack = false;
                        player.isEnemyAttacking = true;
                        player.trigger = true;
                        player.alarm4 = 60;
                    }

                    if ((Math.Abs(transform.position.x - player.transform.position.x) > 0.572f) && transform.position.x > -10.27f && transform.position.x < -1.27f)
                    {
                        if (transform.position.x > player.transform.position.x) { dirX = -1; } else { dirX = 1; }
                    }

                    if (Math.Abs(transform.position.x - player.transform.position.x) < 0.572f) { dirX = 0; }
                    if ((Math.Abs(transform.position.x - player.transform.position.x) > 0.572f) && transform.position.x < -10.27f) { dirX = 1; }
                    if ((Math.Abs(transform.position.x - player.transform.position.x) > 0.572f) && transform.position.x > -1.27f) { dirX = -1; }
                    if (transform.position.x < -10.27f && transform.position.y < 3.6f) { transform.position = new Vector2(-9.79f, 4.44f); }
                    if (transform.position.x > -1.27f && transform.position.y < 3.6f) { transform.position = new Vector2(-1.73f, 4.44f); }

                    if (Vector3.Distance(player.transform.position, transform.position) >= 2f && canThrow)
                    {
                        throwing = true;
                        canThrow = false;

                        if (!startAlarm11)
                        {
                            alarm11 = 5;
                            startAlarm11 = true;
                        }
                    }

                    if (throwing)
                    {
                        dirX = 0;

                        if (player.transform.position.x > transform.position.x) { sprite.flipX = false; } else { sprite.flipX = true; }
                    }

                    if (!throwing && threw)
                    {
                        alarm12 = 90;
                        threw = false;
                    }

                    if (!throwing) { spinners = false; }
                    if (threw) { spinners = false; }

                    if (player.transform.position.y + 1f < transform.position.y)
                    {
                        throwing = true;
                        canThrow = false;
                        spinners = true;
                    }

                    if (spinners)
                    {
                        dirX = 0;

                        if (stateInfo.IsName("Goblin_Throw") && (stateInfo.normalizedTime >= 0.95f))
                        {
                            alarm12 = 90;
                            spinners = false;
                            throwing = false;
                        }

                        if ((stateInfo.normalizedTime >= 0.5f) && (stateInfo.normalizedTime <= 0.53f) && FindObjectsOfType<PumpkinSpinner>().Length < 2)
                        {
                            int dirS = sprite.flipX ? -1 : 1;

                            if (player.transform.position.y < transform.position.y)
                            {
                                SpawnSpinner(dirS, RandomChoice(16, 18, 20));
                                SpawnSpinner(dirS, RandomChoice(18, 18, 19));
                                SpawnSpinner(dirS, RandomChoice(17, 19));
                            }
                            else
                            {
                                SpawnSpinner(dirS, RandomChoice(2, 4, 6, 8));
                                SpawnSpinner(dirS, RandomChoice(4, 8, 12, 16));
                                SpawnSpinner(dirS, RandomChoice(3, 6, 9, 12));
                            }

                            audioController.PlayRandom(sndGSpinner1, sndGSpinner2);
                        }
                    }

                    if (threw)
                    {
                        dirX = 0;

                        if (stateInfo.IsName("Goblin_Throw") && (stateInfo.normalizedTime >= 0.95f))
                        {
                            int alarmIndex = UnityEngine.Random.Range(0, 3);

                            switch (alarmIndex)
                            {
                                case 0: { alarm11 = 60; } break;
                                case 1: { alarm11 = 120; } break;
                                case 2: { alarm11 = 180; } break;
                            }

                            alarm12 = 90;
                            threw = false;
                            throwing = false;
                        }

                        if ((stateInfo.normalizedTime >= 0.5f) && (stateInfo.normalizedTime <= 0.53f) && FindObjectsOfType<PumpkinProjectile>().Length == 0)
                        {
                            GameObject bomb = Instantiate(goblinBombPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                            int dirP = sprite.flipX ? -1 : 1;
                            bomb.GetComponent<PumpkinProjectile>().dir = dirP;
                            audioController.PlayRandom(sndGLaugh1, sndGLaugh2, sndGLaugh3);
                        }
                    }

                    if (!wasGrounded && Grounded() && gState == GoblinState.engaged)
                        audioController.Play(sndLand);

                    wasGrounded = Grounded();
                }
                break;




            // Mid-punch: closes the last bit of distance during the animation's early frames, then returns to engaged once it finishes. Actual damage is dealt separately, via OnPlayerHit.
            case GoblinState.attack:
                {
                    rb.velocity = new Vector2(0f, 0f);
                    canAttack = false;

                    if (Math.Abs(player.transform.position.x - transform.position.x) >= 0.45f && ((stateInfo.IsName("Goblin_Punch1") && stateInfo.normalizedTime <= 0.24f) || (stateInfo.IsName("Goblin_Punch2") && stateInfo.normalizedTime <= 0.38f)))
                    {
                        float step = 4f * Time.deltaTime;
                        Vector2 targetPosition = new Vector2(player.transform.position.x, transform.position.y);
                        transform.position = Vector2.MoveTowards(transform.position, targetPosition, step);
                        if (targetPosition.x < transform.position.x) { sprite.flipX = true; } else { sprite.flipX = false; }
                    }

                    if ((stateInfo.IsName("Goblin_Punch1") && stateInfo.normalizedTime >= 1f) || (stateInfo.IsName("Goblin_Punch2") && stateInfo.normalizedTime >= 1f))
                    {
                        int hitIndex = UnityEngine.Random.Range(0, 3);

                        switch (hitIndex)
                        {
                            case 0: { alarm4 = 300; } break;
                            case 1: { alarm4 = 400; } break;
                            case 2: { alarm4 = 500; } break;
                        }

                        gState = GoblinState.engaged;
                        player.isEnemyAttacking = false;
                        animDriver.Speed = 1f;
                        rb.gravityScale = 1;
                    }
                }
                break;




            // Reacting to a hit or breaking free of a web, returns to engaged once the animation finishes, possibly triggering a forced evade (see TryForceEvadeAfterHit).
            case GoblinState.getting_hit:
                {
                    animDriver.Speed = 1f;

                    if ((stateInfo.IsName("Goblin_Hit1") && stateInfo.normalizedTime >= 1f) || (stateInfo.IsName("Goblin_Hit2") && stateInfo.normalizedTime >= 1f) || (stateInfo.IsName("Goblin_BreakWeb1") && stateInfo.normalizedTime >= 1f) || (stateInfo.IsName("Goblin_BreakWeb2") && stateInfo.normalizedTime >= 1f))
                    {
                        gState = GoblinState.engaged;
                        TryForceEvadeAfterHit();
                    }
                }
                break;




            // Guarding, see OnPlayerHit for how a blocked hit differs from a normal one. Returns to engaged once the block animation finishes.
            case GoblinState.blocking:
                {
                    animDriver.Speed = 1f;
                    dirX = 0;
                    canAttack = true;
                    if (stateInfo.IsName("Goblin_Block") && stateInfo.normalizedTime >= 1f) { gState = GoblinState.engaged; }
                }
                break;




            // Defeated: stops colliding with the player. Stays glider-relative if defeated mid-flight, otherwise settles to the ground under normal gravity.
            case GoblinState.death:
                {
                    Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Enemy"), LayerMask.NameToLayer("Player"), true);

                    if (glider.state == GState.AirFight)
                    {
                        Vector2 pos = glider.transform.position;
                        transform.position = new Vector2(pos.x, pos.y + 0.54f);
                    }
                    else
                    {
                        rb.gravityScale = 1;
                        rb.velocity = Vector2.zero;
                    }
                }
                break;




            // A scripted arc jump away from the player (see DriveJumpAnim and StartEvasion's target-picking). Once landed, either rushes straight into an attack (if evadeWillRush rolled true and the player is close enough) or settles back into engaged.
            case GoblinState.evade:
                {
                    sprite.flipX = evadeDir < 0f;

                    if (jumpT < 1f)
                    {
                        rb.gravityScale = 0;
                        rb.velocity = Vector2.zero;

                        if (!jumpInitialized)
                        {
                            jumpStartPos = transform.position;
                            jumpInitialized = true;
                            animDriver.Speed = 1f;
                        }

                        jumpT += Time.deltaTime / evadeJumpDuration;
                        jumpT = Mathf.Clamp01(jumpT);

                        Vector2 linearPos = Vector2.Lerp(jumpStartPos, evadeJumpTarget, jumpT);
                        float arcOffset = Mathf.Sin(jumpT * Mathf.PI) * evadeJumpHeight;
                        transform.position = new Vector2(linearPos.x, linearPos.y + arcOffset);

                        DriveJumpAnim(jumpT);

                        if (jumpT >= 1f)
                        {
                            transform.position = evadeJumpTarget;
                            rb.gravityScale = 1;
                            jumpInitialized = false;

                            if (Grounded()) audioController.Play(sndLand);
                        }
                    }
                    else
                    {
                        if (evadeRushDelay > 0f)
                        {
                            rb.velocity = new Vector2(0f, rb.velocity.y);
                            evadeRushDelay -= Time.deltaTime;
                        }
                        else
                        {
                            if (evadeWillRush && Vector3.Distance(player.transform.position, transform.position) <= 5f)
                            {
                                gState = GoblinState.attack;
                                audioController.PlayRandom(sndGAction1, sndGAction2);
                                rb.gravityScale = 0;

                                int hitIndex = UnityEngine.Random.Range(0, 2);
                                MovementState mstate = hitIndex == 0 ? MovementState.punch1 : MovementState.punch2;
                                animDriver.Speed = 1f;
                                animDriver.SetMovementState((int)mstate);

                                canAttack = false;
                                player.isEnemyAttacking = true;
                                player.trigger = true;
                                player.alarm4 = 60;
                            }
                            else
                            {
                                gState = GoblinState.engaged;
                            }
                        }
                    }
                }
                break;




            // A scripted arc jump back up onto the glider, mirroring jump_to_platform. Returns to on_glider on landing.
            case GoblinState.jump_to_glider:
                {
                    Vector2 targetPos = new Vector2(glider.transform.position.x, glider.transform.position.y + 0.54f);

                    sprite.flipX = glider.sr.flipX;
                    rb.gravityScale = 0;
                    rb.velocity = Vector2.zero;

                    if (!jumpInitialized)
                    {
                        jumpStartPos = transform.position;
                        jumpT = 0f;
                        jumpInitialized = true;
                        animDriver.Speed = 1f;
                    }

                    jumpT += Time.deltaTime / jumpDuration;
                    jumpT = Mathf.Clamp01(jumpT);

                    Vector2 linearPos = Vector2.Lerp(jumpStartPos, targetPos, jumpT);
                    float arcOffset = Mathf.Sin(jumpT * Mathf.PI) * jumpArcHeight;
                    transform.position = new Vector2(linearPos.x, linearPos.y + arcOffset);

                    DriveJumpAnim(jumpT);

                    if (jumpT >= 1f)
                    {
                        transform.position = targetPos;
                        jumpInitialized = false;
                        canThrow = true;
                        gState = GoblinState.on_glider;
                    }
                }
                break;
        }


        UpdateAnimationState();
    }
    



    /// <summary>
    /// Updates the goblin character's animation state based on its current logical state and movement conditions.
    /// </summary>
    private void UpdateAnimationState()
    {
        if (gState != GoblinState.attack && gState != GoblinState.evade && gState != GoblinState.jump_to_glider)
        {
            if (dirX > 0f)
                sprite.flipX = false;
            else if (dirX < 0f)
                sprite.flipX = true;
        }

        if (gState == GoblinState.getting_hit) return;
        if (gState == GoblinState.blocking) return;
        if (gState == GoblinState.attack) return;

        if (gState == GoblinState.evade)
        {
            sprite.flipX = evadeDir < 0f;

            MovementState evadeMstate = jumpT < 1f ? MovementState.jump : MovementState.idle;
            animDriver.SetMovementState((int)evadeMstate);
            return;
        }

        if (gState == GoblinState.jump_to_glider)
        {
            sprite.flipX = glider.sr.flipX;
            animDriver.SetMovementState((int)MovementState.jump);
            return;
        }

        MovementState mstate = MovementState.idle;

        if (gState == GoblinState.on_glider)
        {
            sprite.flipX = glider.sr.flipX;

            if (blockOnGlider)
            {
                mstate = MovementState.blocking;
            }
            else if (hurtOnGlider)
            {
                AnimatorStateInfo stateInfo2 = animDriver.CurrentState;

                if (!stateInfo2.IsName("Goblin_Hit1") && !stateInfo2.IsName("Goblin_Hit2"))
                {
                    int hitIndex = UnityEngine.Random.Range(0, 2);

                    if (hitIndex == 0)
                        mstate = MovementState.hurt1;
                    else
                        mstate = MovementState.hurt2;
                }
                else
                {
                    if (stateInfo2.IsName("Goblin_Hit1"))
                        mstate = MovementState.hurt1;
                    else if (stateInfo2.IsName("Goblin_Hit2"))
                        mstate = MovementState.hurt2;
                }
            }
            else if (threw)
            {
                mstate = MovementState.throwing;
            }
            else
            {
                mstate = MovementState.crouching;
            }
        }

        if (gState == GoblinState.jump_to_platform)
        {
            mstate = MovementState.jump;
        }

        if (gState == GoblinState.engaged)
        {
            if (threw || spinners)
                mstate = MovementState.throwing;
            else if (dirX > 0f)
                mstate = MovementState.sprinting;
            else if (dirX < 0f)
                mstate = MovementState.sprinting;
            else
                mstate = MovementState.idle;

            if (rb.velocity.y < -0.1f) { mstate = MovementState.falling; }
        }

        AnimatorStateInfo stateInfo = animDriver.CurrentState;
        float normalizedTime = stateInfo.normalizedTime % 1f;

        if (gState == GoblinState.death)
        {
            animDriver.Speed = 1f;
            mstate = MovementState.death;

            if (normalizedTime >= 0.352f && normalizedTime <= 0.389f)
            {
                if (Grounded()) audioController.Play(sndLand);
            }

            if (!startAlarm2)
            {
                alarm2 = 360;
                startAlarm2 = true;
            }

            if (normalizedTime >= 0.99f)
            {
                animDriver.Speed = 0f;
            }
        }

        if (mstate == MovementState.sprinting)
        {
            if (normalizedTime >= 0.25f && normalizedTime <= 0.35f && !hasPlayedStep1)
            {
                audioController.Play(sndStep);
                hasPlayedStep1 = true;
            }
            else if (normalizedTime >= 0.70f && normalizedTime <= 0.80f && !hasPlayedStep2)
            {
                audioController.Play(sndStep2);
                hasPlayedStep2 = true;
            }

            if (normalizedTime < 0.05f)
            {
                hasPlayedStep1 = false;
                hasPlayedStep2 = false;
            }
        }
        else
        {
            hasPlayedStep1 = false;
            hasPlayedStep2 = false;
        }

        animDriver.SetMovementState((int)mstate);
    }




    /// <summary>
    /// True while this enemy is standing on the ground.
    /// </summary>
    public bool Grounded()
    {
        return CharacterPhysics2D.IsGrounded(coll, jumpableGround);
    }




    /// <summary>
    /// Called when the player's attack (or counter) connects with this boss, or attempts to, since
    /// this method also decides whether the hit is even valid: it's ignored outside engaged/getting_hit/
    /// air-fight/counter-during-attack. Applies damage, knockback, and a hurt/block reaction, plays the
    /// matching hit sound and VFX, and updates the health bar. A blocked hit does no knockback and
    /// plays a block animation instead of a hurt one.
    /// </summary>
    /// <param name="target">This boss itself, passed through from the player's hit-resolution event.</param>
    /// <param name="isCounterHit">True if this is a counter-attack rather than a regular hit, allows the hit to land even mid-attack.</param>
    /// <returns>True if the hit was valid and applied; false if it was ignored (e.g. the boss wasn't in a hittable state).</returns>
    public bool OnPlayerHit(GoblinStep target, bool isCounterHit = false)
    {
        player.isEnemyAttacking = false;

        if (gState == GoblinState.engaged || gState == GoblinState.getting_hit || (gState == GoblinState.on_glider && glider.state == GState.AirFight) || (gState == GoblinState.attack && isCounterHit))
        {
            float dir = 0;

            if (!player.sprite.flipX)
            {
                dir = 1f;
                dirX = -1f;
            }
            else
            {
                dir = -1f;
                dirX = 1f;
            }

            if (gState != GoblinState.on_glider)
            {
                if (blocking)
                    rb.velocity = new Vector2(0f, 0f);
                else if (player.uppercut)
                    rb.velocity = new Vector2(2.5f * dir, 0f);
                else if ((player.combo - 4) % 5 == 0)
                    rb.velocity = new Vector2(2.5f * dir, 0f);
                else
                    rb.velocity = new Vector2(dir, 0f);
            }

            MovementState mstate;
            animDriver.Speed = 1f;
            bool wasBlocked = false;

            if (gState != GoblinState.on_glider)
            {
                rb.gravityScale = 1;

                if (blocking && !isCounterHit)
                {
                    gState = GoblinState.blocking;
                    mstate = MovementState.blocking;
                    audioController.Play(sndBlock);
                    wasBlocked = true;
                }
                else
                {
                    gState = GoblinState.getting_hit;
                    hitStreak++;

                    int hitIndex = UnityEngine.Random.Range(0, 2);

                    if (hitIndex == 0)
                        mstate = MovementState.hurt1;
                    else
                        mstate = MovementState.hurt2;

                    if ((player.combo - 4) % 5 == 0)
                    {
                        audioController.PlayRandom(sndStrongHit, sndStrongHit2);
                    }
                    else
                    {
                        audioController.PlayRandom(sndQuickHit, sndQuickHit2);
                    }

                    Vector2 hitPoint = transform.position;
                    player.SpawnHitEffect(hitPoint);

                    if (health > 0)
                    {
                        if ((player.combo - 4) % 5 == 0)
                            health -= 11;
                        else if (player.countering)
                            health -= 5;
                        else if (player.uppercut)
                            health -= 8;
                        else
                            health -= 6;

                        healthbar.UpdateHealthBar(health, maxHealth);
                    }

                    audioController.PlayRandom(sndHit, sndHit2, sndHit3);
                }
            }
            else
            {
                int alarmIndex = UnityEngine.Random.Range(0, 3);

                switch (alarmIndex)
                {
                    case 0: { alarm11 = 60; startAlarm11 = true; } break;
                    case 1: { alarm11 = 120; startAlarm11 = true; } break;
                    case 2: { alarm11 = 180; startAlarm11 = true; } break;
                }

                if (blocking && !isCounterHit)
                {
                    mstate = MovementState.blocking;
                    audioController.Play(sndBlock);
                    blockOnGlider = true;
                    wasBlocked = true;
                }
                else
                {
                    int hitIndex = UnityEngine.Random.Range(0, 2);

                    if (hitIndex == 0)
                        mstate = MovementState.hurt1;
                    else
                        mstate = MovementState.hurt2;

                    if ((player.combo - 4) % 5 == 0)
                    {
                        audioController.PlayRandom(sndStrongHit, sndStrongHit2);
                    }
                    else
                    {
                        audioController.PlayRandom(sndQuickHit, sndQuickHit2);
                    }

                    Vector2 hitPoint = transform.position;
                    player.SpawnHitEffect(hitPoint);

                    if (health > 0)
                    {
                        if ((player.combo - 4) % 5 == 0)
                            health -= 11;
                        else if (player.countering)
                            health -= 5;
                        else if (player.uppercut)
                            health -= 8;
                        else
                            health -= 6;

                        healthbar.UpdateHealthBar(health, maxHealth);
                    }

                    hurtOnGlider = true;

                    audioController.PlayRandom(sndHit, sndHit2, sndHit3);
                }
            }

            int attackTime = UnityEngine.Random.Range(0, 3);

            switch (attackTime)
            {
                case 0: { alarm4 = 300; } break;
                case 1: { alarm4 = 400; } break;
                case 2: { alarm4 = 500; } break;
            }

            animDriver.SetMovementState((int)mstate);

            return !wasBlocked;
        }
        else if (gState == GoblinState.on_glider && glider.state != GState.AirFight)
        {
            MovementState mstate;
            animDriver.Speed = 1f;
            int alarmIndex = UnityEngine.Random.Range(0, 3);

            switch (alarmIndex)
            {
                case 0: { alarm11 = 60; startAlarm11 = true; } break;
                case 1: { alarm11 = 120; startAlarm11 = true; } break;
                case 2: { alarm11 = 180; startAlarm11 = true; } break;
            }

            mstate = MovementState.blocking;
            audioController.Play(sndBlock);
            blockOnGlider = true;
            animDriver.SetMovementState((int)mstate);

            return false;
        }

        return false;
    }




    /// <summary>
    /// Called from an Animation Event on this boss's attack impact frame. Deals damage to the player if they're still within range by that point in the animation.
    /// </summary>
    public void AttackEvent()
    {
        if (Vector3.Distance(player.transform.position, transform.position) <= 0.55f) { player.DamageGoblin(this); }
    }




    /// <summary>
    /// Spawns a one-off hit-impact visual effect at the given point.
    /// </summary>
    /// <param name="impactPoint">Where to spawn the effect.</param>
    /// <param name="other">The object that was hit, currently unused, but kept for a consistent signature with the other enemy types' equivalent method.</param>
    public void SpawnObjectHitEffect(Vector2 impactPoint, GameObject other)
    {
        Vector3 hitPosition = (transform.position + other.transform.position) / 2f;
        GameObject hitFX = Instantiate(hitParticlePrefab, impactPoint, Quaternion.identity);
    }




    /// <summary>
    /// Spawns a single pumpkin spinner projectile, thrown from this boss's current position.
    /// </summary>
    /// <param name="dir">Direction the spinner travels: -1 for left, 1 for right.</param>
    /// <param name="speed">The spinner's horizontal speed (combined with <paramref name="dir"/> to get its actual velocity).</param>
    void SpawnSpinner(int dir, int speed)
    {
        GameObject spinner = Instantiate(goblinSpinnerPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        var s = spinner.GetComponent<PumpkinSpinner>();
        s.dir = dir;
        s.hspeed = speed * dir;
    }




    /// <summary>
    /// Implements a cooldown period after a throw action, delaying further throws based on the current glider state.
    /// </summary>
    /// <returns>An enumerator that yields for the duration of the cooldown period. The wait time is 6 seconds if the glider is
    /// in air fight state; otherwise, 1 second.</returns>
    IEnumerator ThrowCooldown()
    {
        if (glider.state == GState.AirFight)
            yield return new WaitForSeconds(6f);
        else
            yield return new WaitForSeconds(1f); // 1f = 60 frames

        threw = true;
    }




    /// <summary>
    /// Picks one value at random from the given set, with equal probability. A small helper for the varied throw speeds/distances used when spawning spinners.
    /// </summary>
    /// <param name="values">The set of values to choose from.</param>
    int RandomChoice(params int[] values)
    {
        return values[UnityEngine.Random.Range(0, values.Length)];
    }




    /// <summary>
    /// Begins the evade state: picks a jump target a random distance away from the player (clamped
    /// to stay within the arena bounds), and rolls whether this evade will end in a rush-attack.
    /// Unlike RobotStep's evasion, this doesn't check for hazards or ledges first. The boss simply
    /// jumps, so there's no ground-walking risk to avoid.
    /// </summary>
    private void StartEvasion()
    {
        gState = GoblinState.evade;
        hitStreak = 0;

        evadeDir = (transform.position.x < player.transform.position.x) ? -1f : 1f;

        float jumpDist = UnityEngine.Random.Range(evadeJumpMinDist, evadeJumpMaxDist);
        float targetX = Mathf.Clamp(transform.position.x + evadeDir * jumpDist, arenaLeftBound + 0.5f, arenaRightBound - 0.5f);

        evadeJumpTarget = new Vector2(targetX, transform.position.y);
        jumpT = 0f;
        jumpInitialized = false;

        evadeWillRush = UnityEngine.Random.Range(0, 2) == 0;
        evadeRushDelay = evadeWillRush ? UnityEngine.Random.Range(0.2f, 0.5f) : 0f;
    }




    /// <summary>
    /// Rolls a chance to force this boss into evasion right after taking a hit, guaranteed after 2 or more hits in a row (hitStreak), a coin flip otherwise.
    /// </summary>
    private void TryForceEvadeAfterHit()
    {
        float evadeChance = hitStreak >= 2 ? 1f : 0.5f;

        if (UnityEngine.Random.value < evadeChance)
            StartEvasion();
    }




    /// <summary>Starts the scripted jump back up onto the glider. Does nothing if this boss is already dead.</summary>
    public void BeginJumpToGlider()
    {
        if (gState == GoblinState.death) return;

        gState = GoblinState.jump_to_glider;
        jumpInitialized = false;
        jumpT = 0f;
    }




    /// <summary>
    /// Drives the mid-air animation for a scripted jump (used by jump_to_platform, jump_to_glider,
    /// and evade), based on progress through the jump from 0 (start) to 1 (landed): crouch to wind up,
    /// an airborne pose for the bulk of the arc, then a landing pose right at the end.
    /// </summary>
    /// <param name="jumpT">Progress through the current jump, from 0 to 1.</param>
    void DriveJumpAnim(float jumpT)
    {
        float animT;

        if (jumpT < 0.45f)
            animT = Mathf.Lerp(0f, 0.5f, jumpT / 0.45f);
        else if (jumpT < 0.85f)
            animT = 0.5f;
        else
            animT = Mathf.Lerp(0.5f, 1f, (jumpT - 0.85f) / 0.15f);

        animDriver.Speed = 0f;
        animDriver.Play("Goblin_Jump", animT);
    }
}