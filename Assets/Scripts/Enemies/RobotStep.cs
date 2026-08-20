using System;
using System.Collections.Generic;
using Framework.Core;
using UnityEngine;
using UnityEngine.Events;


/// <summary>
/// Represents a patrolling enemy robot with state-driven AI, combat interactions, and environmental awareness.
/// Implements barrier and targeting logic for player interactions as part of the IEnemyBarrier interface.
/// </summary>


public class RobotStep : MonoBehaviour, IEnemyBarrier
{
    public Rigidbody2D rb;
    [SerializeField] public Animator anim;
    [SerializeField] public SpriteRenderer sprite;
    private BoxCollider2D coll;
    private float lastspd = 0f;
    [SerializeField] private float dirX = 0f;
    [SerializeField] private bool setCustomStartingDir = false;
    [SerializeField] private LayerMask jumpableGround;
    [SerializeField] private LayerMask playerMask;
    [SerializeField] public float hsp = 1f; // Horizontal speed
    [SerializeField] private int waitTime = 120;
    private Camera cam;




    /// <summary>Drives which Animator clip is playing, via the shared "mstate" integer parameter. Set through <see cref="AnimationDriver.SetMovementState"/>, not directly.</summary>
    public enum MovementState { idle, running, falling, hurt1, hurt2, launched, shocked, sprinting, alertidle, punch1, punch2, kick, backstep, webbed, death, breakfree }




    /// <summary>
    /// This robot's high-level AI state, driving the switch statement in <see cref="Update"/>.
    /// <list type="bullet">
    /// <item><description><b>normal</b>: patrolling back and forth, unaware of the player.</description></item>
    /// <item><description><b>shocked</b>: just noticed the player is nearby. A brief alert reaction before becoming fully alert.</description></item>
    /// <item><description><b>alert</b>: aware of the player, closing distance or backing off to a preferred range, watching for an attack opening.</description></item>
    /// <item><description><b>attack</b>: mid-punch/kick, closing the last bit of distance and dealing damage on the animation's impact frame.</description></item>
    /// <item><description><b>hurt</b>: reacting to being hit. Either a quick stagger or, if launched, airborne until landing.</description></item>
    /// <item><description><b>webbed</b>: immobilized, struggling to break free.</description></item>
    /// <item><description><b>evade</b>: retreating a short distance, possibly rushing back into an attack afterward.</description></item>
    /// <item><description><b>death</b>: defeated, playing a death animation before being destroyed.</description></item>
    /// </list>
    /// </summary>
    public enum EnemyState { normal, death, hurt, shocked, alert, attack, webbed, evade }
    public EnemyState eState;




    // Narrows Animator access down to a small, documented API. Built in Start() around the existing 'anim' reference. See AnimationDriver.
    private AnimationDriver animDriver;




    // Plays one-shot sound effects without re-declaring clip arrays and Random.Range logic at every call site. Built in Start(). See AudioController.
    private AudioController audioController;
    private AudioController webAudioController;




    // Answers "what's ahead / is there a hazard nearby" without this class needing its own copies of those raycast/overlap checks. Built in Start(). See TerrainSensor2D.
    private TerrainSensor2D terrainSensor;




    // Sound Files
    [SerializeField] private AudioSource audioSrc;
    private AudioSource webAudioSrc;
    [SerializeField] private AudioClip sndAlert;
    [SerializeField] private AudioClip sndAlert2;
    [SerializeField] private AudioClip sndAlert3;
    [SerializeField] private AudioClip sndAttack;
    [SerializeField] private AudioClip sndAttack2;
    [SerializeField] private AudioClip sndHit;
    [SerializeField] private AudioClip sndHit2;
    [SerializeField] private AudioClip sndHit3;
    [SerializeField] private AudioClip sndLand;
    [SerializeField] private AudioClip sndStep;
    [SerializeField] private AudioClip sndWebbedStruggle;
    [SerializeField] private AudioClip sndWebbedEscape;
    private AudioClip sndQuickHit;
    private AudioClip sndQuickHit2;
    private AudioClip sndStrongHit;
    private AudioClip sndStrongHit2;
    private AudioClip sndCarBreak;
    private bool wasGrounded = false;
    private bool hasPlayedStep1;
    private bool hasPlayedStep2;




    // Alarms
    private int alarm1;
    private int alarm2 = 0;
    [SerializeField] private int alarm3 = 0;
    [SerializeField] public int alarm4 = 0;
    public int alarm5 = 0;
    [SerializeField] private int alarm6 = 0;
    private bool startAlarm1 = true;
    private bool startAlarm2 = false;
    private bool startAlarm6 = false;
    [SerializeField] private float distanceFromPlayer = 0f;
    public int alarm7 = 0;




    // Combat
    private Material outline;
    [SerializeField] private PlayerStep player;
    [SerializeField] private bool noHitWall;
    private bool noHitHazard;
    [SerializeField] private bool shocked = false;
    public UnityEvent<PlayerStep> OnAttack;
    public bool kick = false;
    public bool attacking = false;
    public bool collidedWithPlayer = false;
    private bool backstep = false;
    [SerializeField] private bool breakingWeb = false;
    [SerializeField] private GameObject hitParticlePrefab;
    private float evadeTimer = 0f;
    private float evadeDir = 1f;
    private bool evadeWillRush = false;
    private float evadeRushDelay = 0f;
    private float retargetGraceTimer = 0f;
    public bool isEngaged = true;
    private float launchGraceTimer = 0f;
    public float swingKickHitCooldown = 0f;
    private int hitStreak = 0;
    private bool hazardBlockedAlert = false;
    private bool wallBlockedAlert = false;
    private bool escapingHazard = false;
    private float hazardEscapeDir = 1f;




    /// <summary>
    /// Provides a mapping between movement states and their corresponding hit animation names.
    /// </summary>
    private static readonly Dictionary<MovementState, string> HitAnimNames = new Dictionary<MovementState, string>
    {
        { MovementState.hurt1, "Enemy_Hit1" },
        { MovementState.hurt2, "Enemy_Hit2" },
        { MovementState.launched, "Enemy_Launched" },
    };




    /// <summary>
    /// Plays the hit animation corresponding to the specified movement state, if available.
    /// </summary>
    /// <param name="state">The movement state for which to play the associated hit animation.</param>
    private void PlayHitAnimation(MovementState state)
    {
        animDriver.SetMovementState((int)state);

        if (HitAnimNames.TryGetValue(state, out string clipName))
            animDriver.Play(clipName, 0f);
    }




    // health bar
    private int health = 25;
    private int maxHealth = 25;
    HealthBar healthbar;




    // specialized vars for level objects
    private float wireHitCooldown = 0f;
    private bool wireWasActive = false;
    private float lightningHitCooldown = 0f;
    private bool lightningWasActive = false;
    private float hitCooldown = 0f;




    // mission level specific vars
    [SerializeField] private bool keyGiver = false;
    [SerializeField] private string keyColor = "nothing";
    private bool gaveKey = false;
    [SerializeField] private GameObject keyPrefab;




    private bool hasFallen = false;




    /// <summary>True while this enemy should physically block the player as a solid barrier (part of <see cref="IEnemyBarrier"/>). False while dead, hurt, attacking, or webbed, so the player can pass through in those states.</summary>
    public bool IsSolidToPlayer => eState == EnemyState.normal || eState == EnemyState.shocked || eState == EnemyState.alert || eState == EnemyState.evade;




    /// <summary>True while the player is allowed to select this enemy as a melee target (part of <see cref="IEnemyBarrier"/>). False while dead, mid-retreat, or during the brief grace period right after being released as a target.</summary>
    public bool IsTargetable => eState != EnemyState.death && !(eState == EnemyState.evade && evadeTimer > 0f) && retargetGraceTimer <= 0f;




    /// <summary>This enemy's collider, used by the player's barrier-blocking logic (part of <see cref="IEnemyBarrier"/>).</summary>
    public Collider2D BarrierCollider => coll;




    /// <summary>
    /// Nudges this enemy a small step sideways, unless something solid is immediately in the way.
    /// </summary>
    /// <param name="dir">Which way to push: negative nudges left, positive nudges right.</param>
    public void NudgeAway(float dir)
    {
        CharacterPhysics2D.NudgeAwayFromOverlap(rb, dir, jumpableGround);
    }




    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<BoxCollider2D>();
        eState = EnemyState.normal;

        // Duplicate audioSrc's settings onto a new dedicated AudioSource
        webAudioSrc = gameObject.AddComponent<AudioSource>();
        webAudioSrc.outputAudioMixerGroup = audioSrc.outputAudioMixerGroup;
        webAudioSrc.spatialBlend = audioSrc.spatialBlend;
        webAudioSrc.volume = audioSrc.volume;
        webAudioSrc.pitch = audioSrc.pitch;
        webAudioSrc.rolloffMode = audioSrc.rolloffMode;
        webAudioSrc.minDistance = audioSrc.minDistance;
        webAudioSrc.maxDistance = audioSrc.maxDistance;
        webAudioSrc.dopplerLevel = audioSrc.dopplerLevel;
        webAudioSrc.spread = audioSrc.spread;
        webAudioSrc.priority = audioSrc.priority;

        if (!setCustomStartingDir) { dirX = UnityEngine.Random.Range(0, 2) == 0 ? 1 : -1; }
        alarm1 = waitTime;
        outline = sprite.material;
        animDriver = new AnimationDriver(anim);
        audioController = new AudioController(audioSrc);
        webAudioController = new AudioController(webAudioSrc);
        terrainSensor = new TerrainSensor2D(rb, coll, jumpableGround);
        player.OnHit.AddListener((x) => OnPlayerHit(x));
        sndQuickHit = player.sndQuickHit;
        sndQuickHit2 = player.sndQuickHit2;
        sndStrongHit = player.sndStrongHit;
        sndStrongHit2 = player.sndStrongHit2;
        sndCarBreak = player.sndCarBreak;
        healthbar = GetComponentInChildren<HealthBar>();
        healthbar.UpdateHealthBar(health, maxHealth);
        cam = Camera.main;
    }




    void Update()
    {
        if (!IsInsideExtendedView(3.5f))
        {
            return;
        }

        if (swingKickHitCooldown > 0f)
            swingKickHitCooldown -= Time.deltaTime;

        if (retargetGraceTimer > 0f)
            retargetGraceTimer -= Time.deltaTime;

        UpdateHazardEscape();

        // Outline Shader Color Control
        if (eState == EnemyState.attack) { outline.color = Color.red; }
        else if (player.currentTarget == this) { outline.color = Color.white; }
        else { outline.color = Color.black; }

        collidedWithPlayer = Physics2D.Raycast(transform.position, transform.right * -dirX, 0.65f, playerMask);

        AnimatorStateInfo stateInfo = animDriver.CurrentState;
        distanceFromPlayer = Vector3.Distance(player.transform.position, transform.position);
        noHitWall = !Physics2D.Raycast(transform.position, (player.transform.position - transform.position).normalized, distanceFromPlayer, jumpableGround);


        bool legitZeroAnimSpeed = eState == EnemyState.hurt && stateInfo.IsName("Enemy_Launched") && stateInfo.normalizedTime >= 1f;

        if (animDriver.Speed == 0f && !legitZeroAnimSpeed)
            animDriver.Speed = 1f;


        Vector2 start = transform.position;
        Vector2 end = player.transform.position;
        RaycastHit2D[] hits = Physics2D.LinecastAll(start, end);
        noHitHazard = true;

        foreach (var hit in hits)
        {
            if (hit.collider != null)
            {
                LightningScript lightning = hit.collider.GetComponent<LightningScript>();

                if (lightning != null && lightning.phase == 0)
                {
                    noHitHazard = false;
                    break;
                }
            }
        }

        if (startAlarm1)
        {
            if (alarm1 > 0)
            {
                alarm1 -= 1;
            }
            else
            {
                if (eState == EnemyState.normal)
                {
                    lastspd = dirX;
                    dirX = 0;
                    startAlarm1 = false;
                    startAlarm2 = true;
                }

                alarm2 = 240;
            }
        }

        if (startAlarm2)
        {
            if (alarm2 > 0)
            {
                alarm2 -= 1;
            }
            else
            {
                if (eState == EnemyState.normal)
                {
                    if (lastspd == 0) { lastspd = UnityEngine.Random.Range(0, 2) == 0 ? 1 : -1; }
                    dirX = -lastspd;
                    startAlarm2 = false;
                    startAlarm1 = true;
                }

                alarm1 = waitTime;
            }
        }

        if (alarm3 > 0)
        {
            alarm3 -= 1;
        }
        else
        {
            if (shocked && eState != EnemyState.attack && eState != EnemyState.webbed)
            {
                if ((distanceFromPlayer <= 3f) && ((!sprite.flipX && transform.position.x < player.transform.position.x) || (sprite.flipX && transform.position.x > player.transform.position.x)) && noHitWall)
                    alarm3 = 300;
                else
                    shocked = false;
            }

            if (eState == EnemyState.attack || eState == EnemyState.webbed)
            {
                shocked = true;
                alarm3 = 3;
            }
        }

        if (alarm4 > 0)
        {
            alarm4 -= 4;
        }
        else
        {
            if (eState == EnemyState.alert && !escapingHazard)
            {
                if (distanceFromPlayer >= 4.5f || !noHitWall) eState = EnemyState.normal;
                bool canAttack = !player.isEnemyAttacking && Vector3.Distance(player.transform.position, transform.position) <= 2.05f && ((!sprite.flipX && transform.position.x < player.transform.position.x) || (sprite.flipX && transform.position.x > player.transform.position.x)) && noHitWall && noHitHazard;

                if (canAttack)
                {
                    // 25% chance to evade instead of attacking when the player is in range
                    bool doEvade = Grounded() && UnityEngine.Random.Range(0, 4) == 0;
                    bool evaded = doEvade && StartEvasion();

                    if (!evaded)
                    {
                        eState = EnemyState.attack;
                        audioController.PlayRandom(sndAttack, sndAttack2);
                        rb.gravityScale = 0;
                        int hitIndex = UnityEngine.Random.Range(0, 3);
                        MovementState mstate = MovementState.idle;

                        switch (hitIndex)
                        {
                            case 0:
                                {
                                    mstate = MovementState.punch1;
                                    animDriver.Speed = 0.6f;
                                }
                                break;

                            case 1:
                                {
                                    mstate = MovementState.punch2;
                                    animDriver.Speed = 0.6f;
                                }
                                break;

                            case 2:
                                {
                                    mstate = MovementState.kick;
                                    kick = true;
                                    animDriver.Speed = 1f;
                                }
                                break;
                        }

                        animDriver.SetMovementState((int)mstate);
                        player.isEnemyAttacking = true;
                    }
                }
                else
                {
                    // Reset timer and occasionally evade anyway
                    bool doEvade = Grounded() && UnityEngine.Random.Range(0, 6) == 0;
                    bool evaded = doEvade && StartEvasion();

                    if (!evaded)
                    {
                        int hitIndex = UnityEngine.Random.Range(0, 3);

                        switch (hitIndex)
                        {
                            case 0: alarm4 = 300; break;
                            case 1: alarm4 = 400; break;
                            case 2: alarm4 = 500; break;
                        }
                    }
                }
            }
        }

        if (alarm5 > 0)
        {
            alarm5 -= 1;
        }
        else
        {
            if (eState == EnemyState.webbed && !breakingWeb)
            {
                webAudioSrc.Stop();
                webAudioController.Play(sndWebbedEscape);
                animDriver.SetMovementState(15);
                breakingWeb = true;
            }
        }

        if (startAlarm6)
        {
            if (alarm6 > 0)
            {
                alarm6 -= 1;
            }
            else
            {
                if (eState == EnemyState.death)
                {
                    Destroy(gameObject);
                }
            }
        }

        if (alarm7 > 0)
        {
            alarm7 -= 1;
        }
        else
        {
            if (eState == EnemyState.webbed && !breakingWeb)
            {
                webAudioController.PlayRandomWithSilenceChance(sndWebbedStruggle);
                alarm7 = 30;
            }
        }

        if (health <= 0)
        {
            if (eState != EnemyState.death)
            {
                webAudioSrc.Stop();
            }

            eState = EnemyState.death;
        }

        if (eState != EnemyState.webbed)
            breakingWeb = false;


        switch (eState)
        {
            // Patrolling: walks back and forth, turning around at hazards, ledges, or blocking objects. Escalates to "shocked" the moment the player comes into sight.
            case EnemyState.normal:
                {
                    float normalVelX = dirX * hsp;

                    bool movingTowardPlayer = Mathf.Sign(dirX) == Mathf.Sign(player.transform.position.x - transform.position.x);

                    if (collidedWithPlayer && movingTowardPlayer && !player.IsPhysicallyPassable())
                        normalVelX = 0f;

                    // Don't walk into hazards or off a ledge during patrol
                    if (dirX != 0f)
                    {
                        float moveDir = Mathf.Sign(dirX);
                        bool hazardAhead = terrainSensor.IsHazardAhead(moveDir, 0.6f, 0.9f);
                        bool ledgeAhead = !terrainSensor.IsGroundAhead(moveDir, coll.bounds.extents.x + 0.3f);
                        bool blockingAhead = terrainSensor.IsBlockingObjectAhead(moveDir, 0.6f, 0.9f);

                        if (hazardAhead || ledgeAhead || blockingAhead)
                        {
                            normalVelX = 0f;
                            dirX = -dirX;
                            lastspd = dirX;
                        }
                    }

                    rb.velocity = new Vector2(normalVelX, rb.velocity.y);

                    if ((((Math.Abs(transform.position.x - player.transform.position.x) <= 3f) && ((!sprite.flipX && transform.position.x < player.transform.position.x) || (sprite.flipX && transform.position.x > player.transform.position.x))) || collidedWithPlayer) && !shocked && Grounded() && noHitWall && noHitHazard)
                    {
                        TriggerShocked();
                    }

                    if (!wasGrounded && Grounded() && eState == EnemyState.normal)
                        audioController.Play(sndLand);

                    wasGrounded = Grounded();
                }
                break;




            // Reacting to a hit: a launching hit freezes the animation airborne until landing, then returns to alert; any other hit just plays out its stagger animation. Either way, may trigger a forced evade afterward (see TryForceEvadeAfterHit).
            case EnemyState.hurt:
                {
                    if (stateInfo.IsName("Enemy_Launched"))
                    {
                        if (launchGraceTimer > 0f) launchGraceTimer -= Time.deltaTime;

                        if (stateInfo.normalizedTime >= 1f) { animDriver.Speed = 0f; }

                        if (Grounded() && launchGraceTimer <= 0f)
                        {
                            animDriver.Speed = 1f;
                            eState = EnemyState.alert;
                            TryForceEvadeAfterHit();
                        }
                    }
                    else
                    {
                        animDriver.Speed = 1f;
                        if ((stateInfo.IsName("Enemy_Hit1") && stateInfo.normalizedTime >= 1f) || (stateInfo.IsName("Enemy_Hit2") && stateInfo.normalizedTime >= 1f))
                        {
                            eState = EnemyState.alert;
                            TryForceEvadeAfterHit();
                        }
                    }
                }
                break;




            // Brief noticing player reaction (entered via TriggerShocked), once the animation finishes, becomes alert and picks a random delay before its first attack window.
            case EnemyState.shocked:
                {
                    if (stateInfo.IsName("Enemy_Shocked") && stateInfo.normalizedTime >= 1f)
                    {
                        eState = EnemyState.alert;
                        int hitIndex = UnityEngine.Random.Range(0, 3);

                        switch (hitIndex)
                        {
                            case 0: { alarm4 = 300; } break;
                            case 1: { alarm4 = 400; } break;
                            case 2: { alarm4 = 500; } break;
                        }
                    }
                }
                break;




            // Engaged with the player: closes distance if too far, backsteps if too close, and holds position at a preferred range. The alarm4 timer (counted down above this switch) eventually opens an attack window, with a chance to evade instead.
            case EnemyState.alert:
                {
                    if (Math.Abs(transform.position.x - player.transform.position.x) > 1.9f)
                    {
                        backstep = false;

                        if (transform.position.x < player.transform.position.x)
                        {
                            dirX = 1f;
                            sprite.flipX = false;
                        }
                        else
                        {
                            dirX = -1f;
                            sprite.flipX = true;
                        }
                    }
                    else if (Math.Abs(transform.position.x - player.transform.position.x) < 1.7f)
                    {
                        backstep = true;

                        if (transform.position.x < player.transform.position.x)
                        {
                            dirX = -0.6f;
                            sprite.flipX = false;
                        }
                        else
                        {
                            dirX = 0.6f;
                            sprite.flipX = true;
                        }
                    }
                    else
                    {
                        backstep = false;
                        dirX = 0f;
                    }


                    float alertVelX = dirX * (3f * hsp);


                    bool movingTowardPlayer = Mathf.Sign(dirX) == Mathf.Sign(player.transform.position.x - transform.position.x);
                    bool playerBlocked = collidedWithPlayer && movingTowardPlayer && !player.IsPhysicallyPassable();
                    bool geometryBlocked = dirX != 0f && terrainSensor.IsWallAhead(Mathf.Sign(dirX));
                    bool ledgeAheadAlert = dirX != 0f && !terrainSensor.IsGroundAhead(Mathf.Sign(dirX), coll.bounds.extents.x + 0.3f);
                    bool objectBlockedAlert = dirX != 0f && terrainSensor.IsBlockingObjectAhead(Mathf.Sign(dirX), 0.6f, 0.9f);

                    wallBlockedAlert = playerBlocked || geometryBlocked || ledgeAheadAlert || objectBlockedAlert;

                    if (wallBlockedAlert)
                        alertVelX = 0f;


                    // Don't chase or backstep straight into a hazard
                    hazardBlockedAlert = dirX != 0f && terrainSensor.IsHazardAhead(Mathf.Sign(dirX), 0.6f, 0.9f);

                    if (hazardBlockedAlert)
                        alertVelX = 0f;


                    rb.velocity = new Vector2(alertVelX, rb.velocity.y);


                    if (!wasGrounded && Grounded() && eState == EnemyState.alert) // Landing Sound Code
                        audioController.Play(sndLand);


                    wasGrounded = Grounded();
                }
                break;




            // Mid-attack: closes the last bit of distance during the animation's early frames, then returns to alert once the punch/kick animation finishes. Actual damage is dealt separately, via OnPlayerHit being called from the player's own attack resolution.
            case EnemyState.attack:
                {
                    rb.velocity = new Vector2(0f, 0f);

                    if (Math.Abs(player.transform.position.x - transform.position.x) >= 0.45f && ((stateInfo.IsName("Enemy_Kick") && stateInfo.normalizedTime <= 0.31f) || (stateInfo.IsName("Enemy_Punch1") && stateInfo.normalizedTime <= 0.45f) || (stateInfo.IsName("Enemy_Punch2") && stateInfo.normalizedTime <= 0.29f)))
                    {
                        float step = 4f * Time.deltaTime;
                        Vector2 targetPosition = new Vector2(player.transform.position.x, transform.position.y);
                        transform.position = Vector2.MoveTowards(transform.position, targetPosition, step);
                        if (targetPosition.x < transform.position.x) { sprite.flipX = true; } else { sprite.flipX = false; }
                    }

                    if ((stateInfo.IsName("Enemy_Punch1") && stateInfo.normalizedTime >= 1f) || (stateInfo.IsName("Enemy_Punch2") && stateInfo.normalizedTime >= 1f) || (stateInfo.IsName("Enemy_Kick") && stateInfo.normalizedTime >= 1f))
                    {
                        int hitIndex = UnityEngine.Random.Range(0, 3);

                        switch (hitIndex)
                        {
                            case 0: { alarm4 = 300; } break;
                            case 1: { alarm4 = 400; } break;
                            case 2: { alarm4 = 500; } break;
                        }

                        eState = EnemyState.alert;
                        player.isEnemyAttacking = false;
                        animDriver.Speed = 1f;
                        kick = false;
                        rb.gravityScale = 1;
                    }
                }
                break;




            // Immobilized by a web shot: frozen in place until the break-free animation completes, then returns to alert. Struggle sounds are played separately by the alarm7 timer above this switch block.
            case EnemyState.webbed:
                {
                    rb.velocity = new Vector2(0f, 0f);
                    shocked = true;

                    if (animDriver.CurrentState.IsName("Enemy_BreakFree") && (animDriver.CurrentState.normalizedTime >= 1f))
                    {
                        eState = EnemyState.alert;
                    }
                }
                break;




            // Retreating a short random distance (started via StartEvasion), stopping early if it would run off a ledge. Once the retreat finishes, either rushes straight into an attack (if evadeWillRush rolled true and the player is still reachable) or settles back into alert.
            case EnemyState.evade:
                {
                    if (evadeTimer > 0f)
                    {
                        bool groundAhead = terrainSensor.IsGroundAhead(evadeDir, 0.3f);

                        if (!groundAhead)
                        {
                            // Stop right here instead of running off the edge
                            evadeTimer = 0f;
                            retargetGraceTimer = 0.05f;
                            rb.velocity = new Vector2(0f, rb.velocity.y);
                            sprite.flipX = (evadeDir > 0);
                        }
                        else
                        {
                            evadeTimer -= Time.deltaTime;

                            if (evadeTimer <= 0f)
                                retargetGraceTimer = 0.05f;

                            rb.velocity = new Vector2(evadeDir * hsp * 4.5f, rb.velocity.y);
                            sprite.flipX = (evadeDir > 0);
                        }
                    }
                    else
                    {
                        if (evadeRushDelay > 0f)
                        {
                            // Brief pause before rushing
                            rb.velocity = new Vector2(0f, rb.velocity.y);
                            evadeRushDelay -= Time.deltaTime;
                        }
                        else
                        {
                            // Rush into attack or return to alert
                            if (evadeWillRush && distanceFromPlayer <= 5f && noHitWall)
                            {
                                isEngaged = true;

                                // Jump straight to attack state
                                eState = EnemyState.attack;
                                audioController.PlayRandom(sndAttack, sndAttack2);
                                rb.gravityScale = 0;


                                int hitIndex = UnityEngine.Random.Range(0, 3);
                                MovementState mstate2 = MovementState.idle;

                                switch (hitIndex)
                                {
                                    case 0:
                                        {
                                            mstate2 = MovementState.punch1;
                                            animDriver.Speed = 0.6f;
                                        }
                                        break;


                                    case 1:
                                        {
                                            mstate2 = MovementState.punch2;
                                            animDriver.Speed = 0.6f;
                                        }
                                        break;


                                    case 2:
                                        {
                                            mstate2 = MovementState.kick;
                                            kick = true;
                                            animDriver.Speed = 1f;
                                        }
                                        break;
                                }

                                animDriver.SetMovementState((int)mstate2);
                                player.isEnemyAttacking = true;
                            }
                            else
                            {
                                isEngaged = true;
                                eState = EnemyState.alert;
                                alarm4 = UnityEngine.Random.Range(80, 160);
                            }
                        }
                    }
                }
                break;
        }


        if (escapingHazard)
        {
            float escapeSpeed = hsp * 5f;
            rb.velocity = new Vector2(hazardEscapeDir * escapeSpeed, rb.velocity.y);
            dirX = hazardEscapeDir;
            sprite.flipX = hazardEscapeDir < 0f;
        }


        UpdateAnimationState();
    }




    private void UpdateAnimationState()
    {
        if (!((eState == EnemyState.alert && backstep) || eState == EnemyState.attack || eState == EnemyState.evade))
        {
            if (dirX > 0f)
                sprite.flipX = false;
            else if (dirX < 0f)
                sprite.flipX = true;
        }

        if (eState == EnemyState.hurt) return;
        if (eState == EnemyState.webbed) return;
        if (eState == EnemyState.shocked) return;
        if (eState == EnemyState.attack) return;

        MovementState mstate = MovementState.idle;

        if (eState == EnemyState.normal)
        {
            if (dirX > 0f)
                mstate = MovementState.running;
            else if (dirX < 0f)
                mstate = MovementState.running;
            else
                mstate = MovementState.idle;

            if (rb.velocity.y < -0.1f && !Grounded()) { mstate = MovementState.falling; }
        }

        if (eState == EnemyState.evade)
        {
            animDriver.SetMovementState((int)MovementState.backstep);
            return;
        }

        if (eState == EnemyState.alert)
        {
            if (hazardBlockedAlert || wallBlockedAlert)
            {
                mstate = MovementState.alertidle;
            }
            else if (dirX > 0f)
            {
                if (backstep)
                    mstate = MovementState.backstep;
                else
                    mstate = MovementState.sprinting;
            }
            else if (dirX < 0f)
            {
                if (backstep)
                    mstate = MovementState.backstep;
                else
                    mstate = MovementState.sprinting;
            }
            else
            {
                mstate = MovementState.alertidle;
            }

            if (rb.velocity.y < -0.1f && !Grounded()) { mstate = MovementState.falling; }
        }

        if (eState == EnemyState.death)
        {
            animDriver.Speed = 1f;
            mstate = MovementState.death;
        }

        AnimatorStateInfo stateInfo = animDriver.CurrentState;
        float normalizedTime = stateInfo.normalizedTime % 1f;

        if (mstate == MovementState.running)
        {
            if (normalizedTime >= 0.21f && normalizedTime <= 0.24f && !hasPlayedStep1)
            {
                audioController.Play(sndStep);
                hasPlayedStep1 = true;
            }
            else if (normalizedTime >= 0.67f && normalizedTime <= 0.70f && !hasPlayedStep2)
            {
                audioController.Play(sndStep);
                hasPlayedStep2 = true;
            }

            if (normalizedTime < 0.05f)
            {
                hasPlayedStep1 = false;
                hasPlayedStep2 = false;
            }
        }
        else if (mstate == MovementState.sprinting)
        {
            if (normalizedTime >= 0.45f && normalizedTime <= 0.55f && !hasPlayedStep1)
            {
                audioController.Play(sndStep);
                hasPlayedStep1 = true;
            }
            else if (normalizedTime >= 0.90f && normalizedTime <= 1.00f && !hasPlayedStep2)
            {
                audioController.Play(sndStep);
                hasPlayedStep2 = true;
            }

            if (normalizedTime < 0.05f)
            {
                hasPlayedStep1 = false;
                hasPlayedStep2 = false;
            }
        }
        else if (mstate == MovementState.backstep)
        {
            if (normalizedTime >= 0.60f && normalizedTime <= 0.68f && !hasPlayedStep1)
            {
                audioController.Play(sndStep);
                hasPlayedStep1 = true;
            }
            else if (normalizedTime >= 0.90f && normalizedTime <= 1.00f && !hasPlayedStep2)
            {
                audioController.Play(sndStep);
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

        if (mstate == MovementState.death)
        {
            if (normalizedTime >= 0.352f && normalizedTime <= 0.389f)
            {
                if (Grounded() && !hasFallen)
                {
                    audioController.Play(sndLand);
                    hasFallen = true;
                }

                if (!startAlarm6)
                {
                    alarm6 = 240;
                    startAlarm6 = true;
                }

                if (keyGiver && !gaveKey)
                {
                    if (player.keys < 3)
                    {
                        player.keys += 1;
                        GameObject key = Instantiate(keyPrefab, new Vector3(transform.position.x, transform.position.y, transform.position.z), Quaternion.identity);

                        if (player.keys == 1)
                            key.GetComponent<Keys>().keyIndex = 1;
                        else if (player.keys == 2)
                            key.GetComponent<Keys>().keyIndex = 2;
                        else if (player.keys == 3)
                            key.GetComponent<Keys>().keyIndex = 3;

                        key.GetComponent<Keys>().keyColor = keyColor;
                        gaveKey = true;
                    }
                }
            }

            if (normalizedTime == 1f)
            {
                animDriver.Speed = 0f;
                normalizedTime = 1f;
            }
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
    /// Called when the player's attack (or counter) connects with this robot. Applies damage, picks
    /// a hurt reaction (a launching uppercut hit vs. a regular hit), plays the matching hit sound and
    /// FX, and updates the health bar. Combat-recovery timing alarm4 is re-rolled here too,
    /// so a robot that just got hit doesn't immediately re-attack.
    /// </summary>
    /// <param name="target">This robot itself, passed through from the player's hit-resolution event.</param>
    public void OnPlayerHit(RobotStep target)
    {
        player.isEnemyAttacking = false;

        if (target == this)
        {
            if (eState == EnemyState.webbed)
            {
                webAudioSrc.Stop();
            }

            rb.gravityScale = 1;
            hitStreak++;
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

            if (player.uppercut)
                rb.velocity = new Vector2(dir, 5f);
            else if ((player.combo - 4) % 5 == 0)
                rb.velocity = new Vector2(2.5f * dir, 0f);
            else
                rb.velocity = new Vector2(dir, 0f);

            animDriver.Speed = 1f;
            eState = EnemyState.hurt;

            int attackTime = UnityEngine.Random.Range(0, 3);

            switch (attackTime)
            {
                case 0: { alarm4 = 300; } break;
                case 1: { alarm4 = 400; } break;
                case 2: { alarm4 = 500; } break;
            }

            MovementState mstate;

            if (player.uppercut)
            {
                mstate = MovementState.launched;
                launchGraceTimer = 0.15f;
                audioController.PlayRandom(sndStrongHit, sndStrongHit2);
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
            }

            PlayHitAnimation(mstate);
            Vector2 hitPoint = transform.position;
            player.SpawnHitEffect(hitPoint);

            if (health > 0)
            {
                if ((player.combo - 4) % 5 == 0)
                    health -= 7;
                else if (player.countering)
                    health -= 3;
                else if (player.uppercut)
                    health -= 5;
                else
                    health -= 4;

                healthbar.UpdateHealthBar(health, maxHealth);
            }

            audioController.PlayRandom(sndHit, sndHit2, sndHit3);
        }
    }




    /// <summary>
    /// Transitions this robot into the brief "shocked" alert reaction: turns to face the player, plays
    /// the alert sound and animation, and stops moving. Called either when the player comes into range 
    /// during patrol, or when <see cref="NotifyPlayerBlocked"/> reports the player physically ran 
    /// into this robot.
    /// </summary>
    private void TriggerShocked()
    {
        sprite.flipX = transform.position.x >= player.transform.position.x;

        eState = EnemyState.shocked;
        audioController.PlayRandom(sndAlert, sndAlert2, sndAlert3);
        animDriver.SetMovementState((int)MovementState.shocked);
        rb.velocity = new Vector2(0f, rb.velocity.y);
        animDriver.Speed = 1f;
        shocked = true;
        alarm3 = 300;
        collidedWithPlayer = false;
    }




    /// <summary>
    /// Called by the player when it physically collides with this robot while the robot is still
    /// patrolling and unaware. Triggers the shocked/alert reaction immediately, as long as this
    /// robot is grounded and has clear line of sight (not blocked by a wall or an active hazard).
    /// </summary>
    public void NotifyPlayerBlocked()
    {
        if (eState != EnemyState.normal) return;
        if (shocked) return;
        if (!Grounded()) return;
        if (!noHitWall || !noHitHazard) return;

        TriggerShocked();
    }




    /// <summary>
    /// Called from an Animation Event on this robot's attack impact frame. Deals damage to the 
    /// player if they're still within range by that point in the animation.
    /// </summary>
    public void AttackEvent()
    {
        if (Vector3.Distance(player.transform.position, transform.position) <= 0.55f) { player.Damage(this); }
    }




    /// <summary>
    /// True if this robot is currently within the given camera's viewport.
    /// </summary>
    /// <param name="cam">The camera to check visibility against.</param>
    public bool IsOnScreen(Camera cam)
    {
        Vector3 viewportPos = cam.WorldToViewportPoint(transform.position);
        return viewportPos.x > 0 && viewportPos.x < 1 && viewportPos.y > 0 && viewportPos.y < 1 && viewportPos.z > 0;
    }




    /// <summary>
    /// Spawns a visual hit effect at the specified impact point when this object collides with another game object.
    /// </summary>
    /// <param name="impactPoint">The world-space position where the hit effect should be instantiated.</param>
    /// <param name="other">The other game object involved in the collision. Used to determine the hit position.</param>
    public void SpawnObjectHitEffect(Vector2 impactPoint, GameObject other)
    {
        Vector3 hitPosition = (transform.position + other.transform.position) / 2f;
        GameObject hitFX = Instantiate(hitParticlePrefab, impactPoint, Quaternion.identity);
    }




    /// <summary>
    /// Attempts to begin the evade state, retreating away from the player. Picks the direction away
    /// from the player first, but falls back to the opposite direction if that's not safe (a hazard,
    /// blocking object, or ledge), and gives up entirely if neither direction is safe.
    /// </summary>
    /// <returns>True if evasion actually started; false if no safe direction was available.</returns>
    private bool StartEvasion()
    {
        float dir = (transform.position.x < player.transform.position.x) ? -1f : 1f;

        if (!terrainSensor.IsDirectionSafeToEvade(dir))
        {
            float altDir = -dir;

            if (terrainSensor.IsDirectionSafeToEvade(altDir))
                dir = altDir;
            else
                return false;
        }

        eState = EnemyState.evade;
        isEngaged = false;
        hitStreak = 0;

        player.ReleaseTargetIfCurrent(this);

        evadeDir = dir;
        evadeTimer = UnityEngine.Random.Range(0.35f, 0.65f);

        evadeWillRush = UnityEngine.Random.Range(0, 2) == 0;
        evadeRushDelay = evadeWillRush ? UnityEngine.Random.Range(0.2f, 0.5f) : 0f;

        return true;
    }




    /// <summary>
    /// Rolls a chance to force this robot into evasion right after taking a hit, guaranteed after 2 or more hits in a row (hitStreak), a coin flip otherwise. Only applies while grounded.
    /// </summary>
    private void TryForceEvadeAfterHit()
    {
        if (!Grounded()) return;

        // Guaranteed evade after 2+ hits in a row
        float evadeChance = hitStreak >= 2 ? 1f : 0.5f;

        if (UnityEngine.Random.value < evadeChance)
            StartEvasion();
    }




    /// <summary>
    /// Checks whether this robot is currently standing inside a hazard, and if so, sets
    /// escapingHazard/hazardEscapeDir so the state machine can move it clear. Does nothing
    /// while dead, webbed, hurt, or attacking.
    /// </summary>
    private void UpdateHazardEscape()
    {
        if (eState == EnemyState.death || eState == EnemyState.webbed || eState == EnemyState.hurt || eState == EnemyState.attack)
        {
            escapingHazard = false;
            return;
        }

        Collider2D hazard = terrainSensor.GetOverlappingHazard();

        if (hazard == null)
        {
            escapingHazard = false;
            return;
        }

        escapingHazard = true;
        hazardEscapeDir = transform.position.x >= hazard.bounds.center.x ? 1f : -1f;
    }




    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Car"))
        {
            rb.WakeUp();

            Animator carAnim = collision.GetComponent<Animator>();
            bool carNormal = carAnim.GetCurrentAnimatorStateInfo(0).IsName("CarNormal");
            AnimatorStateInfo stateInfo = animDriver.CurrentState;

            if (carNormal && eState == EnemyState.attack && ((transform.position.x > collision.transform.position.x && sprite.flipX) || (transform.position.x < collision.transform.position.x && !sprite.flipX)) && ((stateInfo.IsName("Enemy_Kick") && stateInfo.normalizedTime <= 0.31f) || (stateInfo.IsName("Enemy_Punch1") && stateInfo.normalizedTime <= 0.45f) || (stateInfo.IsName("Enemy_Punch2") && stateInfo.normalizedTime <= 0.29f)))
            {
                rb.WakeUp();
                rb.position = rb.position;
                audioController.Play(sndCarBreak);
                collision.GetComponent<Animator>().Play("CarBreak");
            }
        }

        if (collision.gameObject.CompareTag("Wires"))
        {
            rb.WakeUp();
            if (eState == EnemyState.death) return;

            Animator wireAnim = collision.GetComponent<Animator>();
            bool wireIsActive = wireAnim.GetCurrentAnimatorStateInfo(0).IsName("WiresActive");

            if (wireIsActive && !wireWasActive)
            {
                wireHitCooldown = 0f;
                rb.WakeUp();
                rb.position = rb.position;
            }

            wireWasActive = wireIsActive;

            if (!wireIsActive)
            {
                wireHitCooldown = 0f;
                return;
            }

            if (wireHitCooldown > 0f)
            {
                wireHitCooldown -= Time.deltaTime;
                return;
            }

            wireHitCooldown = 0.05f;

            float dir = sprite.flipX ? 1 : -1;
            rb.velocity = new Vector2(dir, 5f);
            animDriver.Speed = 1f;
            eState = EnemyState.hurt;

            int attackTime = UnityEngine.Random.Range(0, 3);

            switch (attackTime)
            {
                case 0: { alarm4 = 300; } break;
                case 1: { alarm4 = 400; } break;
                case 2: { alarm4 = 500; } break;
            }

            MovementState mstate = MovementState.launched;

            audioController.PlayRandom(sndStrongHit, sndStrongHit2);

            animDriver.SetMovementState((int)mstate);

            Vector2 hitPoint = transform.position;
            SpawnObjectHitEffect(hitPoint, collision.gameObject);

            health -= 8;
            healthbar.UpdateHealthBar(health, maxHealth);
        }

        if (collision.gameObject.CompareTag("Lightning"))
        {
            rb.WakeUp();
            if (eState == EnemyState.death) return;

            Animator wireAnim = collision.GetComponent<Animator>();
            bool wireIsActive = wireAnim.GetCurrentAnimatorStateInfo(0).IsName("LightningActive");

            if (wireIsActive && !lightningWasActive)
            {
                lightningHitCooldown = 0f;
                rb.WakeUp();
                rb.position = rb.position;
            }

            lightningWasActive = wireIsActive;

            if (!wireIsActive)
            {
                return;
            }

            if (lightningHitCooldown > 0f)
            {
                lightningHitCooldown -= Time.deltaTime;
                return;
            }

            lightningHitCooldown = 0.05f;

            float dir = sprite.flipX ? 1 : -1;
            rb.velocity = new Vector2(dir, 5f);
            animDriver.Speed = 1f;
            eState = EnemyState.hurt;

            int attackTime = UnityEngine.Random.Range(0, 3);

            switch (attackTime)
            {
                case 0: { alarm4 = 300; } break;
                case 1: { alarm4 = 400; } break;
                case 2: { alarm4 = 500; } break;
            }

            MovementState mstate = MovementState.launched;

            audioController.PlayRandom(sndStrongHit, sndStrongHit2);

            animDriver.SetMovementState((int)mstate);

            Vector2 hitPoint = transform.position;
            SpawnObjectHitEffect(hitPoint, collision.gameObject);

            health -= 8;
            healthbar.UpdateHealthBar(health, maxHealth);
        }

        if (collision.gameObject.CompareTag("OneHitHazard"))
        {
            if (eState == EnemyState.death) return;

            rb.WakeUp();
            rb.position = rb.position;

            if (hitCooldown > 0f)
            {
                hitCooldown -= Time.deltaTime;
                return;
            }

            hitCooldown = 0.15f;

            float dir = sprite.flipX ? 1f : -1f;
            rb.velocity = new Vector2(dir * 2f, 5f);
            animDriver.Speed = 1f;
            eState = EnemyState.hurt;

            MovementState mstate;
            int hitIndex = UnityEngine.Random.Range(0, 2); // 0 or 1

            if (hitIndex == 0)
                mstate = MovementState.hurt1;
            else
                mstate = MovementState.hurt2;

            audioController.PlayRandom(sndQuickHit, sndQuickHit2);

            animDriver.SetMovementState((int)mstate);

            Vector2 hitPoint = transform.position;
            SpawnObjectHitEffect(hitPoint, collision.gameObject);

            health -= 8;
            healthbar.UpdateHealthBar(health, maxHealth);
        }

        if (collision.gameObject.CompareTag("Hydrant"))
        {
            if (!collision.GetComponent<FireHydrant>().webbed)
            {
                if (eState == EnemyState.death) return;

                rb.WakeUp();
                rb.position = rb.position;

                if (hitCooldown > 0f)
                {
                    hitCooldown -= Time.deltaTime;
                    return;
                }

                hitCooldown = 0.15f;

                float dir = sprite.flipX ? 1f : -1f;
                rb.velocity = new Vector2(dir * 2f, 5f);
                animDriver.Speed = 1f;
                eState = EnemyState.hurt;

                MovementState mstate;
                int hitIndex = UnityEngine.Random.Range(0, 2); // 0 or 1

                if (hitIndex == 0)
                    mstate = MovementState.hurt1;
                else
                    mstate = MovementState.hurt2;

                audioController.PlayRandom(sndQuickHit, sndQuickHit2);

                animDriver.SetMovementState((int)mstate);

                Vector2 hitPoint = transform.position;
                SpawnObjectHitEffect(hitPoint, collision.gameObject);

                health -= 8;
                healthbar.UpdateHealthBar(health, maxHealth);
            }
        }
    }




    /// <summary>
    /// True if this robot is within a margin around the camera's view, larger than the camera's
    /// actual viewport by <paramref name="extensionFactor"/>. <see cref="Update"/> skips this robot's
    /// entire AI logic when this returns false, as a cheap way to avoid running dozens of robots'
    /// worth of raycasts and state machines for enemies nowhere near what the player can see.
    /// </summary>
    /// <param name="extensionFactor">How much larger than the camera's actual view to check, e.g. 3.5 checks an area 3.5x the camera's width/height.</param>
    bool IsInsideExtendedView(float extensionFactor)
    {
        if (!cam) return false;

        float camHeight = cam.orthographicSize * 2f;
        float camWidth = camHeight * cam.aspect;

        float extWidth = camWidth * extensionFactor;
        float extHeight = camHeight * extensionFactor;

        Vector3 camPos = cam.transform.position;

        float minX = camPos.x - extWidth / 2f;
        float maxX = camPos.x + extWidth / 2f;
        float minY = camPos.y - extHeight / 2f;
        float maxY = camPos.y + extHeight / 2f;

        Bounds b = sprite.bounds;

        bool overlap = b.max.x >= minX && b.min.x <= maxX && b.max.y >= minY && b.min.y <= maxY;

        return overlap;
    }
}