using System;
using System.Collections;
using System.Collections.Generic;
using Framework.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;


/// <summary>
/// Controls the player character's movement, combat, and interaction logic, including swinging,
/// wall crawling, zipping, attacking, and handling environmental hazards.
/// </summary>


[RequireComponent(typeof(PlayerInputReader))]
public class PlayerStep : MonoBehaviour
{
    // Reads raw keyboard input and exposes it as named properties; auto-fetched below in Start()
    private PlayerInputReader playerInput;


    // Narrows Animator access down to the small set of calls the player logic below actually needs; built in Start() around the existing 'anim' reference below
    private AnimationDriver animDriver;


    // Plays one-shot sound effects without every call site re-declaring its own clip array and Random.Range logic; built in Start() around the existing 'audioSrc' reference below
    private AudioController audioController;


    // Computes barrier-aware run velocity and detects landings. See PlayerGroundMovement.
    private PlayerGroundMovement groundMovement;


    // Simulates the rope's pendulum physics; built in Start() using the existing accelerationRate field below, which stays [SerializeField] for inspector tuning
    private PlayerRopePhysics ropePhysics;


    // Draws the rope/zip-line as pooled segments; built in Start() using the existing ropeSegmentPrefab field, which stays [SerializeField] for inspector assignment
    private PlayerRopeRenderer ropeRenderer;


    // Computes crawl-surface velocity from input and surface direction (check out PlayerCrawlMovement.cs)
    private PlayerCrawlMovement crawlMovement;




    public AnimationDriver AnimationDriver => animDriver;
    public AudioController AudioController => audioController;
    public PlayerRopeRenderer RopeRenderer => ropeRenderer;




    public Rigidbody2D rb;
    [SerializeField] public Animator anim;
    [SerializeField] public SpriteRenderer sprite;
    [SerializeField] private Transform visual;
    public BoxCollider2D coll;
    public CircleCollider2D collCircle;
    public float dirX = 0f;
    public float dirY = 0f;




    // Swinging variables
    public bool swingEnd = false;
    [SerializeField] private float accelerationRate = -0.02f;
    [SerializeField] private bool swingPointSelected = false;
    private float swingAnimFullSpeedThreshold = 1.2f;
    private float swingAnimMinSpeed = 0f;




    // Swing kick variables
    private bool swingKickTriggered = false;
    private List<Component> swingKickTargets = new List<Component>();
    private float swingKickCooldown = 0f;
    private const float SwingKickVelocityThreshold = 2.0f;




    // Crawl kick variables
    private bool crawlKickTriggered = false;




    // Crawling variables
    [SerializeField] private bool groundDetected;
    [SerializeField] private bool wallDetected;
    [SerializeField] private Transform groundPositionChecker;
    [SerializeField] private Transform wallPositionChecker;
    [SerializeField] private Transform ceilingPositionChecker;
    private float wallCheckDistance = 0.1f;
    private float ceilingCheckDistance = 0.1f;




    // Which surface the player is currently oriented to: 1 = floor, 2 = left wall, 3 = ceiling, 4 = right wall
    private int direction;




    private float crawlDir = 0f;
    private bool shoot = false;
    private float zipFromCrawlGrace = 0f;
    private Vector2 crawlShootAimDir = Vector2.zero;




    // True once an inner-corner turn has been triggered for the current approach, so it doesn't re-trigger every frame while still close to the corner
    private bool hasTurnInner = false;


    // True while an inner-corner turn coroutine (RotateAroundCornerInner) is actively runnin
    private bool isTurningInner = false;


    // True once an outer-corner turn has been triggered for the current approach
    private bool hasTurnOuter = false;


    // True while an outer-corner turn coroutine (RotateAroundCornerOuter) is actively running
    private bool isTurningOuter = false;


    // Short debounce timer used when detecting an outer corner, to avoid triggering on a single noisy frame
    private float cliffTimer = 0f;


    // True while the older, shared turn coroutine (RotateAroundCorner, used by swing/zip/CanStartCrawling) is actively running
    private bool isTurningLegacy = false;




    // Combined flags so the swing and zip code can read/reset turn state as a single value
    private bool isTurning
    {
        get => isTurningInner || isTurningOuter || isTurningLegacy;

        set
        {
            isTurningInner = value;
            isTurningOuter = value;
            isTurningLegacy = value;
        }
    }




    // Handle to whichever corner-turn coroutine is currently running, if any, so it can be stopped cleanly by CancelActiveTurn
    private Coroutine activeTurnCoroutine = null;




    // Zip variables
    [SerializeField] private Transform quickZipTarget;
    [SerializeField] private Tilemap tilemap;
    public Vector2? moveTarget = null;
    private float zipTravelDist = 0f;
    private float crawlEntryGrace = 0f;
    private bool uKeyReleaseRequired = false;




    [SerializeField] private LayerMask jumpableGround;
    [SerializeField] private LayerMask swingPoint;
    private float hsp = 4f;
    public float jspd = 5f;
    [SerializeField] public GameObject ropeSegmentPrefab;




    public enum MovementState { idle, running, jumping, falling, swinging, endswing, crawling, zip, groundshoot, airshoot, crawlshoot, punch1, punch2, punch3, punch4, airkick, airpunch, kick1, kick2, uppercut, launched, hurt1, hurt2, block1, block2, block3, block4, death, swingkick, crawlkick }
    public enum PlayerState { normal, swing, crawl, quickzip, dashenemy, hurt, death }
    public PlayerState pState;
    private PlayerState _prevPState; // last frame's state, used to detect crawl and quickzip enter/exit




    // Sound files
    [SerializeField] public AudioSource audioSrc;
    [SerializeField] private AudioClip sndJump;
    [SerializeField] private AudioClip sndJump2;
    [SerializeField] public AudioClip sndSwing;
    [SerializeField] public AudioClip sndSwing2;
    [SerializeField] public AudioClip sndSwing3;
    [SerializeField] public AudioClip sndLand;
    [SerializeField] public AudioClip sndLand2;
    [SerializeField] public AudioClip sndHardLand;
    [SerializeField] public AudioClip sndHardLand2;
    [SerializeField] private AudioClip sndWebSnap;
    [SerializeField] public AudioClip sndWebRelease;
    [SerializeField] private AudioClip sndWebTension;
    [SerializeField] private AudioClip sndWebTension2;
    [SerializeField] private AudioClip sndWebTension3;
    [SerializeField] private AudioClip sndWebShoot;
    [SerializeField] public AudioClip sndStep;
    [SerializeField] public AudioClip sndStep2;
    [SerializeField] private AudioClip sndCrawlStep;
    [SerializeField] private AudioClip sndCrawlStep2;
    [SerializeField] private AudioClip sndAttack;
    [SerializeField] private AudioClip sndAttack2;
    [SerializeField] private AudioClip sndAttack3;
    [SerializeField] public AudioClip sndSwipe;
    [SerializeField] public AudioClip sndSwipe2;
    [SerializeField] public AudioClip sndSwipe3;
    [SerializeField] public AudioClip sndQuickHit;
    [SerializeField] public AudioClip sndQuickHit2;
    [SerializeField] public AudioClip sndStrongHit;
    [SerializeField] public AudioClip sndStrongHit2;
    [SerializeField] public AudioClip sndHurt;
    [SerializeField] public AudioClip sndHurt2;
    [SerializeField] public AudioClip sndHurt3;
    [SerializeField] private AudioClip sndSpiderSense;
    [SerializeField] private AudioClip sndHealth;
    [SerializeField] public AudioClip sndCarBreak;
    [SerializeField] public AudioClip sndWarning;
    [SerializeField] public AudioClip sndLevelComplete;
    [SerializeField] public AudioClip sndGoblinBoss;
    [SerializeField] public AudioClip sndBoss;
    [SerializeField] private AudioClip sndGTaunt1;
    [SerializeField] private AudioClip sndGTaunt2;
    [SerializeField] private AudioClip sndGTaunt3;


    private float senseSoundTimer = 0f;


    // Alarms
    private int alarm1 = 0;
    private int alarm2 = 0;
    private bool startAlarm2 = false;
    public int alarm3 = 0;
    public int alarm4 = 0;


    public bool trigger = false;


    // Combat
    public RobotStep currentTarget = null;
    public RobotStep currentCounter = null;
    public bool isEnemyAttacking = false;
    [SerializeField] private LayerMask enemyMask;
    private float dash_spd = 0f;
    public UnityEvent<RobotStep> OnHit;
    public UnityEvent<GoblinStep> OnHitG;
    public UnityEvent<ShockerStep> OnHitS;
    [SerializeField] private bool waitingToHit = false;
    [SerializeField] private GameObject hitParticlePrefab;
    [SerializeField] private GameObject hurtParticlePrefab;
    public bool uppercut = false;
    public Vector3 enemyHitSpawn = new Vector3(0f, 0f, 0f);
    public bool attacking = false;
    public bool countering = false;
    [SerializeField] private bool pastHitEvent = false;
    [SerializeField] private GameObject webPrefab;
    [SerializeField] private GameObject sensePrefab;
    public bool spiderSense = false;
    public int combo = 0;
    [SerializeField] private Text comboText;
    private float postAttackBuffer = 0f;
    private bool postAttackWasGrounded = false;
    private float attackTimeoutTimer = 0f;
    private float attackCooldown = 0f;
    private float attackCooldownDuration = 0.6f;    // minimum time between attack starts
    private float airAttackCooldownDuration = 0.52f; // minimum time between air kicks specifically
    private float launchedFreezeTimer = 0f;
    public float launchGroundGrace = 0f;
    public float launchTechTimer = 0f;
    private float launchTechWindow = 0.35f; // time before jump can cancel launched hitstun
    private bool oKeyReleaseRequired = false;


    // Owns target resolution: deciding which nearby enemy is a valid attack/counter target. Constructed in Start(). See PlayerCombatTargeting for why boss/shocker moved here rather than staying as PlayerStep fields (nothing external ever read them).
    private PlayerCombatTargeting combatTargeting;


    // True while a counter target is currently selected. Read by SenseScript to decide when to show the spider-sense warning
    public bool HasCounterTarget => combatTargeting.HasCounterTarget;


    [SerializeField] private string titleSceneName = "Title Screen";




    /// <summary>
    /// Sets the attack cooldown duration based on whether the character is grounded or airborne.
    /// </summary>
    private void SetAttackCooldown()
    {
        attackCooldown = Grounded() ? attackCooldownDuration : airAttackCooldownDuration;
    }




    // Health bar
    [SerializeField] public int health = 80;
    [SerializeField] public int maxHealth = 80;
    [SerializeField] public HealthBar healthbar;


    [SerializeField] private Material noOutlineMaterial;


    // Level object interactions
    private float wireHitCooldown = 0f;
    private bool wireWasActive = false;
    private int barrierContactDir = 0; // -1 is a barrier to the left, 1 is a barrier to the right, 0 is none
    private Collider2D blockingEnemyCollider = null;
    private float lightningHitCooldown = 0f;
    private bool lightningWasActive = false;
    private float hitCooldown = 0f;
    public int keys = 0;
    public string keyColor1 = "nothing";
    public string keyColor2 = "nothing";
    public string keyColor3 = "nothing";
    public bool stopMove = false;




    /// <summary>
    /// Calculates the dash speed based on the horizontal distance to the specified target and whether the dash is a
    /// counter action.
    /// </summary>
    /// <param name="target">The target transform whose horizontal position is used to determine the dash speed. Cannot be null.</param>
    /// <param name="isCounter">Indicates whether the dash is performed as a counter action. If <see langword="true"/>, higher speeds are used.</param>
    /// <returns>The dash speed, in units per second, determined by the distance to the target and the dash type.</returns>
    private float CalcDashSpeed(Transform target, bool isCounter = false)
    {
        float dist = Mathf.Abs(target.position.x - transform.position.x);

        if (isCounter)
        {
            if (dist > 3.75f) return 24f;
            if (dist > 2.5f) return 18f;
            if (dist > 1.25f) return 12f;
            return 6f;
        }
        else
        {
            if (dist > 3.75f) return 16f;
            if (dist > 2.5f) return 12f;
            if (dist > 1.25f) return 8f;
            return 4f;
        }
    }




    /// <summary>
    /// Selects a random attack animation state based on whether the character is grounded.
    /// </summary>
    /// <returns>A MovementState value representing the chosen attack animation. Returns one of several punch or kick states if
    /// grounded; otherwise, returns MovementState.airkick.</returns>
    private MovementState PickAttackAnimation()
    {
        if (Grounded())
        {
            int hitIndex = UnityEngine.Random.Range(0, 7);

            return hitIndex switch
            {
                0 => MovementState.punch1,
                1 => MovementState.punch2,
                2 => MovementState.punch3,
                3 => MovementState.punch4,
                4 => MovementState.kick1,
                5 => MovementState.kick2,
                _ => MovementState.airpunch
            };
        }

        return MovementState.airkick;
    }
    



    /// <summary>
    /// Selects a random counter animation state from the available block movement states.
    /// </summary>
    /// <returns>One of the MovementState values representing a block animation. The returned value is randomly chosen from
    /// block1, block2, block3, or block4.</returns>
    private MovementState PickCounterAnimation()
    {
        int hitIndex = UnityEngine.Random.Range(0, 4);

        return hitIndex switch
        {
            0 => MovementState.block1,
            1 => MovementState.block2,
            2 => MovementState.block3,
            _ => MovementState.block4
        };
    }




    /// <summary>
    /// Plays the attack and swipe sound effects associated with an attack action.
    /// </summary>
    private void PlayAttackSounds()
    {
        audioController.PlayRandomWithSilenceChance(sndAttack, sndAttack2, sndAttack3);
        audioController.PlayRandom(sndSwipe, sndSwipe2, sndSwipe3);
    }




    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<BoxCollider2D>();
        collCircle = GetComponent<CircleCollider2D>();
        playerInput = GetComponent<PlayerInputReader>();
        animDriver = new AnimationDriver(anim);
        audioController = new AudioController(audioSrc);
        combatTargeting = new PlayerCombatTargeting();
        groundMovement = new PlayerGroundMovement();
        ropePhysics = new PlayerRopePhysics(accelerationRate);
        ropeRenderer = new PlayerRopeRenderer(ropeSegmentPrefab);
        crawlMovement = new PlayerCrawlMovement();
        pState = PlayerState.normal;
        direction = 1;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        healthbar.UpdateHealthBar(health, maxHealth);
        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Enemy"), true);
    }




    void FixedUpdate()
    {
        if (pState == PlayerState.swing)
        {
            Vector2 currentPos = rb.position;
            Vector2 targetPos = ropePhysics.Position;
            Vector2 delta = targetPos - currentPos;
            float dist = delta.magnitude;

            if (dist > 0.0001f)
            {
                float castRadius = Mathf.Min(coll.size.x, coll.size.y) * 0.4f;
                RaycastHit2D swingHit = Physics2D.CircleCast(currentPos, castRadius, delta.normalized, dist, jumpableGround);

                if (swingHit.collider != null)
                {
                    ForceExitSwingToCrawl(swingHit.point, swingHit.normal);
                    return; // don't MovePosition into/through it this frame
                }
            }

            rb.MovePosition(targetPos);
        }
    }




    void Update()
    {
        playerInput.RefreshInput();

        // Crawl and zip are no-clip states, so toggle the collider trigger flag on enter and exit
        bool isNoClipState = (pState == PlayerState.crawl || pState == PlayerState.quickzip);
        bool wasNoClipState = (_prevPState == PlayerState.crawl || _prevPState == PlayerState.quickzip);

        if (isNoClipState && !wasNoClipState)
        {
            coll.isTrigger = true;
        }
        else if (!isNoClipState && wasNoClipState)
        {
            coll.isTrigger = false;
        }

        // Launched animation can freeze forever if Grounded() never fires; force recovery after a timeout
        if (pState == PlayerState.hurt && animDriver.Speed == 0f)
        {
            launchedFreezeTimer += Time.deltaTime;

            if (launchedFreezeTimer > 1.5f)
            {
                animDriver.Speed = 1f;
                pState = PlayerState.normal;
                launchedFreezeTimer = 0f;
            }
        }
        else
        {
            launchedFreezeTimer = 0f;
        }

        _prevPState = pState;

        // If something forced us out of crawl while a corner turn was mid-flight, the coroutine is still running unsupervised so cancel it
        if (pState != PlayerState.crawl && isTurning)
            CancelActiveTurn();

        // Getting hit while crawl-kicking orphans this flag, which then blocks all future animation updates in UpdateAnimationState()
        if (pState == PlayerState.hurt && crawlKickTriggered)
            crawlKickTriggered = false;

        // Getting hit while the swing-end animation is still playing orphans this flag the same way, permanently blocking UpdateAnimationState()
        if (pState == PlayerState.hurt && swingEnd)
            swingEnd = false;

        UpdateBarrierContact();
        UpdateEnemyTopBlock();

        if (blockingEnemyCollider != null)
        {
            RobotStep blockedRobot = blockingEnemyCollider.GetComponent<RobotStep>();
            blockedRobot?.NotifyPlayerBlocked();
        }

        if (senseSoundTimer > 0) senseSoundTimer -= Time.deltaTime;
        if (attackCooldown > 0f) attackCooldown -= Time.deltaTime;
        if (launchGroundGrace > 0f) launchGroundGrace -= Time.deltaTime;

        AnimatorStateInfo stateInfo = animDriver.CurrentState;

        if (swingEnd && stateInfo.IsName("Player_Swing_End") && stateInfo.normalizedTime >= 1f)
            swingEnd = false;

        if (pState == PlayerState.crawl)
        {
            float crawlSign = crawlDir >= 0 ? 1f : -1f;
            wallPositionChecker.localPosition = new Vector2(0.325f * crawlSign, -0.389f);
        }
        else
        {
            if (dirX > 0)
                wallPositionChecker.localPosition = new Vector2(0.325f, -0.389f);
            else
                wallPositionChecker.localPosition = new Vector2(-0.325f, -0.389f);
        }

        Vector2? bestCorner = FindClosestTileTopCorner(transform.position);

        if (bestCorner.HasValue)
        {
            quickZipTarget.position = bestCorner.Value;
            quickZipTarget.gameObject.SetActive(true);
        }
        else
        {
            quickZipTarget.gameObject.SetActive(false);
        }

        // Web tension sound
        if (alarm1 > 0)
        {
            alarm1--;
        }
        else
        {
            if (pState == PlayerState.swing)
            {
                audioController.PlayRandom(sndWebTension, sndWebTension2, sndWebTension3);
                alarm1 = 400;
            }
        }

        // Death timer
        if (startAlarm2)
        {
            if (alarm2 > 0)
            {
                alarm2--;
            }
            else
            {
                if (pState == PlayerState.death)
                {
                    SceneManager.LoadScene(titleSceneName);
                }
            }
        }

        // Combo reset
        if (alarm3 > 0)
            alarm3--;
        else if (combo > 0)
            combo = 0;

        // Trigger
        if (trigger)
        {
            if (alarm4 > 0)
                alarm4--;
            else
                trigger = false;
        }

        // Counter detection, look for enemies currently in their attack state
        Vector2 origin = transform.position;
        float closestEDistanceC = Mathf.Infinity;
        RobotStep closestCounter = null;
        Collider2D[] ehitsC = Physics2D.OverlapCircleAll(origin, 5.2f, enemyMask);

        foreach (var ehitC in ehitsC)
        {
            RobotStep enemyC = ehitC.GetComponent<RobotStep>();
            if (enemyC == null || enemyC.eState == RobotStep.EnemyState.death) continue;
            if (enemyC.eState != RobotStep.EnemyState.attack) continue;


            RaycastHit2D hitC = Physics2D.Linecast(transform.position, enemyC.transform.position, jumpableGround);
            if (hitC.collider != null && (Vector2)hitC.point != (Vector2)enemyC.transform.position) continue;


            float distC = Mathf.Abs(enemyC.transform.position.x - origin.x);

            if (distC < closestEDistanceC)
            {
                closestEDistanceC = distC;
                closestCounter = enemyC;
            }
        }

        if (!countering)
        {
            Component candidate = closestCounter; // existing RobotStep result

            if (combatTargeting.Boss != null && combatTargeting.Boss.gState == GoblinStep.GoblinState.attack)
            {
                float bossDist = Mathf.Abs(combatTargeting.Boss.transform.position.x - origin.x);
                RaycastHit2D hitB = Physics2D.Linecast(transform.position, combatTargeting.Boss.transform.position, jumpableGround);
                bool bossVisible = hitB.collider == null || (Vector2)hitB.point == (Vector2)combatTargeting.Boss.transform.position;

                if (bossVisible && (candidate == null || bossDist < closestEDistanceC))
                    candidate = combatTargeting.Boss;
            }

            currentCounter = candidate as RobotStep;   // keep old field in sync for anything else referencing it
            combatTargeting.CurrentCounterTarget = candidate;
        }

        RefreshCombatTarget(origin);

        // Spider sense
        if ((trigger || combatTargeting.CurrentCounterTarget != null) && !spiderSense && pState != PlayerState.death)
        {
            Instantiate(sensePrefab, transform.position, Quaternion.identity);

            if (senseSoundTimer <= 0f)
            {
                audioController.Play(sndSpiderSense);
                senseSoundTimer = sndSpiderSense.length;
            }

            spiderSense = true;
        }
        else if (!trigger && combatTargeting.CurrentCounterTarget == null)
        {
            spiderSense = false;
        }

        if (health <= 0)
            pState = PlayerState.death;

        if (health > maxHealth)
            health = maxHealth;

        comboText.text = combo == 0 ? "" : "x" + combo;

        if (pState != PlayerState.quickzip && pState != PlayerState.swing)
            ropeRenderer.ReturnAllToPool();

        keys = Mathf.Clamp(keys, 0, 3);

        // Gravity scale can get stuck at 0 after an attack ends, this is a safety net to catch that
        if (rb.gravityScale == 0f)
        {
            bool gravityLegitZero = pState == PlayerState.swing || pState == PlayerState.crawl || pState == PlayerState.quickzip || (pState == PlayerState.dashenemy && (attacking || countering));
            if (!gravityLegitZero) rb.gravityScale = 1f;
        }

        // Swing kick anim speed can get stuck at 0 after a swing kick, this is a safety net to catch that
        if (swingKickTriggered && animDriver.Speed == 0f)
            animDriver.Speed = 1f;

        // Attack anim speed can get stuck at 0 if waitingToHit never resolves
        if (attacking && animDriver.Speed == 0f && !waitingToHit)
            animDriver.Speed = 1f;

        if (stopMove)
            pState = PlayerState.normal;

        if (uKeyReleaseRequired && !playerInput.ShootHeld)
            uKeyReleaseRequired = false;

        if (oKeyReleaseRequired && !playerInput.AttackHeld)
            oKeyReleaseRequired = false;




        switch (pState)
        {
            case PlayerState.normal:
                {
                    visual.rotation = Quaternion.Euler(0, 0, 0);

                    bool movementKey = playerInput.IsMovementKeyHeld;
                    bool otherKeyPressed = playerInput.IsAnyKeyHeld && !movementKey;

                    if (!otherKeyPressed)
                    {
                        coll.size = new Vector2(0.8397379f, 1.615343f);
                        coll.offset = new Vector2(-0.03511286f, -0.03012538f);
                    }

                    if (!stopMove)
                    {
                        dirX = playerInput.Horizontal;
                        dirY = -playerInput.Vertical;
                    }

                    rb.velocity = groundMovement.ComputeHorizontalVelocity(dirX, hsp, rb.velocity.y, barrierContactDir);

                    // Jump
                    if (playerInput.JumpPressed && Grounded() && !shoot && !stopMove)
                    {
                        audioController.PlayRandomWithSilenceChance(sndJump, sndJump2);
                        rb.velocity = new Vector2(rb.velocity.x, jspd);
                    }

                    // Swing
                    if (playerInput.JumpPressed && !Grounded() && !shoot && !stopMove)
                    {
                        Vector2 playerPos = transform.position;
                        Vector2 inputDir = new Vector2(dirX * 2.5f, -dirY * 1.25f);
                        Vector2 searchOrigin = playerPos + inputDir;

                        Debug.DrawLine(transform.position, searchOrigin, Color.cyan);

                        LayerMask combinedMask = jumpableGround | swingPoint;
                        Collider2D[] hits = Physics2D.OverlapCircleAll(searchOrigin, 3f, combinedMask);
                        float closestDistance = float.MaxValue;
                        Vector2 bestAttachPoint = Vector2.zero;
                        bool bestIsSwingPoint = false;
                        bool found = false;
                        Collider2D bestCollider = null;

                        foreach (Collider2D hit in hits)
                        {
                            bool isSwingPoint = ((1 << hit.gameObject.layer) & swingPoint) != 0;
                            Vector2 point = isSwingPoint ? (Vector2)hit.transform.position : hit.ClosestPoint(searchOrigin);

                            if (point.y <= playerPos.y) continue;
                            if (dirX > 0 && point.x <= playerPos.x) continue;
                            if (dirX < 0 && point.x >= playerPos.x) continue;
                            if (IsBarrierBetween(playerPos, point)) continue;

                            float dist = Vector2.Distance(playerPos, point);
                            bool shouldReplace = !found || (isSwingPoint && !bestIsSwingPoint) || (isSwingPoint == bestIsSwingPoint && dist < closestDistance);

                            if (shouldReplace)
                            {
                                closestDistance = dist;
                                bestAttachPoint = point;
                                bestIsSwingPoint = isSwingPoint;
                                swingPointSelected = bestIsSwingPoint;
                                bestCollider = hit;
                                found = true;
                            }
                        }

                        if (found)
                        {
                            rb.gravityScale = 0;
                            ropePhysics.Attach(bestAttachPoint, transform.position);
                            coll.size = new Vector2(1.339648f, 1.561783f);
                            coll.offset = new Vector2(-0.6135812f, -0.6907219f);
                            audioController.PlayRandom(sndSwing, sndSwing2, sndSwing3);
                            alarm1 = 400;
                            swingEnd = false;
                            pState = PlayerState.swing;

                            GliderScript attachedGlider = bestCollider != null ? bestCollider.GetComponentInParent<GliderScript>() : null;

                            if (attachedGlider != null && (attachedGlider.state == GliderScript.GState.Shooting || attachedGlider.state == GliderScript.GState.Throwing || attachedGlider.state == GliderScript.GState.AirFight))
                            {
                                audioController.PlayRandom(sndGTaunt1, sndGTaunt2, sndGTaunt3);
                            }
                        }
                    }

                    if (CanStartCrawling())
                    {
                        pState = PlayerState.crawl;
                        rb.gravityScale = 0;
                    }

                    // Quick zip to the nearest exposed tile corner
                    if (playerInput.QuickZipPressed && !stopMove)
                    {
                        if (bestCorner.HasValue)
                        {
                            moveTarget = bestCorner.Value;
                            coll.size = new Vector2(0.7719507f, 1.863027f);
                            coll.offset = new Vector2(-0.3766563f, -0.968719f);
                            audioController.PlayRandom(sndSwing, sndSwing2, sndSwing3);

                            pState = PlayerState.quickzip;
                            zipTravelDist = 0f;
                            Vector2 zipNudgeDir = (moveTarget.Value - (Vector2)transform.position).normalized;
                            TeleportRigidbody((Vector2)transform.position + zipNudgeDir * 0.3f);
                            rb.gravityScale = 0;
                        }
                        else
                        {
                            moveTarget = null;
                        }
                    }

                    // Normal zip, aimed via held input direction and confirmed with a raycast
                    if (playerInput.ShootHeld && !stopMove && !uKeyReleaseRequired)
                    {
                        rb.velocity = new Vector2(0f, rb.velocity.y);
                        shoot = true;

                        if (Grounded())
                        {
                            if ((dirX != 0) && dirY >= 0) animDriver.Play("Player_Ground_Shoot", 0.33f);
                            else if ((dirX != 0) && dirY < 0) animDriver.Play("Player_Ground_Shoot", 0.66f);
                            else if (dirX == 0 && dirY < 0) animDriver.Play("Player_Ground_Shoot", 0.99f);
                        }
                        else
                        {
                            if ((dirX != 0) && dirY >= 0) animDriver.Play("Player_Air_Shoot", 0.33f);
                            else if ((dirX != 0) && dirY < 0) animDriver.Play("Player_Air_Shoot", 0.66f);
                            else if (dirX == 0 && dirY < 0) animDriver.Play("Player_Air_Shoot", 0.99f);
                        }

                        Vector2 playerPos = transform.position;
                        Vector2 exactDir = new Vector2(dirX, -dirY).normalized;
                        float maxZipRange = 12f;

                        RaycastHit2D rayHit = Physics2D.Raycast(playerPos, exactDir, maxZipRange, jumpableGround);
                        Vector2 bestAttachPoint = Vector2.zero;
                        bool found = false;

                        if (rayHit.collider != null && !IsBarrierBetween(playerPos, rayHit.point))
                        {
                            bestAttachPoint = rayHit.point;
                            found = true;
                        }

                        if (found && playerInput.JumpPressed)
                        {
                            rb.gravityScale = 0;
                            moveTarget = bestAttachPoint;
                            coll.size = new Vector2(0.7719507f, 1.863027f);
                            coll.offset = new Vector2(-0.3766563f, -0.968719f);
                            Vector2 zipNudgeDir = (moveTarget.Value - (Vector2)transform.position).normalized;
                            TeleportRigidbody((Vector2)transform.position + zipNudgeDir * 0.3f);
                            audioController.PlayRandom(sndSwing, sndSwing2, sndSwing3);

                            pState = PlayerState.quickzip;
                            zipTravelDist = 0f;
                        }
                    }
                    else
                    {
                        if (shoot && !stopMove)
                        {
                            if (playerInput.ShootReleased && (animDriver.CurrentState.IsName("Player_Ground_Shoot") || animDriver.CurrentState.IsName("Player_Air_Shoot")))
                            {
                                float hRaw = playerInput.Horizontal;
                                float vRaw = playerInput.Vertical;
                                Quaternion rot = transform.rotation;

                                if (hRaw > 0 && vRaw == 0) rot = transform.rotation;
                                else if (hRaw > 0 && vRaw > 0) rot = transform.rotation * Quaternion.Euler(0f, 0f, 45f);
                                else if (hRaw == 0 && vRaw > 0) rot = transform.rotation * Quaternion.Euler(0f, 0f, 90f);
                                else if (hRaw < 0 && vRaw > 0) rot = transform.rotation * Quaternion.Euler(0f, 0f, 135f);
                                else if (hRaw < 0 && vRaw == 0) rot = transform.rotation * Quaternion.Euler(0f, 0f, 180f);
                                else rot = sprite.flipX ? transform.rotation * Quaternion.Euler(0f, 0f, 180f) : transform.rotation;

                                Instantiate(webPrefab, transform.position, rot);
                                audioController.Play(sndWebShoot);
                            }

                            shoot = false;
                        }
                    }

                    // Landing sound
                    switch (groundMovement.UpdateLandingState(Grounded(), pState == PlayerState.normal, rb.velocity.y))
                    {
                        case PlayerGroundMovement.LandingResult.Hard:
                            audioController.PlayRandom(sndHardLand, sndHardLand2);
                            break;

                        case PlayerGroundMovement.LandingResult.Soft:
                            audioController.PlayRandom(sndLand, sndLand2);
                            break;
                    }

                    // Enemy targeting
                    bool facingLeft = sprite.flipX;
                    Collider2D[] ehits = Physics2D.OverlapCircleAll(origin, 5.2f, enemyMask);

                    float closestEDistance = Mathf.Infinity;
                    RobotStep closestEnemy = null;

                    foreach (var ehit in ehits)
                    {
                        RobotStep enemy = ehit.GetComponent<RobotStep>();
                        if (enemy == null || enemy.eState == RobotStep.EnemyState.death || !enemy.IsTargetable) continue;

                        RaycastHit2D hit = Physics2D.Linecast(transform.position, enemy.transform.position, jumpableGround);
                        if (hit.collider != null && (Vector2)hit.point != (Vector2)enemy.transform.position) continue;

                        // Skip this enemy if a live hazard is blocking the line of sight
                        bool noLightning = true;

                        foreach (var hl in Physics2D.LinecastAll(transform.position, enemy.transform.position))
                        {
                            LightningScript ls = hl.collider?.GetComponent<LightningScript>();

                            if (ls != null && ls.phase == 0)
                            {
                                noLightning = false;
                                break;
                            }
                        }

                        if (!noLightning) continue;


                        float dx = enemy.transform.position.x - origin.x;
                        if ((facingLeft && dx > 0) || (!facingLeft && dx < 0)) continue;


                        float dist = Mathf.Abs(dx);

                        if (dist < closestEDistance)
                        {
                            closestEDistance = dist;
                            closestEnemy = enemy;
                        }
                    }

                    currentTarget = closestEnemy;
                    combatTargeting.CurrentTarget = combatTargeting.ResolveTarget(origin, facingLeft, transform, jumpableGround, closestEnemy, closestEDistance);

                    // Attack
                    if (playerInput.AttackHeld && combatTargeting.CurrentTarget != null && !stopMove && attackCooldown <= 0f && !oKeyReleaseRequired)
                    {
                        StartAttackTowardTarget(combatTargeting.TargetTransform, attacking: true);
                    }
                    else if (playerInput.AttackHeld && combatTargeting.CurrentTarget == null && !stopMove && attackCooldown <= 0f && !oKeyReleaseRequired)
                    {
                        StartAttackTowardTarget(null, attacking: false);
                    }

                    // Uppercut
                    if (playerInput.UppercutHeld && Grounded() && !stopMove && attackCooldown <= 0f)
                    {
                        bool targetClose = combatTargeting.CurrentTarget != null && Mathf.Abs(combatTargeting.TargetTransform.position.x - origin.x) <= 1f;
                        dash_spd = targetClose ? CalcDashSpeed(combatTargeting.TargetTransform) : 0f;
                        attacking = targetClose;
                        uppercut = targetClose;
                        pState = PlayerState.dashenemy;
                        animDriver.Speed = 2f;
                        PlayAttackAnimation(MovementState.uppercut);
                        rb.gravityScale = targetClose ? 0 : 1;
                        SetAttackCooldown();
                        PlayAttackSounds();
                    }

                    // Counter
                    if (playerInput.CounterHeld && Grounded() && !stopMove)
                    {
                        if (combatTargeting.CurrentCounterTarget != null)
                        {
                            dash_spd = CalcDashSpeed(combatTargeting.CounterTargetTransform, isCounter: true);
                            countering = true;
                            combatTargeting.CounterTargetAnim.speed = 0f;
                            pState = PlayerState.dashenemy;
                            sprite.flipX = combatTargeting.CounterTargetTransform.position.x < transform.position.x;
                            animDriver.Speed = 2f;
                            animDriver.SetMovementState((int)PickCounterAnimation());
                            rb.gravityScale = 0;
                            PlayAttackSounds();
                        }
                        else
                        {
                            dash_spd = 0f;
                            countering = false;
                            pState = PlayerState.dashenemy;
                            animDriver.Speed = 1.5f;
                            animDriver.SetMovementState((int)PickCounterAnimation());
                            PlayAttackSounds();
                        }
                    }
                }
                break;




            case PlayerState.swing:
                {
                    if (swingPointSelected)
                    {
                        GameObject swingPointObj = GameObject.Find("SwingPoint");
                        ropePhysics.SetGrapplePoint(swingPointObj.transform.position);
                    }

                    dirX = playerInput.Horizontal;
                    dirY = -playerInput.Vertical;
                    ropePhysics.Step(dirX, dirY);

                    if (swingKickCooldown > 0f) swingKickCooldown -= Time.deltaTime;

                    // Trigger a swing kick once angular speed is high enough to know we're actually swinging, not idling
                    if (!swingKickTriggered && swingKickCooldown <= 0f && Mathf.Abs(ropePhysics.AngularVelocity) >= (0.65f * SwingKickVelocityThreshold))
                    {
                        List<Component> arcEnemies = ScanSwingArc();

                        if (arcEnemies.Count > 0)
                        {
                            swingKickTargets = arcEnemies;
                            swingKickTriggered = true;

                            if (!animDriver.CurrentState.IsName("Player_Swing_Kick"))
                            {
                                audioController.PlayRandom(sndSwipe, sndSwipe2, sndSwipe3);
                            }

                            animDriver.Speed = 1f;
                            animDriver.Play("Player_Swing_Kick", 0f);
                        }
                    }

                    // Reset kick state once the animation finishes
                    if (swingKickTriggered)
                    {
                        AnimatorStateInfo kickState = animDriver.CurrentState;

                        if (kickState.IsName("Player_Swing_Kick") && kickState.normalizedTime >= 1f)
                        {
                            swingKickTriggered = false;
                            swingKickTargets.Clear();
                            swingKickCooldown = 0.4f;
                        }
                    }

                    Vector2 ropeDirection = (ropePhysics.Position - ropePhysics.GrapplePoint).normalized;
                    float ropeAngleDeg = Mathf.Atan2(ropeDirection.y, ropeDirection.x) * Mathf.Rad2Deg;
                    visual.rotation = Quaternion.Euler(0, 0, ropeAngleDeg + 90);

                    if (playerInput.JumpReleased)
                    {
                        rb.velocity = new Vector2(rb.velocity.x, jspd);
                        rb.gravityScale = 1;
                        animDriver.SetMovementState((int)MovementState.endswing);
                        audioController.Play(sndWebRelease);
                        ExitSwing();
                        pState = PlayerState.normal;
                        swingEnd = true;
                    }

                    float dirOff = sprite.flipX ? -1f : 1f;
                    bool nearWall = Physics2D.Raycast(new Vector2(wallPositionChecker.position.x - 0.315f, wallPositionChecker.position.y - 0.372f), transform.right * dirX, wallCheckDistance, jumpableGround);
                    bool nearCeiling = Physics2D.Raycast(new Vector2(ceilingPositionChecker.position.x - 0.53f, ceilingPositionChecker.position.y - 0.68f), transform.up, ceilingCheckDistance, jumpableGround);
                    bool onGround = Grounded();

                    if (onGround)
                    {
                        audioController.Play(sndWebSnap);
                        ExitSwing();
                        pState = PlayerState.normal;
                        rb.gravityScale = 1;
                    }
                    else if (nearWall && dirOff > 0)
                    {
                        hasTurnInner = false;
                        hasTurnOuter = false;
                        ExitSwing(resetTransformRotation: true);
                        StartCoroutine(RotateAroundCorner(new Vector3(-0.1f, 0.1f, 0), 90f, 4));
                    }
                    else if (nearWall && dirOff < 0)
                    {
                        hasTurnInner = false;
                        hasTurnOuter = false;
                        visual.rotation = Quaternion.Euler(0, 0, 0);
                        animDriver.Speed = 1f;
                        StartCoroutine(RotateAroundCorner(new Vector3(0.1f, 0.1f, 0), -90f, 2));
                    }
                    else if (nearCeiling)
                    {
                        hasTurnInner = false;
                        hasTurnOuter = false;
                        visual.rotation = Quaternion.Euler(0, 0, 0);
                        animDriver.Speed = 1f;
                        StartCoroutine(RotateAroundCorner(new Vector3(0f, 0.15f, 0), 180f, 3));
                    }

                    if (nearWall || nearCeiling)
                    {
                        coll.size = new Vector2(0.8397379f, 1.615343f);
                        coll.offset = new Vector2(-0.03511286f, -0.03012538f);
                        audioController.Play(sndWebSnap);
                        swingPointSelected = false;
                        pState = PlayerState.crawl;
                        ropeRenderer.ReturnAllToPool();
                        rb.gravityScale = 0;
                        swingKickTriggered = false;
                        swingKickTargets.Clear();
                        swingKickCooldown = 0f;
                    }
                }
                break;




            case PlayerState.crawl:
                {
                    if (crawlEntryGrace > 0f) crawlEntryGrace -= Time.deltaTime;
                    swingEnd = false;
                    groundMovement.WasGrounded = true;
                    dirX = playerInput.Horizontal;


                    // Crawl kick: resets itself once the animation finishes
                    if (crawlKickTriggered)
                    {
                        AnimatorStateInfo kickState = animDriver.CurrentState;

                        if (kickState.IsName("Player_Crawl_Kick") && kickState.normalizedTime >= 1f)
                            crawlKickTriggered = false;
                    }

                    // Trigger a stationary crawl kick, only usable while crawling, doesn't move/dash
                    if (!crawlKickTriggered && playerInput.AttackPressed && !stopMove && !shoot && attackCooldown <= 0f)
                    {
                        crawlKickTriggered = true;
                        rb.velocity = Vector2.zero;
                        animDriver.Speed = 1f;
                        PlayAttackAnimation(MovementState.crawlkick);
                        attackCooldown = attackCooldownDuration;
                        PlayAttackSounds();
                    }

                    // While the kick plays, freeze crawl movement and skip everything else this state normally does
                    if (crawlKickTriggered)
                    {
                        crawlDir = 0f;
                        rb.velocity = Vector2.zero;
                        break;
                    }


                    PlayerCrawlMovement.Result crawlResult = crawlMovement.ComputeVelocity(direction, playerInput.Horizontal, playerInput.Vertical, transform.right);
                    crawlDir = crawlResult.CrawlDirection;
                    Vector2 crawlVel = crawlResult.Velocity;
                    if (barrierContactDir == 1 && crawlVel.x > 0f) crawlVel.x = 0f;
                    if (barrierContactDir == -1 && crawlVel.x < 0f) crawlVel.x = 0f;

                    if (crawlDir != 0f)
                    {
                        float crawlMovSign = Mathf.Sign(crawlDir);
                        Vector2 crawlProbeOrigin = rb.position;
                        Vector2 crawlProbeDir = (Vector2)(transform.right * crawlMovSign);
                        float crawlProbeDist = coll.size.x * 0.5f + 0.2f;

                        RaycastHit2D crawlEnemyHit = Physics2D.Raycast(
                            crawlProbeOrigin, crawlProbeDir, crawlProbeDist, enemyMask);

                        if (crawlEnemyHit.collider != null && IsEnemySolid(crawlEnemyHit.collider))
                            crawlVel = Vector2.zero;
                    }

                    rb.velocity = crawlVel;

                    if (isTurning) return;

                    // Snap to the true surface using the physical distance from pivot to collider bottom edge
                    float groundOffsetMagnitude = GetGroundOffsetMagnitude();
                    float surfaceCastDist = groundOffsetMagnitude + 0.5f;

                    RaycastHit2D surfaceHit = Physics2D.Raycast(rb.position, -transform.up, surfaceCastDist, jumpableGround);

                    if (surfaceHit.collider != null)
                    {
                        rb.position = surfaceHit.point + surfaceHit.normal * groundOffsetMagnitude;
                    }

                    float halfW = Mathf.Abs(wallPositionChecker.localPosition.x) * transform.localScale.x;
                    float halfH = Mathf.Abs(groundPositionChecker.localPosition.y) * transform.localScale.y;
                    float movSign = crawlDir != 0 ? Mathf.Sign(crawlDir) : (sprite.flipX ? -1f : 1f);

                    // Inner corner, turn into a wall found ahead while crawling
                    if (crawlDir != 0 && !hasTurnInner && crawlEntryGrace <= 0f)
                    {
                        Vector2 wallRayOrigin = rb.position + (Vector2)(transform.right * halfW * movSign);
                        Vector2 wallRayOriginLow = wallRayOrigin - (Vector2)(transform.up * halfH * 0.5f);
                        wallDetected = Physics2D.Raycast(wallRayOrigin, transform.right * movSign, 0.35f, jumpableGround) || Physics2D.Raycast(wallRayOriginLow, transform.right * movSign, 0.35f, jumpableGround);

                        if (wallDetected)
                        {
                            hasTurnInner = true;

                            if (crawlDir > 0)
                            {
                                switch (direction)
                                {
                                    case 1: activeTurnCoroutine = StartCoroutine(RotateAroundCornerInner(new Vector3(-0.1f, 0.1f, 0), 90f, 4)); break;
                                    case 2: activeTurnCoroutine = StartCoroutine(RotateAroundCornerInner(new Vector3(0.3f, 0.1f, 0), 90f, 1)); break;
                                    case 3: activeTurnCoroutine = StartCoroutine(RotateAroundCornerInner(new Vector3(0.3f, -0.3f, 0), 90f, 2)); break;
                                    case 4: activeTurnCoroutine = StartCoroutine(RotateAroundCornerInner(new Vector3(-0.3f, -0.3f, 0), 90f, 3)); break;
                                }
                            }
                            else
                            {
                                switch (direction)
                                {
                                    case 1: activeTurnCoroutine = StartCoroutine(RotateAroundCornerInner(new Vector3(0.1f, 0.1f, 0), -90f, 2)); break;
                                    case 2: activeTurnCoroutine = StartCoroutine(RotateAroundCornerInner(new Vector3(-0.3f, -0.1f, 0), -90f, 3)); break;
                                    case 3: activeTurnCoroutine = StartCoroutine(RotateAroundCornerInner(new Vector3(-0.3f, 0.3f, 0), -90f, 4)); break;
                                    case 4: activeTurnCoroutine = StartCoroutine(RotateAroundCornerInner(new Vector3(-0.1f, 0.1f, 0), -90f, 1)); break;
                                }
                            }
                        }
                    }

                    // Outer corner, turn around a ledge when the ground runs out ahead
                    if (crawlDir != 0 && !hasTurnOuter && !hasTurnInner)
                    {
                        float castDist = halfH + 0.5f;
                        float footOffset = halfW * 0.8f;
                        Vector2 frontOrigin = rb.position + (Vector2)(transform.right * footOffset * movSign);
                        Vector2 frontMidOrigin = rb.position + (Vector2)(transform.right * footOffset * movSign * 0.45f);
                        Vector2 backOrigin = rb.position + (Vector2)(transform.right * -footOffset * movSign);

                        bool centerOnGround = surfaceHit.collider != null;
                        bool frontOnGround = Physics2D.Raycast(frontOrigin, -transform.up, castDist, jumpableGround) || Physics2D.Raycast(frontMidOrigin, -transform.up, castDist, jumpableGround);
                        bool backOnGround = Physics2D.Raycast(backOrigin, -transform.up, castDist, jumpableGround);
                        groundDetected = backOnGround || centerOnGround;

                        Vector2 wro = rb.position + (Vector2)(transform.right * halfW * movSign);
                        bool wallAhead = Physics2D.Raycast(wro, transform.right * movSign, 0.35f, jumpableGround) || Physics2D.Raycast(wro - (Vector2)(transform.up * halfH * 0.5f), transform.right * movSign, 0.35f, jumpableGround);

                        if (!frontOnGround && !wallAhead)
                        {
                            cliffTimer += Time.deltaTime;

                            if (cliffTimer >= 0.02f)
                            {
                                cliffTimer = 0f;
                                hasTurnOuter = true;
                                Vector2 cornerPoint = FindOuterCornerPoint(crawlDir);
                                float rotSign = crawlDir > 0 ? -1f : 1f;
                                int nextDir = GetNextDirection(direction, crawlDir > 0 ? -1 : 1);
                                StartCoroutine(RotateAroundCornerOuter(90f * rotSign, nextDir, cornerPoint));
                            }
                        }
                        else if (groundDetected && frontOnGround)
                        {
                            cliffTimer = 0f;
                        }
                    }

                    // Quick zip to the nearest exposed tile corner while crawling
                    if (playerInput.QuickZipPressed && !stopMove && bestCorner.HasValue)
                    {
                        moveTarget = bestCorner.Value;
                        coll.size = new Vector2(0.7719507f, 1.863027f);
                        coll.offset = new Vector2(-0.3766563f, -0.968719f);

                        // Push off using the surface normal detected this frame, instead of guessing per-direction
                        Vector2 playerSurfaceAway = surfaceHit.collider != null ? surfaceHit.normal : Vector2.up;
                        float playerNudgeDist = (direction == 3) ? 1.0f : 0.5f;

                        transform.rotation = Quaternion.identity;
                        direction = 1;

                        Vector2 zipNudgeDir = (moveTarget.Value - rb.position).normalized;
                        TeleportRigidbody(rb.position + zipNudgeDir * 0.15f + playerSurfaceAway * playerNudgeDist);

                        audioController.PlayRandom(sndSwing, sndSwing2, sndSwing3);

                        shoot = false;
                        animDriver.Speed = 1f;
                        zipFromCrawlGrace = 0.3f;
                        pState = PlayerState.quickzip;
                        zipTravelDist = 0f;
                        rb.gravityScale = 0;
                    }

                    if (pState == PlayerState.quickzip) break;

                    if (playerInput.JumpPressed)
                    {
                        if (direction == 1)
                        {
                            rb.gravityScale = 1;
                            pState = PlayerState.normal;
                        }
                        else
                        {
                            if (direction == 4)
                            {
                                dirX = -1;
                                transform.eulerAngles = Vector3.zero;
                                transform.Translate(new Vector3(-0.1f, 0f, 0f));
                                rb.velocity = new Vector2(-1f, jspd);
                            }
                            else if (direction == 2)
                            {
                                dirX = 1;
                                transform.eulerAngles = Vector3.zero;
                                transform.Translate(new Vector3(0.1f, 0f, 0f));
                                rb.velocity = new Vector2(1f, jspd);
                            }
                            else if (direction == 3)
                            {
                                transform.eulerAngles = Vector3.zero;
                                transform.Translate(new Vector3(0f, -0.1f, 0f));
                                rb.velocity = new Vector2(0f, -1f);
                            }

                            rb.gravityScale = 1;
                            pState = PlayerState.normal;
                        }
                    }

                    // Crawl shoot, hold U to aim a zip from the surface, confirm with space
                    if (playerInput.ShootHeld && !stopMove && !uKeyReleaseRequired)
                    {
                        rb.velocity = Vector2.zero;
                        shoot = true;

                        // Build an aim direction in surface local space
                        float hRaw = playerInput.Horizontal;
                        float vRaw = playerInput.Vertical;

                        Vector2 surfaceAimDir = ((Vector2)transform.right * hRaw + (Vector2)transform.up * vRaw).normalized;
                        if (surfaceAimDir == Vector2.zero) surfaceAimDir = transform.up;

                        crawlShootAimDir = new Vector2(-vRaw, hRaw).normalized;

                        if (crawlShootAimDir == Vector2.zero)
                        {
                            float facingSign = sprite.flipX ? -1f : 1f;
                            crawlShootAimDir = ((Vector2)transform.up * facingSign).normalized;
                        }


                        // Pick the crawl shoot animation frame based on the input direction relative to the surface
                        float logicalH, logicalAway;


                        switch (direction)
                        {
                            case 1:
                                {
                                    logicalH = hRaw;
                                    logicalAway = vRaw;
                                }
                                break;


                            case 3:
                                {
                                    logicalH = hRaw;
                                    logicalAway = -vRaw;
                                }
                                break;


                            case 2:
                                {
                                    logicalH = vRaw;
                                    logicalAway = hRaw;
                                }
                                break;


                            case 4:
                                {
                                    logicalH = vRaw;
                                    logicalAway = -hRaw;
                                }
                                break;


                            default:
                                {
                                    logicalH = hRaw;
                                    logicalAway = vRaw;
                                }
                                break;
                        }


                        if (logicalH != 0 && logicalAway == 0) animDriver.Play("Player_Crawl_Shoot", 0.33f);
                        else if (logicalH != 0 && logicalAway > 0) animDriver.Play("Player_Crawl_Shoot", 0.66f);
                        else if (logicalH == 0 && logicalAway > 0) animDriver.Play("Player_Crawl_Shoot", 0.99f);
                        else animDriver.Play("Player_Crawl_Shoot", 0.33f);


                        float maxZipRange = 12f;
                        RaycastHit2D crawlRayHit = Physics2D.Raycast(rb.position, surfaceAimDir, maxZipRange, jumpableGround);


                        if (crawlRayHit.collider != null && playerInput.JumpPressed)
                        {
                            Vector2 targetSurfaceAway = crawlRayHit.normal;
                            int capturedDirection = direction;

                            Vector2 playerSurfaceAway;

                            switch (capturedDirection)
                            {
                                case 1: playerSurfaceAway = Vector2.up; break;
                                case 3: playerSurfaceAway = Vector2.up; break;
                                case 2: playerSurfaceAway = Vector2.right; break;
                                case 4: playerSurfaceAway = Vector2.left; break;
                                default: playerSurfaceAway = Vector2.up; break;
                            }

                            float playerNudgeDist = (capturedDirection == 3) ? 1.0f : 0.5f;

                            moveTarget = crawlRayHit.point + targetSurfaceAway * 0.5f;
                            coll.size = new Vector2(0.7719507f, 1.863027f);
                            coll.offset = new Vector2(-0.3766563f, -0.968719f);

                            transform.rotation = Quaternion.identity;
                            direction = 1;

                            Vector2 zipNudgeDir = (moveTarget.Value - rb.position).normalized;
                            TeleportRigidbody(rb.position + zipNudgeDir * 0.15f + playerSurfaceAway * playerNudgeDist);

                            audioController.PlayRandom(sndSwing, sndSwing2, sndSwing3);

                            shoot = false;
                            zipFromCrawlGrace = 0.3f;
                            pState = PlayerState.quickzip;
                            zipTravelDist = 0f;
                        }
                    }
                    else
                    {
                        if (shoot)
                        {
                            if (playerInput.ShootReleased && animDriver.CurrentState.IsName("Player_Crawl_Shoot"))
                            {
                                float angle = Mathf.Atan2(crawlShootAimDir.y, crawlShootAimDir.x) * Mathf.Rad2Deg;
                                Quaternion rot = Quaternion.Euler(0f, 0f, angle - 90f);

                                float facingSign = sprite.flipX ? -1f : 1f;
                                AnimatorStateInfo crawlShootInfo = animDriver.CurrentState;
                                float frame = crawlShootInfo.normalizedTime % 1f;
                                Vector2 offset = Vector2.zero;

                                if (direction == 1) // floor
                                {
                                    if (frame < 0.5f) offset = new Vector2(0.189838f, -0.10983f);
                                    else if (frame < 0.83f) offset = new Vector2(0.138882f, 0.01717f);
                                    else offset = new Vector2(-0.027118f, 0.08817f);
                                }
                                else if (direction == 2) // left wall
                                {
                                    if (frame < 0.5f) offset = new Vector2(-0.10983f, 0.189838f);
                                    else if (frame < 0.83f) offset = new Vector2(0.01717f, 0.138882f);
                                    else offset = new Vector2(0.08817f, -0.027118f);
                                }
                                else if (direction == 3) // ceiling
                                {
                                    if (frame < 0.5f) offset = new Vector2(-0.189838f, 0.10983f);
                                    else if (frame < 0.83f) offset = new Vector2(-0.138882f, -0.01717f);
                                    else offset = new Vector2(0.027118f, -0.08817f);
                                }
                                else // direction 4, right wall
                                {
                                    if (frame < 0.5f) offset = new Vector2(0.10983f, -0.189838f);
                                    else if (frame < 0.83f) offset = new Vector2(-0.01717f, -0.138882f);
                                    else offset = new Vector2(-0.08817f, 0.027118f);
                                }

                                if (direction == 1 || direction == 3)
                                    offset.x *= facingSign;
                                else
                                    offset.y *= facingSign;

                                Instantiate(webPrefab, new Vector3(transform.position.x + offset.x, transform.position.y + offset.y, transform.position.z), rot);

                                audioController.Play(sndWebShoot);
                            }

                            shoot = false;
                        }
                    }
                }
                break;




            case PlayerState.quickzip:
                {
                    zipFromCrawlGrace -= Time.deltaTime;

                    // If the path to the target is blocked by a wall, stop short and crawl from there instead
                    if (moveTarget.HasValue)
                    {
                        Vector2 currentPos = rb.position;
                        Vector2 target = moveTarget.Value;

                        // Check for geometry blocking the zip path
                        Vector2 zipDir = (target - currentPos).normalized;
                        RaycastHit2D wallBlock = Physics2D.BoxCast(currentPos, new Vector2(0.15f, 0.15f), 0f, zipDir, 0.4f, jumpableGround);

                        if (zipDir.y > 0f && zipTravelDist > 0.6f)
                        {
                            RaycastHit2D ceilingEarly = Physics2D.BoxCast(currentPos, new Vector2(coll.size.x * 0.5f, 0.05f), 0f, Vector2.up, GetGroundOffsetMagnitude() + 0.3f, jumpableGround);

                            if (ceilingEarly.collider != null && Vector2.Distance(ceilingEarly.point, target) > 0.5f)
                            {
                                moveTarget = null;
                                rb.velocity = Vector2.zero;
                                coll.size = new Vector2(0.8397379f, 1.615343f);
                                coll.offset = new Vector2(-0.03511286f, -0.03012538f);
                                ropeRenderer.ReturnAllToPool();
                                transform.rotation = Quaternion.Euler(0f, 0f, 180f);
                                TeleportRigidbody(ceilingEarly.point - Vector2.up * GetGroundOffsetMagnitude());
                                direction = 3;
                                pState = PlayerState.crawl;
                                crawlEntryGrace = 0.1f;
                                uKeyReleaseRequired = true;
                                rb.gravityScale = 0f;
                                break;
                            }
                        }

                        if (wallBlock.collider == null && zipTravelDist > 0.6f)
                        {
                            // Skip a perpendicular probe if it points mostly downward, since it would false-positive on the floor the player just launched from
                            Vector2 zipPerp = new Vector2(-zipDir.y, zipDir.x);
                            bool perpIsDownward = zipPerp.y < -0.3f;
                            Vector2 zipPerpAlt = new Vector2(zipDir.y, -zipDir.x);
                            bool altIsDownward = zipPerpAlt.y < -0.3f;

                            if (!perpIsDownward)
                            {
                                RaycastHit2D sideBlockR = Physics2D.Raycast(currentPos, zipPerp, 0.35f, jumpableGround);

                                if (sideBlockR.collider != null && Vector2.Distance(sideBlockR.point, target) > 0.25f)
                                    wallBlock = sideBlockR;
                            }

                            if (wallBlock.collider == null && !altIsDownward)
                            {
                                RaycastHit2D sideBlockL = Physics2D.Raycast(currentPos, -zipPerp, 0.35f, jumpableGround);

                                if (sideBlockL.collider != null && Vector2.Distance(sideBlockL.point, target) > 0.25f)
                                    wallBlock = sideBlockL;
                            }
                        }

                        if (wallBlock.collider != null && Vector2.Distance(wallBlock.point, target) > 0.25f)
                        {
                            moveTarget = null;
                            rb.velocity = Vector2.zero;

                            coll.size = new Vector2(0.8397379f, 1.615343f);
                            coll.offset = new Vector2(-0.03511286f, -0.03012538f);
                            ropeRenderer.ReturnAllToPool();

                            float surfaceAngle = Mathf.Atan2(wallBlock.normal.y, wallBlock.normal.x) * Mathf.Rad2Deg;
                            transform.rotation = Quaternion.Euler(0f, 0f, surfaceAngle - 90f);

                            direction = GetDirectionFromRotation();
                            SnapRotationToDirection();

                            TeleportRigidbody(wallBlock.point + wallBlock.normal * GetGroundOffsetMagnitude());

                            pState = PlayerState.crawl;
                            crawlEntryGrace = 0.1f;
                            uKeyReleaseRequired = true;
                            rb.gravityScale = 0f;
                            break;
                        }
                    }

                    swingEnd = false;
                    bool freezeRotation = false;

                    if (moveTarget.HasValue)
                    {
                        Vector2 currentPos = rb.position;
                        Vector2 target = moveTarget.Value;
                        Vector2 zipDir = (target - currentPos).normalized;

                        sprite.flipX = target.x <= currentPos.x;

                        if (zipDir != Vector2.zero)
                        {
                            float angle = Mathf.Atan2(zipDir.y, zipDir.x) * Mathf.Rad2Deg;
                            if (!freezeRotation) transform.rotation = Quaternion.Euler(0f, 0f, angle - 90);
                        }

                        float distToTarget = Vector2.Distance(currentPos, target);
                        float zipSpeed = Mathf.Lerp(1.5f, 4f, Mathf.Clamp01(distToTarget / 0.8f));
                        rb.velocity = zipDir * zipSpeed;

                        float stopDist = GetGroundOffsetMagnitude() + 0.1f;

                        Vector2 newPos = Vector2.MoveTowards(currentPos, target, zipSpeed * Time.deltaTime);
                        rb.position = newPos;
                        rb.velocity = Vector2.zero;
                        zipTravelDist += zipSpeed * Time.deltaTime;

                        // If the player lands on top of geometry mid zip (like an intermediate ledge), treat it like an early arrival and orient to that surface instead
                        if (zipDir.y > 0f && zipTravelDist > 0.6f)
                        {
                            RaycastHit2D groundedMidZip = Physics2D.BoxCast(rb.position, new Vector2(coll.size.x * 0.35f, 0.05f), 0f, Vector2.down, GetGroundOffsetMagnitude() + 0.15f, jumpableGround);

                            if (groundedMidZip.collider != null && Vector2.Distance(groundedMidZip.point, target) > 0.5f)
                            {
                                moveTarget = null;
                                rb.velocity = Vector2.zero;

                                coll.size = new Vector2(0.8397379f, 1.615343f);
                                coll.offset = new Vector2(-0.03511286f, -0.03012538f);
                                ropeRenderer.ReturnAllToPool();

                                float surfaceAngle = Mathf.Atan2(groundedMidZip.normal.y, groundedMidZip.normal.x) * Mathf.Rad2Deg;
                                transform.rotation = Quaternion.Euler(0f, 0f, surfaceAngle - 90f);

                                // Ceiling undersides have a downward facing normal, so flip it to snap toward the surface
                                Vector2 snapNormal = groundedMidZip.normal.y < 0f ? -groundedMidZip.normal : groundedMidZip.normal;
                                TeleportRigidbody(groundedMidZip.point + snapNormal * GetGroundOffsetMagnitude());

                                direction = GetDirectionFromRotation();
                                SnapRotationToDirection();
                                pState = PlayerState.crawl;
                                crawlEntryGrace = 0.1f;
                                uKeyReleaseRequired = true;
                                rb.gravityScale = 0f;
                                break;
                            }

                            // Catches ceiling undersides and overhangs the player presses into from below mid zip
                            RaycastHit2D ceilingMidZip = Physics2D.BoxCast(rb.position, new Vector2(coll.size.x * 0.5f, 0.05f), 0f, Vector2.up, GetGroundOffsetMagnitude() + 0.2f, jumpableGround);

                            if (ceilingMidZip.collider != null && Vector2.Distance(ceilingMidZip.point, target) > 0.5f)
                            {
                                moveTarget = null;
                                rb.velocity = Vector2.zero;

                                coll.size = new Vector2(0.8397379f, 1.615343f);
                                coll.offset = new Vector2(-0.03511286f, -0.03012538f);
                                ropeRenderer.ReturnAllToPool();

                                // Rotate to ceiling crawl orientation and snap up toward the surface
                                transform.rotation = Quaternion.Euler(0f, 0f, 180f);
                                TeleportRigidbody(ceilingMidZip.point - Vector2.up * GetGroundOffsetMagnitude());
                                direction = 3;
                                pState = PlayerState.crawl;
                                crawlEntryGrace = 0.1f;
                                uKeyReleaseRequired = true;
                                rb.gravityScale = 0f;
                                break;
                            }
                        }

                        if (distToTarget <= stopDist)
                        {
                            moveTarget = null;

                            coll.size = new Vector2(0.8397379f, 1.615343f);
                            coll.offset = new Vector2(-0.03511286f, -0.03012538f);
                            ropeRenderer.ReturnAllToPool();

                            RaycastHit2D hit = Physics2D.BoxCast(currentPos, new Vector2(0.15f, 0.15f), 0f, zipDir, stopDist + 1f, jumpableGround);

                            if (hit.collider != null)
                            {
                                float surfaceAngle = Mathf.Atan2(hit.normal.y, hit.normal.x) * Mathf.Rad2Deg;
                                transform.rotation = Quaternion.Euler(0f, 0f, surfaceAngle - 90f);

                                TeleportRigidbody(hit.point + hit.normal * GetGroundOffsetMagnitude());

                                direction = GetDirectionFromRotation();
                                SnapRotationToDirection();
                                pState = PlayerState.crawl;
                                crawlEntryGrace = 0.1f;
                                uKeyReleaseRequired = true;
                                rb.gravityScale = 0f;
                            }
                            else
                            {
                                RaycastHit2D fallbackDown = Physics2D.Raycast(currentPos, Vector2.down, 20f, jumpableGround);

                                if (fallbackDown.collider != null)
                                {
                                    moveTarget = fallbackDown.point + fallbackDown.normal * GetGroundOffsetMagnitude();
                                }
                                else
                                {
                                    transform.rotation = Quaternion.identity;
                                    uKeyReleaseRequired = true;
                                    pState = PlayerState.normal;
                                    rb.gravityScale = 1f;
                                }
                            }
                        }

                        float dirOff = sprite.flipX ? -1f : 1f;

                        if (zipFromCrawlGrace <= 0f)
                        {
                            // Back off along the travel direction so the cast starts outside the geometry, this avoids sampling a normal from a point that is already embedded in a collider
                            float probeBackoff = 0.6f;
                            Vector2 castOrigin = rb.position - zipDir * probeBackoff;
                            RaycastHit2D surfHit = Physics2D.Raycast(castOrigin, zipDir, probeBackoff + 0.25f, jumpableGround);

                            if (surfHit.collider == null)
                            {
                                // Fallback for the rare case where even the backed off origin is embedded, like a thick wall
                                Collider2D[] nearby = Physics2D.OverlapCircleAll(rb.position, 0.4f, jumpableGround);

                                foreach (var c in nearby)
                                {
                                    Vector2 approxNormal = -zipDir;
                                    surfHit = Physics2D.Raycast(rb.position + approxNormal * 0.5f, -approxNormal, 0.6f, jumpableGround);
                                    if (surfHit.collider != null) break;
                                }
                            }

                            if (surfHit.collider != null)
                            {
                                hasTurnInner = false;
                                hasTurnOuter = false;
                                freezeRotation = true;
                                float surfaceAngle = Mathf.Atan2(surfHit.normal.y, surfHit.normal.x) * Mathf.Rad2Deg;
                                transform.rotation = Quaternion.Euler(0f, 0f, surfaceAngle - 90f);
                                moveTarget = null;
                                coll.size = new Vector2(0.8397379f, 1.615343f);
                                coll.offset = new Vector2(-0.03511286f, -0.03012538f);
                                coll.enabled = true;
                                collCircle.enabled = false;
                                ropeRenderer.ReturnAllToPool();
                                rb.gravityScale = 0;

                                direction = GetDirectionFromRotation();
                                SnapRotationToDirection();
                                pState = PlayerState.crawl;
                                crawlEntryGrace = 0.1f;
                                uKeyReleaseRequired = true;
                            }
                        }
                    }
                }
                break;




            case PlayerState.dashenemy:
                {
                    // If we lost the target mid attack, stop attacking
                    if (attacking && combatTargeting.CurrentTarget == null)
                        attacking = false;

                    if (waitingToHit && combatTargeting.CurrentTarget == null && !countering)
                    {
                        waitingToHit = false;
                        animDriver.Speed = 1f;
                        rb.gravityScale = 1f;
                    }

                    if (waitingToHit && countering && combatTargeting.CurrentCounterTarget == null)
                    {
                        waitingToHit = false;
                        animDriver.Speed = 1f;
                        rb.gravityScale = 1f;
                    }

                    if (attacking)
                    {
                        if (combatTargeting.CurrentTarget != null)
                            Attacking(combatTargeting.CurrentTarget.gameObject);

                        if (pastHitEvent)
                        {
                            // Re-evaluate targets after landing a hit
                            bool facingLeft = sprite.flipX;

                            if (playerInput.Horizontal > 0) facingLeft = false;
                            else if (playerInput.Horizontal < 0) facingLeft = true;

                            Collider2D[] ehits2 = Physics2D.OverlapCircleAll(origin, 5.2f, enemyMask);
                            float closestEDistance2 = Mathf.Infinity;
                            RobotStep closestEnemy2 = null;

                            foreach (var ehit in ehits2)
                            {
                                RobotStep enemy = ehit.GetComponent<RobotStep>();
                                if (enemy == null || enemy.eState == RobotStep.EnemyState.death || !enemy.IsTargetable) continue;

                                RaycastHit2D hit2 = Physics2D.Linecast(transform.position, enemy.transform.position, jumpableGround);
                                if (hit2.collider != null && (Vector2)hit2.point != (Vector2)enemy.transform.position) continue;

                                bool noLightning2 = true;

                                foreach (var hl in Physics2D.LinecastAll(transform.position, enemy.transform.position))
                                {
                                    LightningScript ls = hl.collider?.GetComponent<LightningScript>();

                                    if (ls != null && ls.phase == 0)
                                    {
                                        noLightning2 = false;
                                        break;
                                    }
                                }

                                if (!noLightning2) continue;


                                float dx2 = enemy.transform.position.x - origin.x;
                                if ((facingLeft && dx2 > 0) || (!facingLeft && dx2 < 0)) continue;


                                float dist2 = Mathf.Abs(dx2);

                                if (dist2 < closestEDistance2)
                                {
                                    closestEDistance2 = dist2;
                                    closestEnemy2 = enemy;
                                }
                            }

                            currentTarget = closestEnemy2;
                            combatTargeting.CurrentTarget = combatTargeting.ResolveTarget(origin, facingLeft, transform, jumpableGround, closestEnemy2, closestEDistance2);

                            // Re-evaluate counter
                            float ceDist2 = Mathf.Infinity;
                            Component ctr2 = null;

                            foreach (var ehitC in ehitsC)
                            {
                                RobotStep eC = ehitC.GetComponent<RobotStep>();

                                if (eC == null || eC.eState == RobotStep.EnemyState.death || eC.eState != RobotStep.EnemyState.attack)
                                    continue;

                                RaycastHit2D hc = Physics2D.Linecast(transform.position, eC.transform.position, jumpableGround);

                                if (hc.collider != null && (Vector2)hc.point != (Vector2)eC.transform.position)
                                    continue;

                                float dc = Mathf.Abs(eC.transform.position.x - origin.x);

                                if (dc < ceDist2)
                                {
                                    ceDist2 = dc;
                                    ctr2 = eC;
                                }
                            }

                            if (combatTargeting.Boss != null && combatTargeting.Boss.gState == GoblinStep.GoblinState.attack)
                            {
                                float bossDist2 = Mathf.Abs(combatTargeting.Boss.transform.position.x - origin.x);
                                RaycastHit2D hitB2 = Physics2D.Linecast(transform.position, combatTargeting.Boss.transform.position, jumpableGround);
                                bool bossVisible2 = hitB2.collider == null || (Vector2)hitB2.point == (Vector2)combatTargeting.Boss.transform.position;

                                if (bossVisible2 && (ctr2 == null || bossDist2 < ceDist2))
                                    ctr2 = combatTargeting.Boss;
                            }

                            currentCounter = ctr2 as RobotStep;
                            combatTargeting.CurrentCounterTarget = ctr2;

                            // Next attack input
                            if (playerInput.AttackHeld && combatTargeting.CurrentTarget != null && attackCooldown <= 0f && !oKeyReleaseRequired)
                            {
                                dash_spd = CalcDashSpeed(combatTargeting.TargetTransform);
                                attacking = true;
                                pState = PlayerState.dashenemy;
                                animDriver.Speed = 2f;
                                PlayAttackAnimation(PickAttackAnimation());
                                rb.gravityScale = 0;
                                pastHitEvent = false;
                                oKeyReleaseRequired = true;
                            }
                            else if (playerInput.AttackHeld && combatTargeting.CurrentTarget == null && attackCooldown <= 0f && !oKeyReleaseRequired)
                            {
                                dash_spd = 0f;
                                attacking = false;
                                pState = PlayerState.dashenemy;
                                animDriver.Speed = 1.5f;
                                PlayAttackAnimation(PickAttackAnimation());
                                pastHitEvent = false;
                                oKeyReleaseRequired = true;
                            }

                            if (playerInput.UppercutHeld && Grounded() && attackCooldown <= 0f)
                            {
                                bool targetClose = combatTargeting.CurrentTarget != null && Mathf.Abs(combatTargeting.TargetTransform.position.x - origin.x) <= 1f;
                                dash_spd = targetClose ? CalcDashSpeed(combatTargeting.TargetTransform) : 0f;
                                attacking = targetClose;
                                uppercut = targetClose;
                                pState = PlayerState.dashenemy;
                                animDriver.Speed = 2f;
                                PlayAttackAnimation(MovementState.uppercut);
                                rb.gravityScale = targetClose ? 0 : 1;
                                SetAttackCooldown();
                                pastHitEvent = false;
                            }

                            if (playerInput.CounterHeld && Grounded())
                            {
                                if (combatTargeting.CurrentCounterTarget != null)
                                {
                                    dash_spd = CalcDashSpeed(combatTargeting.CounterTargetTransform, isCounter: true);
                                    countering = true;
                                    combatTargeting.CounterTargetAnim.speed = 0f;
                                    pState = PlayerState.dashenemy;
                                    sprite.flipX = combatTargeting.CounterTargetTransform.position.x < transform.position.x;
                                    animDriver.Speed = 2f;
                                    animDriver.SetMovementState((int)PickCounterAnimation());
                                    rb.gravityScale = 0;
                                    pastHitEvent = false;
                                }
                                else
                                {
                                    dash_spd = 0f;
                                    countering = false;
                                    pState = PlayerState.dashenemy;
                                    animDriver.Speed = 1.5f;
                                    animDriver.SetMovementState((int)PickCounterAnimation());
                                    pastHitEvent = false;
                                }
                            }
                        }
                    }
                    else if (countering)
                    {
                        Transform counterTf = combatTargeting.CounterTargetTransform;
                        Animator counterAnim = combatTargeting.CounterTargetAnim;
                        Rigidbody2D counterRB = combatTargeting.CounterTargetRB;

                        if (counterTf == null || counterAnim == null || counterRB == null)
                        {
                            // Target vanished mid-counter, exit cleanly
                            pastHitEvent = false;
                            pState = PlayerState.normal;
                            postAttackBuffer = 0.1f;
                            postAttackWasGrounded = Grounded();
                            countering = false;
                            uppercut = false;
                            waitingToHit = false;
                            rb.gravityScale = 1;
                            break;
                        }

                        counterRB.velocity = new Vector2(0f, counterRB.velocity.y);
                        rb.velocity = Vector2.zero;

                        float counterDist = Mathf.Abs(counterTf.position.x - transform.position.x);

                        if (counterDist >= 0.45f)
                        {
                            attackTimeoutTimer += Time.deltaTime;

                            if (attackTimeoutTimer > 1.2f)
                            {
                                pastHitEvent = false;
                                pState = PlayerState.normal;
                                postAttackBuffer = 0.1f;
                                postAttackWasGrounded = Grounded();
                                countering = false;
                                uppercut = false;
                                waitingToHit = false;
                                counterAnim.speed = 1f;
                                counterRB.gravityScale = 1;
                                rb.gravityScale = 1;
                                attackTimeoutTimer = 0f;
                                break;
                            }
                        }
                        else
                        {
                            attackTimeoutTimer = 0f;
                        }


                        int r = UnityEngine.Random.Range(0, 3);
                        int counterCooldown = r == 0 ? 300 : r == 1 ? 400 : 500;


                        switch (combatTargeting.CurrentCounterTarget)
                        {
                            case RobotStep counterRobot:
                                {
                                    counterRobot.alarm4 = counterCooldown;
                                    counterRobot.kick = false;
                                }
                                break;


                            case GoblinStep counterGoblin:
                                {
                                    counterGoblin.alarm4 = counterCooldown;
                                }
                                break;
                        }


                        counterRB.gravityScale = 1;


                        if (Mathf.Abs(counterTf.position.x - transform.position.x) >= 0.45f && !waitingToHit && IsCounterMoveWindow(stateInfo))
                        {
                            float step = dash_spd * Time.deltaTime;
                            transform.position = Vector2.MoveTowards(transform.position, counterTf.position, step);
                        }


                        if (waitingToHit)
                        {
                            float dist = Mathf.Abs(counterTf.position.x - transform.position.x);

                            if (dist < 0.45f)
                            {
                                animDriver.Speed = 1;
                                waitingToHit = false;
                            }
                            else
                            {
                                float step = dash_spd * Time.deltaTime;
                                transform.position = Vector2.MoveTowards(transform.position, counterTf.position, step);
                            }
                        }


                        if (stateInfo.normalizedTime >= 1f)
                        {
                            pastHitEvent = false;
                            pState = PlayerState.normal;
                            postAttackBuffer = 0.1f;
                            postAttackWasGrounded = Grounded();
                            countering = false;
                            uppercut = false;
                            counterRB.gravityScale = 1;
                            if (combatTargeting.CurrentCounterTarget is RobotStep rHsp) rHsp.hsp = 1f;
                            rb.gravityScale = 1;
                        }
                    }
                    else
                    {
                        rb.gravityScale = 1f;
                        if (Grounded()) rb.velocity = Vector2.zero;


                        bool targetNowAvailable = combatTargeting.CurrentTarget != null || combatTargeting.CurrentCounterTarget != null;


                        if (stateInfo.normalizedTime >= 1f || (targetNowAvailable && stateInfo.normalizedTime >= 0.2f))
                        {
                            pastHitEvent = false;
                            pState = PlayerState.normal;
                            postAttackBuffer = 0.1f;
                            postAttackWasGrounded = Grounded();
                            countering = false;
                            animDriver.Speed = 1;
                            uppercut = false;
                        }
                    }
                }
                break;




            case PlayerState.hurt:
                {
                    visual.rotation = Quaternion.Euler(0f, 0f, 0f);
                    transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                    uppercut = false;
                    rb.gravityScale = 1;
                    attacking = false;
                    countering = false;


                    if (dirX > 0)
                        sprite.flipX = true;
                    else
                        sprite.flipX = false;


                    if (stateInfo.IsName("Player_Launched"))
                    {
                        launchTechTimer += Time.deltaTime;

                        if (stateInfo.normalizedTime >= 1f)
                            animDriver.Speed = 0f;

                        bool techWindowOpen = launchTechTimer >= launchTechWindow;
                        bool techInput = techWindowOpen && playerInput.JumpPressed;

                        if (techInput)
                        {
                            rb.velocity = new Vector2(rb.velocity.x, jspd);
                            audioController.PlayRandomWithSilenceChance(sndJump, sndJump2);

                            animDriver.Speed = 1f;
                            pState = PlayerState.normal;
                        }
                        else if (Grounded() && launchGroundGrace <= 0f)
                        {
                            animDriver.Speed = 1f;
                            pState = PlayerState.normal;
                        }
                    }
                    else
                    {
                        animDriver.Speed = 1f;

                        if ((stateInfo.IsName("Player_Hurt1") || stateInfo.IsName("Player_Hurt2")) && stateInfo.normalizedTime >= 1f)
                        {
                            dirX = 0;
                            pState = PlayerState.normal;
                        }
                    }
                }
                break;
        }


        if (pState == PlayerState.swing)
            ropeRenderer.Draw(ropePhysics.GrapplePoint, rb.position);

        if (pState == PlayerState.quickzip && moveTarget.HasValue)
            ropeRenderer.Draw(moveTarget.Value, new Vector2(transform.position.x, transform.position.y));

        UpdateAnimationState();
    }




    /// <summary>
    /// Scans the arc in the direction of the rope's swing to detect enemy components within a specified radius.
    /// </summary>
    /// <returns>A list of enemy components detected along the swing arc. The list contains each enemy only once, even if
    /// detected at multiple sample points. The list is empty if no enemies are found.</returns>
    private List<Component> ScanSwingArc()
    {
        int steps = 6;
        float scanDeg = 15f * Mathf.Sign(ropePhysics.AngularVelocity); // scan in the direction of travel
        float scanRadius = 0.55f;

        var seen = new HashSet<Component>();
        var results = new List<Component>();

        for (int i = 1; i <= steps; i++)
        {
            float futureAngleDeg = ropePhysics.Angle + scanDeg * (i / (float)steps);
            Vector2 samplePos = new Vector2(ropePhysics.GrapplePoint.x + Mathf.Cos(futureAngleDeg * Mathf.Deg2Rad) * ropePhysics.Length, ropePhysics.GrapplePoint.y + Mathf.Sin(futureAngleDeg * Mathf.Deg2Rad) * ropePhysics.Length);

            Collider2D[] hits = Physics2D.OverlapCircleAll(samplePos, scanRadius, enemyMask);

            foreach (var hit in hits)
            {
                Component enemy = (Component)hit.GetComponent<RobotStep>() ?? (Component)hit.GetComponent<GoblinStep>() ?? (Component)hit.GetComponent<ShockerStep>();
                if (enemy == null) continue;
                if (enemy is RobotStep r && r.eState == RobotStep.EnemyState.death) continue;
                if (enemy is GoblinStep g && g.gState == GoblinStep.GoblinState.death) continue;
                if (enemy is ShockerStep s && s.sState == ShockerStep.ShockerState.death) continue;

                // Add each enemy once even if it shows up across multiple sample steps
                if (seen.Add(enemy))
                    results.Add(enemy);
            }
        }

        return results;
    }


    

    /// <summary>
    /// Processes all valid swing kick targets, applying hit effects and launching each enemy within range.
    /// </summary>
    public void SwingKickHitEvent()
    {
        if (swingKickTargets == null || swingKickTargets.Count == 0) return;

        float hitRange = 1.2f;

        // Borrow the uppercut flag so every enemy hit gets launched, then restore it after
        bool savedUppercut = uppercut;
        uppercut = true;

        foreach (Component target in swingKickTargets)
        {
            if (target == null) continue;

            MonoBehaviour targetMB = (MonoBehaviour)target;

            // Skip if the target moved out of range or died since the arc was scanned
            if (Vector2.Distance(transform.position, targetMB.transform.position) > hitRange) continue;

            if (target is RobotStep rd && rd.eState == RobotStep.EnemyState.death) continue;
            if (target is GoblinStep gd && gd.gState == GoblinStep.GoblinState.death) continue;
            if (target is ShockerStep sd && sd.sState == ShockerStep.ShockerState.death) continue;

            if (target is RobotStep robot)
            {
                if (robot.swingKickHitCooldown <= 0f)
                {
                    robot.swingKickHitCooldown = 0.5f;
                }
                else
                {
                    continue;
                }
            }

            if (target is GoblinStep goblin)
            {
                if (goblin.swingKickHitCooldown <= 0f)
                {
                    goblin.swingKickHitCooldown = 0.5f;
                }
                else
                {
                    continue;
                }
            }

            if (target is ShockerStep shocker)
            {
                if (shocker.swingKickHitCooldown <= 0f)
                {
                    shocker.swingKickHitCooldown = 0.5f;
                }
                else
                {
                    continue;
                }
            }

            bool hitLanded = true;

            switch (target)
            {
                case RobotStep rb2: OnHit.Invoke(rb2); break;
                case GoblinStep g: hitLanded = g.OnPlayerHit(g); break;
                case ShockerStep s: OnHitS.Invoke(s); break;
            }

            if (!hitLanded) continue;

            combo++;
            alarm3 = 300;
            SpawnHitEffect(targetMB.transform.position);
        }

        uppercut = savedUppercut;
    }




    /// <summary>
    /// Initiates an attack action directed toward the specified target, configuring attack state and animation based on
    /// the provided parameters.
    /// </summary>
    /// <param name="targetTransform">The transform of the target to attack. If null, the attack is performed without a specific target, which may
    /// result in an air attack.</param>
    /// <param name="attacking">A value indicating whether the attack should be executed as an active attack. Set to <see langword="true"/> to
    /// perform an attack; otherwise, <see langword="false"/>.</param>
    private void StartAttackTowardTarget(Transform targetTransform, bool attacking)
    {
        bool isAirWhiff = targetTransform == null && !Grounded();

        if (!isAirWhiff)
            rb.velocity = Vector2.zero;

        if (targetTransform != null)
            dash_spd = CalcDashSpeed(targetTransform);
        else
            dash_spd = 0f;

        this.attacking = attacking;
        SetAttackCooldown();
        oKeyReleaseRequired = true;

        if (pState != PlayerState.dashenemy)
        {
            PlayAttackSounds();
            pState = PlayerState.dashenemy;
        }

        animDriver.Speed = attacking ? 2f : 1.5f;
        PlayAttackAnimation(PickAttackAnimation());
        rb.gravityScale = attacking ? 0 : 1;
    }




    /// <summary>
    /// Releases the specified target if it is currently selected as the active target.
    /// </summary>
    /// <param name="target">The component to release if it is the current target. This parameter can be null.</param>
    public void ReleaseTargetIfCurrent(Component target)
    {
        combatTargeting.ReleaseIfCurrent(target);

        if (currentTarget == (target as RobotStep))
            currentTarget = null;
    }




    /// <summary>
    /// Determines whether the specified animation state is within the counter move window for blocking actions.
    /// </summary>
    /// <param name="si">An AnimatorStateInfo structure representing the current animation state to evaluate.</param>
    /// <returns>true if the animation state is one of the defined blocking states and its normalized time is within the allowed
    /// counter window; otherwise, false.</returns>
    private bool IsCounterMoveWindow(AnimatorStateInfo si)
    {
        return (si.IsName("Player_Block1") && si.normalizedTime <= 0.28f)
            || (si.IsName("Player_Block2") && si.normalizedTime <= 0.30f)
            || (si.IsName("Player_Block3") && si.normalizedTime <= 0.32f)
            || (si.IsName("Player_Block4") && si.normalizedTime <= 0.38f);
    }




    /// <summary>
    /// Determines whether the specified animation state is within the active window for attack moves.
    /// </summary>
    /// <param name="si">The animation state information to evaluate. Typically represents the current state of the player's animation.</param>
    /// <returns>true if the animation state corresponds to a recognized attack move and is within its active window; otherwise,
    /// false.</returns>
    private bool IsAttackMoveWindow(AnimatorStateInfo si)
    {
        return (si.IsName("Player_Air_Kick") && si.normalizedTime <= 0.86f)
            || (si.IsName("Player_Air_Punch") && si.normalizedTime <= 0.67f)
            || (si.IsName("Player_Kick1") && si.normalizedTime <= 0.65f)
            || (si.IsName("Player_Kick2") && si.normalizedTime <= 0.46f)
            || (si.IsName("Player_Punch1") && si.normalizedTime <= 0.52f)
            || (si.IsName("Player_Punch2") && si.normalizedTime <= 0.48f)
            || (si.IsName("Player_Punch3") && si.normalizedTime <= 0.25f)
            || (si.IsName("Player_Punch4") && si.normalizedTime <= 0.45f)
            || (si.IsName("Player_Uppercut") && si.normalizedTime <= 0.33f);
    }




    /// <summary>
    /// Determines whether the player is currently able to break objects based on the specified animation state.
    /// </summary>
    /// <param name="si">An AnimatorStateInfo structure representing the current animation state of the player.</param>
    /// <returns>true if the player can break objects in the given animation state; otherwise, false.</returns>
    private bool CanBreakObjects(AnimatorStateInfo si)
    {
        if (pState == PlayerState.dashenemy && IsAttackMoveWindow(si)) return true;
        if (pState == PlayerState.crawl && crawlKickTriggered && si.IsName("Player_Crawl_Kick")) return true;
        return false;
    }




    /// <summary>
    /// Updates the player's animation state based on the current movement, action, and status conditions.
    /// </summary>
    private void UpdateAnimationState()
    {
        if (postAttackBuffer > 0f) postAttackBuffer -= Time.deltaTime;

        if (crawlKickTriggered) return;

        if (pState == PlayerState.hurt) return;

        if (pState == PlayerState.dashenemy) return;

        if (pState != PlayerState.crawl)
        {
            if (dirX > 0f) sprite.flipX = false;
            else if (dirX < 0f) sprite.flipX = true;
        }
        else
        {
            if (crawlDir > 0f) sprite.flipX = false;
            else if (crawlDir < 0f) sprite.flipX = true;
        }

        if (swingEnd) return;

        // Don't override the swing kick animation while it's playing
        if (swingKickTriggered && animDriver.CurrentState.IsName("Player_Swing_Kick")) return;

        // Don't override the crawl kick animation while it's playing
        if (crawlKickTriggered && animDriver.CurrentState.IsName("Player_Crawl_Kick")) return;

        MovementState mstate = MovementState.idle;

        if (pState == PlayerState.normal)
        {
            if (shoot)
            {
                animDriver.Speed = 0f;
                mstate = Grounded() ? MovementState.groundshoot : MovementState.airshoot;
            }
            else
            {
                animDriver.Speed = 1f;

                bool pushingIntoBarrier = (barrierContactDir == 1 && dirX > 0) || (barrierContactDir == -1 && dirX < 0);

                if (Grounded())
                {
                    mstate = (dirX != 0f && !pushingIntoBarrier) ? MovementState.running : MovementState.idle;
                }
                else if (postAttackBuffer > 0f && postAttackWasGrounded)
                {
                    mstate = (dirX != 0f && !pushingIntoBarrier) ? MovementState.running : MovementState.idle;
                }
                else
                {
                    mstate = (rb.velocity.y >= 0f) ? MovementState.jumping : MovementState.falling;
                }
            }
        }
        else if (pState == PlayerState.swing)
        {
            if (swingKickTriggered)
            {
                mstate = MovementState.swingkick;
                animDriver.Speed = 1f;
            }
            else
            {
                mstate = MovementState.swinging;

                float speed = Mathf.Abs(ropePhysics.AngularVelocity);
                float t = Mathf.Clamp01(speed / swingAnimFullSpeedThreshold);
                animDriver.Speed = Mathf.Lerp(swingAnimMinSpeed, 1f, t);
            }
        }
        else if (pState == PlayerState.crawl)
        {
            if (shoot)
            {
                animDriver.Speed = 0f;
                mstate = MovementState.crawlshoot;
            }
            else
            {
                mstate = MovementState.crawling;
                bool crawlPushingIntoBarrier = (barrierContactDir == 1 && crawlDir > 0) || (barrierContactDir == -1 && crawlDir < 0);
                animDriver.Speed = (Mathf.Abs(crawlDir) > 0 && !crawlPushingIntoBarrier) ? 1f : 0f;
            }
        }
        else if (pState == PlayerState.quickzip)
        {
            mstate = MovementState.zip;
        }
        else if (pState == PlayerState.death)
        {
            animDriver.Speed = 1f;
            mstate = MovementState.death;
        }

        AnimatorStateInfo si = animDriver.CurrentState;
        float nt = si.normalizedTime % 1f;

        if (mstate == MovementState.running)
        {
            if (nt >= 0.35f && nt <= 0.38f) audioController.Play(sndStep2);
            if (nt >= 0.83f && nt <= 0.86f) audioController.Play(sndStep);
        }

        if (pState == PlayerState.crawl && Mathf.Abs(crawlDir) > 0)
        {
            if (nt >= 0.41f && nt <= 0.44f) audioController.Play(sndCrawlStep);
            if (nt >= 0.82f && nt <= 0.85f) audioController.Play(sndCrawlStep2);
        }

        if (mstate == MovementState.death)
        {
            if (nt >= 0.44f && nt <= 0.46f)
            {
                if (Grounded())
                    audioController.Play(sndHardLand);

                if (!startAlarm2)
                {
                    alarm2 = 240;
                    startAlarm2 = true;
                }
            }

            if (nt >= 1f) animDriver.Speed = 0f;
        }

        animDriver.SetMovementState((int)mstate);
    }




    /// <summary>
    /// Determines whether the character is currently standing on a surface considered ground.
    /// </summary>
    /// <returns>true if the character is on a valid ground surface; otherwise, false.</returns>
    public bool Grounded()
    {
        return CharacterPhysics2D.IsGrounded(coll, jumpableGround);
    }
    



    /// <summary>
    /// Determines whether the player is currently in a state that allows physical pass-through by other objects or
    /// entities.
    /// </summary>
    /// <returns>true if the player is in a state that permits physical passability; otherwise, false.</returns>
    public bool IsPhysicallyPassable()
    {
        if (pState == PlayerState.dashenemy && (attacking || countering)) return true;
        if (pState == PlayerState.hurt) return true;
        if (pState == PlayerState.death) return true;
        return false;
    }




    /// <summary>
    /// Calculates the vertical distance from the object's local origin to the bottom of its collider, accounting for
    /// the object's local scale.
    /// </summary>
    /// <returns>The vertical offset, in local units, from the object's origin to the lowest point of its collider. The value is
    /// always non-negative.</returns>
    private float GetGroundOffsetMagnitude()
    {
        float colliderHalfHeight = coll.size.y * 0.5f;
        float colliderBottomLocalY = coll.offset.y - colliderHalfHeight;
        return Mathf.Abs(colliderBottomLocalY) * transform.localScale.y;
    }




    /// <summary>
    /// Moves the attached Rigidbody2D to the specified position instantly, bypassing interpolation.
    /// </summary>
    /// <param name="newPos">The target position to which the Rigidbody2D will be moved, in world units.</param>
    private void TeleportRigidbody(Vector2 newPos)
    {
        rb.interpolation = RigidbodyInterpolation2D.None;
        rb.position = newPos;
        StartCoroutine(RestoreInterpolationNextFixedUpdate());
    }




    /// <summary>
    /// Initiates a forced quick zip movement toward the specified target position, resetting the player's rotation and
    /// state.
    /// </summary>
    /// <param name="target">The target position, in world coordinates, to which the player will zip.</param>
    public void BeginForcedQuickZip(Vector2 target)
    {
        transform.rotation = Quaternion.identity;
        moveTarget = target;
        zipTravelDist = 0f;
        Vector2 zipNudgeDir = (target - (Vector2)transform.position).normalized;
        TeleportRigidbody((Vector2)transform.position + zipNudgeDir * 0.3f);
        rb.gravityScale = 0;
        pState = PlayerState.quickzip;
    }
    



    /// <summary>
    /// Waits until the next physics update and then restores Rigidbody2D interpolation to Interpolate mode.
    /// </summary>
    /// <returns>An enumerator that waits for the next fixed update before restoring interpolation.</returns>
    private IEnumerator RestoreInterpolationNextFixedUpdate()
    {
        yield return new WaitForFixedUpdate();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }
    



    /// <summary>
    /// Exits the current swing state, resetting relevant visual and gameplay elements to their default values.
    /// </summary>
    /// <param name="resetTransformRotation">true to reset the object's transform rotation to its default orientation; otherwise, false. Defaults to true.</param>
    public void ExitSwing(bool resetTransformRotation = true)
    {
        visual.rotation = Quaternion.Euler(0f, 0f, 0f);

        if (resetTransformRotation)
            transform.rotation = Quaternion.identity;

        swingPointSelected = false;
        ropeRenderer.ReturnAllToPool();
        swingKickTriggered = false;
        swingKickTargets.Clear();
        swingKickCooldown = 0f;
        animDriver.Speed = 1f;
        coll.size = new Vector2(0.8397379f, 1.615343f);
        coll.offset = new Vector2(-0.03511286f, -0.03012538f);
    }




    /// <summary>
    /// Animates the rotation of the object around a corner by a specified angle and updates its direction, optionally
    /// adjusting its position.
    /// </summary>
    /// <param name="positionDelta">The positional offset to apply to the object's transform before rotation. If set to Vector3.zero, no position
    /// adjustment is made.</param>
    /// <param name="rotationDelta">The angle, in degrees, by which to rotate the object around the Z axis.</param>
    /// <param name="newDirection">The new direction value to assign to the object after the rotation completes.</param>
    /// <returns>An enumerator that performs the rotation animation over time. Intended to be used with a coroutine.</returns>
    private IEnumerator RotateAroundCornerInner(Vector3 positionDelta, float rotationDelta, int newDirection)
    {
        isTurningInner = true;
        hasTurnInner = true;

        if (positionDelta != Vector3.zero)
            transform.position += positionDelta;

        float startAngle = transform.eulerAngles.z;
        float targetAngle = startAngle + rotationDelta;
        float duration = 0.15f;
        float time = 0f;

        while (time < duration)
        {
            float t = time / duration;
            transform.eulerAngles = new Vector3(0, 0, Mathf.LerpAngle(startAngle, targetAngle, t));
            time += Time.deltaTime;
            yield return null;
        }

        transform.eulerAngles = new Vector3(0, 0, targetAngle);
        direction = newDirection;

        rb.position += (Vector2)transform.up * 0.15f;
        float fixedOffset = GetGroundOffsetMagnitude();
        RaycastHit2D snapHit = Physics2D.Raycast(rb.position, -transform.up, fixedOffset + 0.6f, jumpableGround);

        if (snapHit.collider != null)
            rb.position = snapHit.point + snapHit.normal * fixedOffset;

        hasTurnInner = false;
        isTurningInner = false;
        activeTurnCoroutine = null;
    }




    /// <summary>
    /// Animates the rotation of the object around a specified pivot point by a given angle, updating its direction upon
    /// completion.
    /// </summary>
    /// <param name="rotationDelta">The angle, in degrees, by which to rotate the object around the pivot point. Positive values rotate clockwise;
    /// negative values rotate counterclockwise.</param>
    /// <param name="newDirection">The new direction value to assign to the object after the rotation completes.</param>
    /// <param name="pivotPoint">The world-space position to use as the pivot point for the rotation.</param>
    /// <returns>An enumerator that performs the rotation animation over time when used with a coroutine.</returns>
    private IEnumerator RotateAroundCornerOuter(float rotationDelta, int newDirection, Vector2 pivotPoint)
    {
        isTurningOuter = true;
        hasTurnOuter = true;

        float startAngle = transform.eulerAngles.z;
        float targetAngle = startAngle + rotationDelta;
        Vector2 startOffset = rb.position - pivotPoint;
        float duration = 0.15f;
        float time = 0f;

        while (time < duration)
        {
            float t = time / duration;
            float currentAngle = Mathf.LerpAngle(startAngle, targetAngle, t);
            transform.eulerAngles = new Vector3(0, 0, currentAngle);
            rb.position = pivotPoint + RotateVector2(startOffset, currentAngle - startAngle);
            time += Time.deltaTime;
            yield return null;
        }

        transform.eulerAngles = new Vector3(0, 0, targetAngle);
        rb.position = pivotPoint + RotateVector2(startOffset, targetAngle - startAngle);
        direction = newDirection;

        float fixedOffset = GetGroundOffsetMagnitude();

        RaycastHit2D snapHit = Physics2D.Raycast(rb.position, -transform.up, fixedOffset + 0.5f, jumpableGround);

        if (snapHit.collider != null)
        {
            rb.position = snapHit.point + snapHit.normal * fixedOffset;
        }
        else
        {
            RaycastHit2D boxSnap = Physics2D.BoxCast(rb.position, new Vector2(Mathf.Abs(wallPositionChecker.localPosition.x) * 0.6f, 0.05f), transform.eulerAngles.z, -transform.up, fixedOffset + 0.5f, jumpableGround);

            if (boxSnap.collider != null)
            {
                rb.position = boxSnap.point + boxSnap.normal * fixedOffset;
            }
        }

        hasTurnOuter = false;
        isTurningOuter = false;
        activeTurnCoroutine = null;
    }




    /// <summary>
    /// Smoothly rotates the player around a corner by a specified angle and updates the player's direction, optionally
    /// adjusting position to align with the new orientation.
    /// </summary>
    /// <param name="positionDelta">The positional offset to apply to the player's transform before rotation. Use Vector3.zero to skip position
    /// adjustment.</param>
    /// <param name="rotationDelta">The angle, in degrees, by which to rotate the player around the Z axis. Positive values rotate counterclockwise.</param>
    /// <param name="newDirection">The new direction value to assign to the player after rotation. This typically corresponds to a directional enum
    /// or integer representing facing direction.</param>
    /// <returns>An enumerator that performs the rotation and alignment over a short duration. Intended to be used with
    /// StartCoroutine.</returns>
    private IEnumerator RotateAroundCorner(Vector3 positionDelta, float rotationDelta, int newDirection)
    {
        isTurningLegacy = true;

        if (positionDelta != Vector3.zero)
            transform.position += positionDelta;

        float startAngle = transform.eulerAngles.z;
        float targetAngle = startAngle + rotationDelta;
        float duration = 0.15f;
        float time = 0f;

        while (time < duration)
        {
            float t = time / duration;
            transform.eulerAngles = new Vector3(0, 0, Mathf.LerpAngle(startAngle, targetAngle, t));
            time += Time.deltaTime;
            yield return null;
        }

        transform.eulerAngles = new Vector3(0, 0, targetAngle);
        direction = newDirection;
        SnapRotationToDirection();

        if (pState == PlayerState.crawl)
        {
            rb.position += (Vector2)transform.up * 0.15f;
            RaycastHit2D snapHit = Physics2D.Raycast(rb.position, -transform.up, Mathf.Abs(groundPositionChecker.localPosition.y) * transform.localScale.y + 0.6f, jumpableGround);

            // Only snap if the normal roughly matches transform.up, so we snap to the face we actually rotated toward rather than the adjacent face of a concave corner
            if (snapHit.collider != null && Vector2.Dot(snapHit.normal, transform.up) > 0.5f)
            {
                float targetDist = Vector2.Distance(rb.position, (Vector2)groundPositionChecker.position);
                rb.position = snapHit.point + snapHit.normal * targetDist;
            }
        }

        isTurningLegacy = false;
        activeTurnCoroutine = null;
    }




    /// <summary>
    /// Rotates a two-dimensional vector by the specified angle in degrees.
    /// </summary>
    /// <param name="v">The vector to rotate.</param>
    /// <param name="degrees">The angle, in degrees, by which to rotate the vector. Positive values rotate counterclockwise.</param>
    /// <returns>A new Vector2 representing the input vector rotated by the specified angle.</returns>
    private Vector2 RotateVector2(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(cos * v.x - sin * v.y, sin * v.x + cos * v.y);
    }



    /// <summary>
    /// Calculates the next direction value in a clockwise cycle based on the current direction and a turn indicator.
    /// </summary>
    /// <param name="currentDir">The current direction value. Must be one of 1, 2, 3, or 4, representing valid directions in the cycle.</param>
    /// <param name="turnSign">The turn indicator. Use 1 to advance clockwise, -1 to move counterclockwise. Other values will advance or
    /// reverse by the specified number of steps modulo 4.</param>
    /// <returns>The next direction value in the cycle after applying the turn indicator. The result is always one of 1, 2, 3, or
    /// 4.</returns>
    private int GetNextDirection(int currentDir, int turnSign)
    {
        int[] cwCycle = { 1, 2, 3, 4 };
        int idx = System.Array.IndexOf(cwCycle, currentDir);
        int nextIdx = (idx + turnSign + 4) % 4;
        return cwCycle[nextIdx];
    }




    /// <summary>
    /// Determines the cardinal direction based on the Z-axis rotation of the transform.
    /// </summary>
    /// <returns>An integer representing the direction: 1 for up, 2 for left, 3 for down, and 4 for right.</returns>
    private int GetDirectionFromRotation()
    {
        float z = transform.eulerAngles.z % 360f;
        if (z < 0) z += 360f;
        if (z >= 315f || z < 45f) return 1;
        if (z >= 45f && z < 135f) return 4;
        if (z >= 135f && z < 225f) return 3;
        return 2;
    }




    /// <summary>
    /// Finds the world-space position of the outer corner point on the tilemap surface in the specified crawl
    /// direction.
    /// </summary>
    /// <param name="crawlDirection">The direction to search for the outer corner. Positive values indicate searching to the right or upward,
    /// negative values to the left or downward, depending on the current movement direction.</param>
    /// <returns>A Vector2 representing the world-space coordinates of the detected outer corner point. If no valid corner is
    /// found, returns the current Rigidbody2D position.</returns>
    private Vector2 FindOuterCornerPoint(float crawlDirection)
    {
        int searchDirX = 0, searchDirY = 0;
        bool wantRightCorner = false;


        switch (direction)
        {
            case 1:
                {
                    searchDirX = crawlDirection > 0 ? 1 : -1;
                    wantRightCorner = crawlDirection > 0;
                }
                break;


            case 3:
                {
                    searchDirX = crawlDirection > 0 ? -1 : 1;
                    wantRightCorner = crawlDirection <= 0;
                }
                break;


            case 2:
                {
                    searchDirY = crawlDirection > 0 ? -1 : 1;
                    wantRightCorner = crawlDirection > 0;
                }
                break;


            case 4:
                {
                    searchDirY = crawlDirection > 0 ? 1 : -1;
                    wantRightCorner = crawlDirection <= 0;
                }
                break;
        }


        float footOff = Mathf.Abs(wallPositionChecker.localPosition.x) * 0.6f;
        Vector2 backFoot = rb.position - (Vector2)(transform.right * Mathf.Sign(crawlDirection) * footOff);

        Vector2 safeSampleOrigin = backFoot + (Vector2)(transform.up * 0.25f);

        RaycastHit2D surfCheck = Physics2D.Raycast(safeSampleOrigin, -transform.up, 1.5f, jumpableGround);
        Vector2 samplePoint = surfCheck.collider != null ? surfCheck.point - (Vector2)(transform.up * 0.01f) : rb.position + (Vector2)(-transform.up * 0.2f);

        Vector3Int surfaceCell = tilemap.WorldToCell(samplePoint);

        Vector3Int edgeCell = surfaceCell;
        bool foundAny = false;


        for (int i = 0; i <= 16; i++)
        {
            Vector3Int checkCell = surfaceCell + new Vector3Int(searchDirX * i, searchDirY * i, 0);

            if (tilemap.HasTile(checkCell))
            {
                edgeCell = checkCell;
                foundAny = true;
            }
            else
            {
                break;
            }
        }


        if (!foundAny)
        {
            Vector2 backPos = rb.position - (Vector2)(transform.right * Mathf.Sign(crawlDirection) * 0.2f);
            RaycastHit2D fallbackHit = Physics2D.Raycast(backPos, -transform.up, 1.5f, jumpableGround);

            if (fallbackHit.collider != null)
            {
                Vector2 fp = fallbackHit.point - (Vector2)(transform.up * 0.01f);
                edgeCell = tilemap.WorldToCell(fp);
            }
        }


        Vector3 worldPos = tilemap.GetCellCenterWorld(edgeCell);
        Vector3 half = new Vector3(tilemap.cellSize.x * tilemap.transform.localScale.x * 0.5f, tilemap.cellSize.y * tilemap.transform.localScale.y * 0.5f, 0f);


        switch (direction)
        {
            case 1:
                return wantRightCorner
                    ? new Vector2(worldPos.x + half.x, worldPos.y + half.y)
                    : new Vector2(worldPos.x - half.x, worldPos.y + half.y);

            case 3:
                return wantRightCorner
                    ? new Vector2(worldPos.x + half.x, worldPos.y - half.y)
                    : new Vector2(worldPos.x - half.x, worldPos.y - half.y);

            case 2:
                return wantRightCorner
                    ? new Vector2(worldPos.x - half.x, worldPos.y - half.y)
                    : new Vector2(worldPos.x - half.x, worldPos.y + half.y);

            case 4:
                return wantRightCorner
                    ? new Vector2(worldPos.x + half.x, worldPos.y - half.y)
                    : new Vector2(worldPos.x + half.x, worldPos.y + half.y);

            default:
                return rb.position;
        }
    }




    /// <summary>
    /// Determines whether the crawling action can be initiated based on the current position and environment
    /// conditions.
    /// </summary>
    /// <returns>true if the character is on the ground and moving upward, near a wall, or near a ceiling; otherwise, false.</returns>
    bool CanStartCrawling()
    {
        bool nearWall = Physics2D.Raycast(wallPositionChecker.position, transform.right * dirX, wallCheckDistance, jumpableGround);
        bool onGround = Grounded();
        bool nearCeiling = Physics2D.Raycast(ceilingPositionChecker.position, transform.up, ceilingCheckDistance, jumpableGround);

        if (dirY > 0 && onGround)
        {
            direction = 1;
        }
        else if (nearWall && dirX > 0)
        {
            hasTurnInner = false;
            hasTurnOuter = false;
            StartCoroutine(RotateAroundCorner(new Vector3(-0.1f, 0.1f, 0), 90f, 4));
        }
        else if (nearWall && dirX < 0)
        {
            hasTurnInner = false;
            hasTurnOuter = false;
            StartCoroutine(RotateAroundCorner(new Vector3(0.1f, 0.1f, 0), -90f, 2));
        }
        else if (nearCeiling)
        {
            hasTurnInner = false;
            hasTurnOuter = false;
            StartCoroutine(RotateAroundCorner(new Vector3(0f, 0.15f, 0), 180f, 3));
        }

        return (dirY > 0 && onGround) || nearWall || nearCeiling;
    }
    



    /// <summary>
    /// Finds the closest top corner of a tile within the camera's view that the player can reach from their current
    /// position.
    /// </summary>
    /// <param name="playerPos">The player's current position in world coordinates. Used as the starting point for reachability checks.</param>
    /// <returns>A Vector2 representing the world position of the closest reachable tile top corner, or null if no suitable
    /// corner is found.</returns>
    Vector2? FindClosestTileTopCorner(Vector2 playerPos)
    {
        Camera cam = Camera.main;
        BoundsInt bounds = GetCameraTileBounds(tilemap, cam);

        float closestDistance = float.MaxValue;
        Vector2 bestCorner = Vector2.zero;
        bool found = false;

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            if (!tilemap.HasTile(pos)) continue;
            if (tilemap.HasTile(pos + Vector3Int.up)) continue;
            if (!tilemap.HasTile(pos + Vector3Int.down)) continue;

            Vector3 worldPos = tilemap.GetCellCenterWorld(pos);
            Vector3 half = tilemap.cellSize * 0.5f;
            Vector2 topLeft = worldPos + new Vector3(-half.x, half.y);
            Vector2 topRight = worldPos + new Vector3(half.x, half.y);

            if (IsExposedCorner(topLeft, false)) TryCorner(topLeft);
            if (IsExposedCorner(topRight, true)) TryCorner(topRight);
        }

        return found ? bestCorner : null;

        void TryCorner(Vector2 corner)
        {
            if (Vector2.Distance(corner, playerPos) > 6f)
                return;

            if (corner.y <= playerPos.y)
                return;

            if (!sprite.flipX && corner.x <= playerPos.x)
                return;

            if (sprite.flipX && corner.x >= playerPos.x)
                return;


            RaycastHit2D hit = Physics2D.Linecast(playerPos, corner, jumpableGround);

            if (hit.collider != null && Vector2.Distance(hit.point, corner) > 0.02f)
                return;

            if (IsBarrierBetween(playerPos, corner))
                return;


            float dist = Vector2.Distance(playerPos, corner);

            if (dist < closestDistance)
            {
                closestDistance = dist;
                bestCorner = corner;
                found = true;
            }
        }
    }




    /// <summary>
    /// Determines whether the specified corner is exposed, meaning there is no jumpable ground immediately adjacent to
    /// it in the horizontal direction.
    /// </summary>
    /// <param name="corner">The world position of the corner to check for exposure.</param>
    /// <param name="isRightCorner">true to check the right side of the corner; false to check the left side.</param>
    /// <returns>true if the corner is exposed and not adjacent to jumpable ground in the specified direction; otherwise, false.</returns>
    bool IsExposedCorner(Vector2 corner, bool isRightCorner)
    {
        Vector2 dir = isRightCorner ? Vector2.right : Vector2.left;
        float sideOffset = tilemap.cellSize.x * 0.3f;
        return Physics2D.OverlapPoint(corner + dir * sideOffset, jumpableGround) == null;
    }




    /// <summary>
    /// Snaps the object's rotation to a predefined angle based on the current direction value.
    /// </summary>
    private void SnapRotationToDirection()
    {
        float snapAngle = direction switch
        {
            1 => 0f,
            4 => 90f,
            3 => 180f,
            2 => 270f,
            _ => 0f
        };

        transform.rotation = Quaternion.Euler(0f, 0f, snapAngle);
    }




    /// <summary>
    /// Cancels any active turn in progress and resets the object's turning state to its default orientation.
    /// </summary>
    private void CancelActiveTurn()
    {
        if (activeTurnCoroutine != null)
        {
            StopCoroutine(activeTurnCoroutine);
            activeTurnCoroutine = null;
        }


        isTurningInner = false;
        isTurningOuter = false;
        isTurningLegacy = false;
        hasTurnInner = false;
        hasTurnOuter = false;


        // Snap to a clean, consistent orientation instead of leaving a partial lerp
        SnapRotationToDirection();


        float offset = GetGroundOffsetMagnitude();
        RaycastHit2D snapHit = Physics2D.Raycast(rb.position, -transform.up, offset + 0.6f, jumpableGround);

        if (snapHit.collider != null)
            rb.position = snapHit.point + snapHit.normal * offset;
    }




    /// <summary>
    /// Forces the player to exit the crawl state if currently crawling on the specified collider.
    /// </summary>
    /// <param name="brokenCollider">The collider that, if currently supporting the player in crawl state, will trigger the exit from crawling.
    /// Cannot be null.</param>
    public void ForceExitCrawlIfOn(Collider2D brokenCollider)
    {
        if (pState != PlayerState.crawl || brokenCollider == null) return;

        RaycastHit2D hit = Physics2D.Raycast(rb.position, -transform.up, GetGroundOffsetMagnitude() + 0.5f, jumpableGround);
        if (hit.collider != brokenCollider) return;

        if (activeTurnCoroutine != null)
        {
            StopCoroutine(activeTurnCoroutine);
            activeTurnCoroutine = null;
        }

        isTurningInner = false;
        isTurningOuter = false;
        isTurningLegacy = false;
        hasTurnInner = false;
        hasTurnOuter = false;

        crawlKickTriggered = false;

        transform.rotation = Quaternion.identity;
        direction = 1;

        coll.size = new Vector2(0.8397379f, 1.615343f);
        coll.offset = new Vector2(-0.03511286f, -0.03012538f);

        pState = PlayerState.normal;
        rb.gravityScale = 1f;
        animDriver.Speed = 1f;
    }
    



    /// <summary>
    /// Forces the player character to immediately exit swinging mode and transition into crawling at the specified
    /// surface contact point.
    /// </summary>
    /// <param name="hitPoint">The world position where the player should begin crawling. This typically represents the contact point with the
    /// surface.</param>
    /// <param name="hitNormal">The normal vector of the surface at the contact point. Used to determine the orientation for crawling.</param>
    private void ForceExitSwingToCrawl(Vector2 hitPoint, Vector2 hitNormal)
    {
        visual.rotation = Quaternion.Euler(0f, 0f, 0f);

        float surfaceAngle = Mathf.Atan2(hitNormal.y, hitNormal.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, surfaceAngle - 90f);
        direction = GetDirectionFromRotation();
        SnapRotationToDirection();

        rb.position = hitPoint + hitNormal * GetGroundOffsetMagnitude();

        coll.size = new Vector2(0.8397379f, 1.615343f);
        coll.offset = new Vector2(-0.03511286f, -0.03012538f);
        audioController.Play(sndWebSnap);

        swingPointSelected = false;
        ropeRenderer.ReturnAllToPool();
        rb.gravityScale = 0;
        swingKickTriggered = false;
        swingKickTargets.Clear();
        swingKickCooldown = 0f;
        ropePhysics.StopSpinning();
        animDriver.Speed = 1f;

        pState = PlayerState.crawl;
        crawlEntryGrace = 0.1f;
    }




    /// <summary>
    /// Calculates the bounds of tiles within a tilemap that are visible to the specified camera, including a margin
    /// around the camera's viewport.
    /// </summary>
    /// <param name="tilemap">The tilemap whose tile bounds are to be calculated. Must not be null.</param>
    /// <param name="cam">The camera used to determine the visible area in world space. Must not be null.</param>
    /// <returns>A BoundsInt representing the rectangular region of tiles that are visible to the camera, expanded by a margin to
    /// ensure coverage of partially visible tiles.</returns>
    BoundsInt GetCameraTileBounds(Tilemap tilemap, Camera cam)
    {
        Vector3 min = cam.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 max = cam.ViewportToWorldPoint(new Vector3(1, 1, 0));
        Vector3Int cellMin = tilemap.WorldToCell(min);
        Vector3Int cellMax = tilemap.WorldToCell(max);
        return new BoundsInt(cellMin.x - 2, cellMin.y - 2, 0, (cellMax.x - cellMin.x) + 4, (cellMax.y - cellMin.y) + 4, 1);
    }




    /// <summary>
    /// Executes the attack logic against the specified target, handling movement, engagement state, and attack timing
    /// for the player character.
    /// </summary>
    /// <param name="target">The target <see cref="GameObject"/> to attack. Must not be null and is expected to have a <see
    /// cref="Rigidbody2D"/> component.</param>
    private void Attacking(GameObject target)
    {
        Rigidbody2D rb_target = target.GetComponent<Rigidbody2D>();
        AnimatorStateInfo si = animDriver.CurrentState;

        // Resolve engaged state through the component type
        bool targetEngaged = combatTargeting.TargetIsEngaged();

        // Distance/attack-range logic should key off whether the PLAYER is airborne, not the target
        bool playerGrounded = Grounded();

        // Stop the target if it's engaged
        if (targetEngaged && !pastHitEvent)
            rb_target.velocity = Vector2.zero;

        // Face the target
        sprite.flipX = target.transform.position.x <= transform.position.x;

        bool targetAirborne = !combatTargeting.TargetGrounded();
        bool useGroundMetric = playerGrounded && !targetAirborne;
        float attackDist = useGroundMetric ? 0.45f : 0.2f;

        // Goblin on a glider needs a tighter distance
        if (target.TryGetComponent<GoblinStep>(out var g) && g.gState == GoblinStep.GoblinState.on_glider)
            attackDist = 0.15f;

        rb.gravityScale = 0;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0;

        float dist = useGroundMetric ? Mathf.Abs(target.transform.position.x - transform.position.x) : Vector2.Distance(transform.position, target.transform.position);

        bool inMoveWindow = IsAttackMoveWindow(si);

        // if we can't close the distance for too long, whiff the attack instead of chasing forever with the hit-pause animation frozen
        if (dist >= attackDist)
        {
            attackTimeoutTimer += Time.deltaTime;

            if (attackTimeoutTimer > 1.2f)
            {
                pastHitEvent = false;
                pState = PlayerState.normal;
                postAttackBuffer = 0.1f;
                postAttackWasGrounded = Grounded();
                attacking = false;
                waitingToHit = false;
                uppercut = false;
                animDriver.Speed = 1f;
                rb.gravityScale = 1f;
                attackTimeoutTimer = 0f;

                if (targetEngaged)
                    rb_target.gravityScale = 1;

                return;
            }
        }
        else
        {
            attackTimeoutTimer = 0f;
        }

        if (dist >= attackDist && !waitingToHit && !pastHitEvent)
        {
            float step = dash_spd * Time.deltaTime;
            float actualStep = useGroundMetric ? step : Mathf.Min(step, dist - 0.05f);

            if (actualStep > 0)
                transform.position = Vector2.MoveTowards(transform.position, target.transform.position, actualStep);
        }

        if (waitingToHit)
        {
            if (dist < attackDist)
            {
                animDriver.Speed = 1;
                waitingToHit = false;
            }
            else if (!pastHitEvent)
            {
                float step = dash_spd * Time.deltaTime;
                float actualStep = useGroundMetric ? step : Mathf.Min(step, dist - 0.05f);

                if (actualStep > 0)
                    transform.position = Vector2.MoveTowards(transform.position, target.transform.position, actualStep);
            }
        }

        if (si.normalizedTime >= 1f)
        {
            pastHitEvent = false;
            pState = PlayerState.normal;
            postAttackBuffer = 0.1f;
            postAttackWasGrounded = Grounded();
            attacking = false;
            uppercut = false;
            animDriver.Speed = 1f;
            rb.gravityScale = 1;

            // Only restore target gravity if it was engaged
            if (targetEngaged)
                rb_target.gravityScale = 1;
        }
    }




    /// <summary>
    /// Processes a hit event against the current combat target or counter target, applying damage or effects as
    /// appropriate.
    /// </summary>
    public void HitEvent()
    {
        if (crawlKickTriggered)
        {
            HandleCrawlKickHit();
            return;
        }

        if (combatTargeting.CurrentTarget == null && !(countering && combatTargeting.CurrentCounterTarget != null)) return;

        float groundDist = 0.45f;
        float airDist = 0.9f;

        if (combatTargeting.CurrentTarget != null)
        {
            bool landed = Grounded() ? Vector3.Distance(combatTargeting.TargetTransform.position, transform.position) <= groundDist : Vector3.Distance(combatTargeting.TargetTransform.position, transform.position) <= airDist;

            if (attacking && landed)
            {
                bool hitConfirmed = false;

                switch (combatTargeting.CurrentTarget)
                {
                    case GoblinStep gb: hitConfirmed = gb.OnPlayerHit(gb); break;
                    case ShockerStep sh: OnHitS.Invoke(sh); hitConfirmed = true; break;
                    case RobotStep rb2: OnHit.Invoke(rb2); hitConfirmed = true; break;
                }

                if (hitConfirmed)
                {
                    if (!pastHitEvent) pastHitEvent = true;
                    combo++;
                    alarm3 = 300;
                }
            }
        }

        // Also hit the counter target (robot, goblin, or shocker) if countering
        if (countering && combatTargeting.CurrentCounterTarget != null)
        {
            Transform counterTf = combatTargeting.CounterTargetTransform;

            if (counterTf != null)
            {
                float cDist = Vector3.Distance(counterTf.position, transform.position);

                if (cDist <= (Grounded() ? groundDist : airDist))
                {
                    switch (combatTargeting.CurrentCounterTarget)
                    {
                        case RobotStep counterRobot: OnHit.Invoke(counterRobot); break;
                        case GoblinStep counterGoblin: counterGoblin.OnPlayerHit(counterGoblin, isCounterHit: true); break;
                        case ShockerStep counterShocker: OnHitS.Invoke(counterShocker); break;
                    }

                    combo++;
                    alarm3 = 300;
                }
            }
        }
    }




    /// <summary>
    /// Handles the logic for detecting and processing hits when performing a crawl kick attack.
    /// </summary>
    private void HandleCrawlKickHit()
    {
        float hitRange = 0.85f;
        float kickSign = sprite.flipX ? -1f : 1f;
        Vector2 hitCenter = (Vector2)transform.position + (Vector2)(transform.right * kickSign * 0.5f);

        Collider2D[] hits = Physics2D.OverlapCircleAll(hitCenter, hitRange + 0.94f, enemyMask);
        var seen = new HashSet<Component>();

        foreach (var hit in hits)
        {
            Component enemy = (Component)hit.GetComponent<RobotStep>() ?? (Component)hit.GetComponent<GoblinStep>() ?? (Component)hit.GetComponent<ShockerStep>();
            if (enemy == null || !seen.Add(enemy)) continue;

            if (enemy is RobotStep r && r.eState == RobotStep.EnemyState.death) continue;
            if (enemy is GoblinStep g && g.gState == GoblinStep.GoblinState.death) continue;
            if (enemy is ShockerStep s && s.sState == ShockerStep.ShockerState.death) continue;

            Vector2 hitPos = ((MonoBehaviour)enemy).transform.position;

            if (Vector2.Distance(transform.position, hitPos) > hitRange) continue;

            switch (enemy)
            {
                case RobotStep robot: OnHit.Invoke(robot); break;
                case GoblinStep goblin: goblin.OnPlayerHit(goblin); break;
                case ShockerStep shockerE: OnHitS.Invoke(shockerE); break;
            }

            combo++;
            alarm3 = 300;
            SpawnHitEffect(hitPos);
        }
    }




    /// <summary>
    /// Pauses the animation and movement of the current attack or counter target before executing a hit.
    /// </summary>
    public void PauseBeforeHit()
    {
        Animator tAnim = combatTargeting.TargetAnim;
        Rigidbody2D tRB = combatTargeting.TargetRB;

        if (attacking && tAnim != null)
        {
            animDriver.Speed = 0;

            if (combatTargeting.TargetIsEngaged())
            {
                tAnim.speed = 0;
                tRB.velocity = Vector2.zero;
            }

            waitingToHit = true;
        }

        if (countering && combatTargeting.CurrentCounterTarget != null)
        {
            animDriver.Speed = 0;
            Animator ctAnim = combatTargeting.CounterTargetAnim;
            Rigidbody2D ctRB = combatTargeting.CounterTargetRB;
            if (ctAnim != null) ctAnim.speed = 0;
            if (ctRB != null) ctRB.velocity = Vector2.zero;
            waitingToHit = true;
        }
    }
    



    /// <summary>
    /// Updates the current combat target based on the specified origin position and the player's facing direction.
    /// Selects the nearest valid enemy within range and line of sight for targeting.
    /// </summary>
    /// <param name="origin">The world position from which to search for potential combat targets. Typically represents the player's current
    /// position.</param>
    private void RefreshCombatTarget(Vector2 origin)
    {
        bool facingLeft = sprite.flipX;

        if (playerInput.Horizontal > 0)
            facingLeft = false;
        else if (playerInput.Horizontal < 0)
            facingLeft = true;

        Collider2D[] ehits = Physics2D.OverlapCircleAll(origin, 5.2f, enemyMask);
        float closestEDistance = Mathf.Infinity;
        RobotStep closestEnemy = null;

        foreach (var ehit in ehits)
        {
            RobotStep enemy = ehit.GetComponent<RobotStep>();

            if (enemy == null || enemy.eState == RobotStep.EnemyState.death || !enemy.IsTargetable) continue;


            RaycastHit2D hit = Physics2D.Linecast(transform.position, enemy.transform.position, jumpableGround);

            if (hit.collider != null && (Vector2)hit.point != (Vector2)enemy.transform.position) continue;


            bool noLightning = true;

            foreach (var hl in Physics2D.LinecastAll(transform.position, enemy.transform.position))
            {
                LightningScript ls = hl.collider?.GetComponent<LightningScript>();
                if (ls != null && ls.phase == 0) { noLightning = false; break; }
            }

            if (!noLightning) continue;


            float dx = enemy.transform.position.x - origin.x;

            if ((facingLeft && dx > 0) || (!facingLeft && dx < 0)) continue;


            float dist = Mathf.Abs(dx);

            if (dist < closestEDistance)
            {
                closestEDistance = dist;
                closestEnemy = enemy;
            }
        }

        currentTarget = closestEnemy;
        combatTargeting.CurrentTarget = combatTargeting.ResolveTarget(origin, facingLeft, transform, jumpableGround, closestEnemy, closestEDistance);
    }




    /// <summary>
    /// Spawns a visual hit effect at the specified impact point.
    /// </summary>
    /// <param name="impactPoint">The position, in world coordinates, where the hit effect will be instantiated.</param>
    public void SpawnHitEffect(Vector2 impactPoint) { Instantiate(hitParticlePrefab, impactPoint, Quaternion.identity); }




    /// <summary>
    /// Spawns a visual effect at the specified point to indicate that damage has occurred.
    /// </summary>
    /// <param name="impactPoint">The world position where the hurt effect should be instantiated.</param>
    public void SpawnHurtEffect(Vector2 impactPoint) { Instantiate(hurtParticlePrefab, impactPoint, Quaternion.identity); }




    /// <summary>
    /// Provides a mapping between attack-related movement states and their corresponding animation names.
    /// </summary>
    private static readonly Dictionary<MovementState, string> AttackAnimNames = new Dictionary<MovementState, string>
    {
        { MovementState.punch1, "Player_Punch1" },
        { MovementState.punch2, "Player_Punch2" },
        { MovementState.punch3, "Player_Punch3" },
        { MovementState.punch4, "Player_Punch4" },
        { MovementState.kick1, "Player_Kick1" },
        { MovementState.kick2, "Player_Kick2" },
        { MovementState.airpunch, "Player_Air_Punch" },
        { MovementState.airkick, "Player_Air_Kick" },
        { MovementState.uppercut, "Player_Uppercut" },
        { MovementState.crawlkick, "Player_Crawl_Kick" },
    };




    /// <summary>
    /// Plays the attack animation corresponding to the specified movement state.
    /// </summary>
    /// <param name="state">The movement state for which to play the associated attack animation.</param>
    private void PlayAttackAnimation(MovementState state)
    {
        animDriver.SetMovementState((int)state);

        if (AttackAnimNames.TryGetValue(state, out string clipName))
        {
            animDriver.Play(clipName, 0f);
        }
    }

    


    /// <summary>
    /// Applies damage to the player as a result of an interaction with the specified enemy step.
    /// </summary>
    /// <param name="target">The enemy step that is causing the damage. Must not be null.</param>
    public void Damage(RobotStep target)
    {
        if (pState == PlayerState.death) return;
        if (!countering) ApplyHurtFromEnemy(target.sprite.flipX, target.kick, target.transform.position);
        else currentCounter?.OnPlayerHit(currentCounter);
    }




    /// <summary>
    /// Applies damage to the specified goblin target, handling counter and non-counter scenarios.
    /// </summary>
    /// <param name="target">The goblin to damage. Must not be null.</param>
    public void DamageGoblin(GoblinStep target)
    {
        if (pState == PlayerState.death) return;
        if (!countering) ApplyHurtFromEnemy(target.sprite.flipX, false, target.transform.position);
        else target.OnPlayerHit(target, isCounterHit: true);
    }




    /// <summary>
    /// Applies damage to the player or triggers a counterattack when interacting with the specified shocker step.
    /// </summary>
    /// <param name="target">The shocker step that is interacting with the player. Cannot be null.</param>
    public void DamageShocker(ShockerStep target)
    {
        if (pState == PlayerState.death) return;
        if (!countering) ApplyHurtFromEnemy(target.sprite.flipX, target.kick, target.transform.position);
        else target.OnPlayerHit(target);
    }




    /// <summary>
    /// Applies damage and knockback effects to the player when hit by an enemy attack.
    /// </summary>
    /// <param name="enemyFacingLeft">Indicates whether the enemy is facing left. If <see langword="true"/>, the knockback is applied to the left;
    /// otherwise, to the right.</param>
    /// <param name="isKick">Specifies whether the enemy attack is a kick. If <see langword="true"/>, the player receives greater knockback
    /// and damage.</param>
    /// <param name="enemyPos">The world position of the enemy at the time of the attack. Used to position visual effects and track the source
    /// of the hit.</param>
    private void ApplyHurtFromEnemy(bool enemyFacingLeft, bool isKick, Vector3 enemyPos)
    {
        float dir = enemyFacingLeft ? -1f : 1f;
        dirX = dir;

        // Force collider back to normal size/offset in case we were hit mid-swing/zip
        coll.size = new Vector2(0.8397379f, 1.615343f);
        coll.offset = new Vector2(-0.03511286f, -0.03012538f);
        coll.isTrigger = false;

        // Also clear combat flags so we don't leave an enemy target frozen mid pause
        attacking = false;
        countering = false;
        waitingToHit = false;

        rb.velocity = isKick ? new Vector2(dir * 3f, 5f) : new Vector2(dir, 0f);
        animDriver.Speed = 1f;
        combo = 0;
        pState = PlayerState.hurt;

        MovementState mstate;

        if (isKick)
        {
            mstate = MovementState.launched;
            launchGroundGrace = 0.2f;
            launchTechTimer = 0f;
            audioController.PlayRandom(sndStrongHit, sndStrongHit2);
        }
        else
        {
            mstate = UnityEngine.Random.Range(0, 2) == 0 ? MovementState.hurt1 : MovementState.hurt2;
            audioController.PlayRandom(sndQuickHit, sndQuickHit2);
        }

        animDriver.SetMovementState((int)mstate);
        enemyHitSpawn = enemyPos;
        SpawnHurtEffect(enemyPos);

        if (health > 0)
        {
            health -= isKick ? 5 : 4;
            healthbar.UpdateHealthBar(health, maxHealth);
        }

        audioController.PlayRandom(sndHurt, sndHurt2, sndHurt3);
    }




    /// <summary>
    /// Decides which colored key to remove based on input.
    /// </summary>
    /// <param name="colorToRemove">The key color to remove.</param>
    private void RemoveKeyByColor(string colorToRemove)
    {
        Keys[] allKeys = FindObjectsOfType<Keys>();
        Keys keyToRemove = null;
        int removedKeyIndex = -1;

        foreach (Keys key in allKeys)
        {
            if (key.keyColor == colorToRemove)
            {
                keyToRemove = key;
                removedKeyIndex = key.keyIndex;
                break;
            }
        }

        if (keyToRemove == null) return;

        keys--;

        if (removedKeyIndex == 1)
        {
            keyColor1 = keyColor2;
            keyColor2 = keyColor3;
            keyColor3 = "nothing";
        }
        else if (removedKeyIndex == 2)
        {
            keyColor2 = keyColor3;
            keyColor3 = "nothing";
        }
        else if (removedKeyIndex == 3)
        {
            keyColor3 = "nothing";
        }

        foreach (Keys key in allKeys)
        {
            if (key != keyToRemove && key.keyIndex > removedKeyIndex) { key.keyIndex--; }
        }

        Destroy(keyToRemove.gameObject);
    }




    /// <summary>
    /// Determines whether there is a collider tagged as "Barrier" between two points in 2D space.
    /// </summary>
    /// <param name="from">The starting point of the line segment to check for barriers.</param>
    /// <param name="to">The ending point of the line segment to check for barriers.</param>
    /// <returns>true if a collider with the tag "Barrier" exists between the specified points; otherwise, false.</returns>
    private bool IsBarrierBetween(Vector2 from, Vector2 to)
    {
        foreach (RaycastHit2D hit in Physics2D.LinecastAll(from, to))
        {
            if (hit.collider != null && hit.collider.CompareTag("Barrier")) return true;
        }

        return false;
    }




    /// <summary>
    /// Updates the player's contact state with barriers and blocking enemies based on the current collider position.
    /// </summary>
    private void UpdateBarrierContact()
    {
        Bounds b = coll.bounds;
        Vector2 probeSize = new Vector2(b.size.x + 0.1f, b.size.y * 0.9f);
        Collider2D[] hits = Physics2D.OverlapBoxAll(b.center, probeSize, 0f);

        barrierContactDir = 0;
        blockingEnemyCollider = null;

        foreach (var hit in hits)
        {
            if (hit.gameObject == this.gameObject) continue;
            if (!hit.CompareTag("Barrier")) continue;

            // Work out which side the barrier is on from the nearest point on its collider,
            // not its transform origin. A wide barrier (or one with an off-centre pivot) can have
            // its origin on the opposite side from the surface actually being touched, which would
            // block the player's movement in the wrong direction and stick them to the wall.
            Vector2 closest = hit.ClosestPoint(b.center);
            float dx = closest.x - b.center.x;

            // ClosestPoint returns the query point itself when it is inside the collider,
            // so fall back to the collider's centre in that case.
            if (Mathf.Abs(dx) < 0.01f) dx = hit.bounds.center.x - b.center.x;
            if (Mathf.Abs(dx) < 0.01f) continue;

            barrierContactDir = dx > 0 ? 1 : -1;
            break;
        }

        bool playerIsPassable = IsPhysicallyPassable() || pState == PlayerState.crawl || pState == PlayerState.quickzip || pState == PlayerState.swing;

        if (barrierContactDir == 0 && !playerIsPassable)
        {
            float castDist = b.size.x * 0.5f + 0.15f;
            float castH = b.size.y * 0.75f;
            Vector2 castSize = new Vector2(0.05f, castH);

            RaycastHit2D rHit = Physics2D.BoxCast(b.center, castSize, 0f, Vector2.right, castDist, enemyMask);

            if (rHit.collider != null && IsEnemySolid(rHit.collider))
            {
                barrierContactDir = 1;
                blockingEnemyCollider = rHit.collider;
            }

            if (barrierContactDir == 0)
            {
                RaycastHit2D lHit = Physics2D.BoxCast(b.center, castSize, 0f, Vector2.left, castDist, enemyMask);

                if (lHit.collider != null && IsEnemySolid(lHit.collider))
                {
                    barrierContactDir = -1;
                    blockingEnemyCollider = lHit.collider;
                }
            }
        }
    }




    /// <summary>
    /// Determines whether the specified enemy collider represents a barrier that is solid to the player.
    /// </summary>
    /// <param name="enemyColl">The collider to check for solidity. Must not be null and should reference an object that may implement the
    /// IEnemyBarrier interface.</param>
    /// <returns>true if the collider implements IEnemyBarrier and is solid to the player; otherwise, false.</returns>
    private bool IsEnemySolid(Collider2D enemyColl)
    {
        IEnemyBarrier barrier = enemyColl.GetComponent<IEnemyBarrier>();
        return barrier != null && barrier.IsSolidToPlayer;
    }




    /// <summary>
    /// Used for handling collisions between Enemy and Player to prevent running/walking through each other.
    /// </summary>
    private void UpdateEnemyTopBlock()
    {
        bool playerPassable = IsPhysicallyPassable() || pState == PlayerState.crawl || pState == PlayerState.quickzip || pState == PlayerState.swing;

        if (playerPassable) return;

        Bounds pb = coll.bounds;
        Collider2D[] hits = Physics2D.OverlapBoxAll(pb.center, pb.size, 0f, enemyMask);

        foreach (var hit in hits)
        {
            IEnemyBarrier barrier = hit.GetComponent<IEnemyBarrier>();
            if (barrier == null || !barrier.IsSolidToPlayer) continue;

            Bounds eb = barrier.BarrierCollider.bounds;

            bool onTop = pb.min.y >= eb.center.y && pb.min.y <= eb.max.y + 0.15f;
            if (!onTop) continue;

            float pushDir = (transform.position.x >= eb.center.x) ? 1f : -1f;

            rb.position += new Vector2(pushDir * 0.05f, 0f);

            if (rb.velocity.y > -0.5f)
                rb.velocity = new Vector2(rb.velocity.x, -0.5f);

            barrier.NudgeAway(-pushDir);
        }
    }




    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (pState != PlayerState.death && collision.gameObject.CompareTag("Health"))
        {
            health += 8;
            healthbar.UpdateHealthBar(health, maxHealth);
            audioController.Play(sndHealth);
            collision.GetComponent<Animator>().Play("HealthCollect");
            collision.GetComponent<SpriteRenderer>().material = noOutlineMaterial;
            Destroy(collision.gameObject, 0.1f);
        }

        if (pState != PlayerState.death && collision.gameObject.CompareTag("Arrow"))
        {
            audioController.Play(sndLevelComplete);
            collision.gameObject.GetComponent<GoalArrow>().levelComplete();
        }

        if (pState != PlayerState.death && collision.gameObject.CompareTag("Trigger"))
        {
            var ot = collision.gameObject.GetComponent<ObjectiveTrigger>();

            bool shouldActivate = !ot.active && (
                (ot.missionType == 1 && ot.missionObjective.GetComponent<HostageScript>().phase == 0) ||
                (ot.missionType == 2) ||
                (ot.missionType == 3) ||
                (ot.missionType == 4 && ot.missionObjective.GetComponent<ExplosiveScript>().phase == 0)
            );

            if (shouldActivate)
            {
                ot.countdown = true;
                ot.active = true;
                ot.start = true;
                trigger = true;
                audioController.Play(sndWarning);
                alarm4 = 60;
            }
        }
    }




    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Car"))
        {
            rb.WakeUp();
            Animator carAnim = collision.GetComponent<Animator>();
            bool carNormal = carAnim.GetCurrentAnimatorStateInfo(0).IsName("CarNormal");
            AnimatorStateInfo si = animDriver.CurrentState;

            bool hittingToward = (transform.position.x > collision.transform.position.x && sprite.flipX) || (transform.position.x < collision.transform.position.x && !sprite.flipX);

            if (carNormal && CanBreakObjects(si) && hittingToward)
            {
                rb.WakeUp();
                rb.position = rb.position;
                audioController.Play(sndCarBreak);
                collision.GetComponent<Animator>().Play("CarBreak");
            }
        }

        if (collision.gameObject.CompareTag("Door"))
        {
            rb.WakeUp();
            AnimatorStateInfo si = animDriver.CurrentState;

            if (collision.gameObject.GetComponent<BreakableDoor>().phase == 0 && CanBreakObjects(si))
            {
                rb.WakeUp();
                rb.position = rb.position;
                collision.gameObject.GetComponent<BreakableDoor>().phase = 1;
            }
        }

        if (collision.gameObject.CompareTag("RedKeyDoor"))
        {
            rb.WakeUp();

            if (collision.gameObject.GetComponent<KeyDoors>().phase == 0 && keys > 0 && (keyColor1 == "red" || keyColor2 == "red" || keyColor3 == "red"))
            {
                rb.WakeUp();
                rb.position = rb.position;
                collision.gameObject.GetComponent<KeyDoors>().phase = 1;
                RemoveKeyByColor("red");
            }
        }

        if (collision.gameObject.CompareTag("BlueKeyDoor"))
        {
            rb.WakeUp();

            if (collision.gameObject.GetComponent<KeyDoors>().phase == 0 && keys > 0 && (keyColor1 == "blue" || keyColor2 == "blue" || keyColor3 == "blue"))
            {
                rb.WakeUp();
                rb.position = rb.position;
                collision.gameObject.GetComponent<KeyDoors>().phase = 1;
                RemoveKeyByColor("blue");
            }
        }

        if (collision.gameObject.CompareTag("YellowKeyDoor"))
        {
            rb.WakeUp();

            if (collision.gameObject.GetComponent<KeyDoors>().phase == 0 && keys > 0 && (keyColor1 == "yellow" || keyColor2 == "yellow" || keyColor3 == "yellow"))
            {
                rb.WakeUp();
                rb.position = rb.position;
                collision.gameObject.GetComponent<KeyDoors>().phase = 1;
                RemoveKeyByColor("yellow");
            }
        }

        if (collision.gameObject.CompareTag("Switch"))
        {
            rb.WakeUp();
            AnimatorStateInfo si = animDriver.CurrentState;

            if (collision.gameObject.GetComponent<BreakableSwitch>().phase == 0 && CanBreakObjects(si))
            {
                rb.WakeUp();
                rb.position = rb.position;
                collision.gameObject.GetComponent<BreakableSwitch>().phase = 1;
            }
        }

        if (collision.gameObject.CompareTag("Explosive"))
        {
            rb.WakeUp();
            AnimatorStateInfo si = animDriver.CurrentState;

            if (collision.gameObject.GetComponent<ExplosiveScript>().phase == 0 && CanBreakObjects(si))
            {
                rb.WakeUp();
                rb.position = rb.position;
                collision.gameObject.GetComponent<ExplosiveScript>().phase = 1;
            }
        }

        if (collision.gameObject.CompareTag("Generator"))
        {
            rb.WakeUp();
            AnimatorStateInfo si = animDriver.CurrentState;

            if (collision.gameObject.GetComponent<BreakableSwitch>().phase == 0 && CanBreakObjects(si))
            {
                rb.WakeUp();
                rb.position = rb.position;
                collision.gameObject.GetComponent<BreakableSwitch>().phase = 1;
            }
        }

        if (collision.gameObject.CompareTag("Wires"))
        {
            if (pState == PlayerState.death)
                return;

            rb.WakeUp();


            Animator wireAnim = collision.GetComponent<Animator>();
            bool wireIsActive = wireAnim.GetCurrentAnimatorStateInfo(0).IsName("WiresActive");


            if (wireIsActive && !wireWasActive)
                wireHitCooldown = 0f;

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


            float dir = sprite.flipX ? 1f : -1f;
            dirX = dir;
            rb.velocity = new Vector2(dir * 3f, 5f);

            animDriver.Speed = 1f;
            combo = 0;
            pState = PlayerState.hurt;

            animDriver.SetMovementState((int)MovementState.launched);
            launchGroundGrace = 0.2f;
            launchTechTimer = 0f;
            enemyHitSpawn = collision.transform.position;
            SpawnHurtEffect(transform.position);

            health -= 8;
            healthbar.UpdateHealthBar(health, maxHealth);

            audioController.PlayRandom(sndStrongHit, sndStrongHit2);
            audioController.PlayRandom(sndHurt, sndHurt2, sndHurt3);
        }

        if (collision.gameObject.CompareTag("Lightning"))
        {
            if (pState == PlayerState.death)
                return;

            rb.WakeUp();


            Animator wireAnim = collision.GetComponent<Animator>();
            bool wireIsActive = wireAnim.GetCurrentAnimatorStateInfo(0).IsName("LightningActive");


            if (wireIsActive && !lightningWasActive)
                lightningHitCooldown = 0f;

            lightningWasActive = wireIsActive;

            if (!wireIsActive)
                return;

            if (lightningHitCooldown > 0f)
            {
                lightningHitCooldown -= Time.deltaTime;
                return;
            }

            lightningHitCooldown = 0.15f;


            float dir = sprite.flipX ? 1f : -1f;
            dirX = dir;
            rb.velocity = new Vector2(dir * 2f, 5f);

            animDriver.Speed = 1f;
            combo = 0;
            pState = PlayerState.hurt;

            MovementState mstate = UnityEngine.Random.Range(0, 2) == 0 ? MovementState.hurt1 : MovementState.hurt2;
            animDriver.SetMovementState((int)mstate);
            enemyHitSpawn = collision.transform.position;
            SpawnHurtEffect(transform.position);

            health -= 8;
            healthbar.UpdateHealthBar(health, maxHealth);

            audioController.PlayRandom(sndQuickHit, sndQuickHit2);
            audioController.PlayRandom(sndHurt, sndHurt2, sndHurt3);
        }

        if (collision.gameObject.CompareTag("OneHitHazard"))
        {
            if (pState == PlayerState.death)
                return;

            rb.WakeUp();
            rb.position = rb.position;

            if (hitCooldown > 0f)
            {
                hitCooldown -= Time.deltaTime;
                return;
            }

            hitCooldown = 0.15f;


            float dir = sprite.flipX ? 1f : -1f;
            dirX = dir;
            float dY = collision.transform.position.y > transform.position.y ? -0.7f : 1f;
            rb.velocity = new Vector2(dir * 3f, 5f * dY);

            animDriver.Speed = 1f;
            combo = 0;
            pState = PlayerState.hurt;

            animDriver.SetMovementState((int)MovementState.launched);
            launchTechTimer = 0f;
            launchGroundGrace = 0.2f;
            enemyHitSpawn = collision.transform.position;
            SpawnHurtEffect(transform.position);

            health -= 8;
            healthbar.UpdateHealthBar(health, maxHealth);

            audioController.PlayRandom(sndStrongHit, sndStrongHit2);
            audioController.PlayRandom(sndHurt, sndHurt2, sndHurt3);
        }

        if (collision.gameObject.CompareTag("Hydrant"))
        {
            if (collision.GetComponent<FireHydrant>().webbed)
                return;

            if (pState == PlayerState.death)
                return;

            rb.WakeUp();
            rb.position = rb.position;

            if (hitCooldown > 0f)
            {
                hitCooldown -= Time.deltaTime;
                return;
            }

            hitCooldown = 0.15f;


            float dir = sprite.flipX ? 1f : -1f;
            dirX = dir;
            float dY = collision.transform.position.y > transform.position.y ? -0.7f : 1f;
            rb.velocity = new Vector2(dir * 3f, 5f * dY);
            animDriver.Speed = 1f;

            combo = 0;
            pState = PlayerState.hurt;

            animDriver.SetMovementState((int)MovementState.launched);
            launchTechTimer = 0f;
            launchGroundGrace = 0.2f;
            enemyHitSpawn = collision.transform.position;
            SpawnHurtEffect(transform.position);

            health -= 8;
            healthbar.UpdateHealthBar(health, maxHealth);

            audioController.PlayRandom(sndStrongHit, sndStrongHit2);
            audioController.PlayRandom(sndHurt, sndHurt2, sndHurt3);
        }

        if (collision.gameObject.CompareTag("Glider"))
        {
            if (collision.gameObject.GetComponent<GliderScript>().state != GliderScript.GState.Zooming)
                return;

            if (pState == PlayerState.death)
                return;

            rb.WakeUp();
            rb.position = rb.position;

            if (hitCooldown > 0f)
            {
                hitCooldown -= Time.deltaTime;
                return;
            }

            hitCooldown = 0.02f;


            float dir = sprite.flipX ? 1f : -1f;
            dirX = dir;
            float dY = collision.transform.position.y > transform.position.y ? -0.7f : 1f;
            rb.velocity = new Vector2(dir * 3f, 5f * dY);

            animDriver.Speed = 1f;
            combo = 0;
            pState = PlayerState.hurt;

            animDriver.SetMovementState((int)MovementState.launched);
            launchTechTimer = 0f;
            launchGroundGrace = 0.2f;
            enemyHitSpawn = collision.transform.position;
            SpawnHurtEffect(transform.position);

            health -= 3;
            healthbar.UpdateHealthBar(health, maxHealth);

            audioController.PlayRandom(sndStrongHit, sndStrongHit2);
            audioController.PlayRandom(sndHurt, sndHurt2, sndHurt3);
        }
    }
}