using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI.Table;

public class PlayerStep : MonoBehaviour
{
    public Rigidbody2D rb;
    [SerializeField] public Animator anim;
    [SerializeField] public SpriteRenderer sprite;
    [SerializeField] private Transform visual;
    public BoxCollider2D coll;
    public CircleCollider2D collCircle;
    public float dirX = 0f;
    public float dirY = 0f;


    // Swinging variables
    private float ropeAngle = 0f;
    private float ropeAngleVelocity = 0f;
    private float ropeX = 0f;
    private float ropeY = 0f;
    private float grappleX = 0f;
    private float grappleY = 0f;
    private float ropeLength = 0f;
    public bool swingEnd = false;
    [SerializeField] private float accelerationRate = -0.02f;
    [SerializeField] private bool swingPointSelected = false;


    // Swing kick variables
    private bool swingKickTriggered = false;
    private List<Component> swingKickTargets = new List<Component>();
    private float swingKickCooldown = 0f;
    private const float SwingKickVelocityThreshold = 2.0f;


    // Crawling variables
    [SerializeField] private bool groundDetected;
    [SerializeField] private bool wallDetected;
    [SerializeField] private Transform groundPositionChecker;
    [SerializeField] private Transform wallPositionChecker;
    [SerializeField] private Transform ceilingPositionChecker;
    private float wallCheckDistance = 0.1f;
    private float ceilingCheckDistance = 0.1f;
    private float ZaxisAdd;
    private int direction;
    private float crawlDir = 0f;
    private bool shoot = false;
    private float zipFromCrawlGrace = 0f;
    private Vector2 crawlShootAimDir = Vector2.zero;


    // Inner corner system
    private bool hasTurnInner = false;
    private bool isTurningInner = false;


    // Outer corner system
    private bool hasTurnOuter = false;
    private bool isTurningOuter = false;
    private float cliffTimer = 0f;

    private bool isTurningLegacy = false;

    // Combined flags so the swing and zip code can read/reset turn state as a single value
    private bool hasTurn
    {
        get => hasTurnInner || hasTurnOuter;

        set
        {
            hasTurnInner = value;
            hasTurnOuter = value;
        }
    }
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


    // Zip variables
    [SerializeField] private Transform quickZipTarget;
    [SerializeField] private Tilemap tilemap;
    public Vector2? moveTarget = null;
    private float zipTravelDist = 0f;
    private float crawlEntryGrace = 0f;

    [SerializeField] private LayerMask jumpableGround;
    [SerializeField] private LayerMask swingPoint;
    private float hsp = 4f;
    public float jspd = 5f;
    [SerializeField] public GameObject ropeSegmentPrefab;
    private float ropeSegmentLength = 0.15f;
    private List<GameObject> ropeSegments = new List<GameObject>();
    private Queue<GameObject> ropeSegmentPool = new Queue<GameObject>();
    private int maxPoolSize = 200;

    public enum MovementState { idle, running, jumping, falling, swinging, endswing, crawling, zip, groundshoot, airshoot, crawlshoot, punch1, punch2, punch3, punch4, airkick, airpunch, kick1, kick2, uppercut, launched, hurt1, hurt2, block1, block2, block3, block4, death, swingkick }
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
    private float senseSoundTimer = 0f;
    private bool wasGrounded = false;


    // Alarms
    private int alarm1 = 0;
    private int alarm2 = 0;
    private bool startAlarm2 = false;
    public int alarm3 = 0;
    public int alarm4 = 0;

    public bool trigger = false;


    // Combat
    public RobotStep currentTarget = null;
    public GoblinStep boss = null;
    public ShockerStep shocker = null;
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
    private float attackTimeoutTimer = 0f;
    private float attackCooldown = 0f;
    private float attackCooldownDuration = 0.6f;    // minimum time between attack starts

    // Unified combat target, can be a robot, goblin, or shocker
    private Component currentCombatTarget = null;

    private Transform CombatTargetTransform => currentCombatTarget switch
    {
        RobotStep r => r.transform,
        GoblinStep g => g.transform,
        ShockerStep s => s.transform,
        _ => null
    };

    private Rigidbody2D CombatTargetRB => currentCombatTarget switch
    {
        RobotStep r => r.rb,
        GoblinStep g => g.rb,
        ShockerStep s => s.rb,
        _ => null
    };

    private Animator CombatTargetAnim => currentCombatTarget switch
    {
        RobotStep r => r.anim,
        GoblinStep g => g.anim,
        ShockerStep s => s.anim,
        _ => null
    };

    private bool CombatTargetIsEngaged()
    {
        return currentCombatTarget switch
        {
            GoblinStep g => g.gState == GoblinStep.GoblinState.engaged,
            ShockerStep s => s.sState == ShockerStep.ShockerState.engaged,
            RobotStep r => r.isEngaged,
            _ => false
        };
    }

    private bool CombatTargetGrounded()
    {
        return currentCombatTarget switch
        {
            RobotStep r => r.Grounded(),
            GoblinStep g => g.Grounded(),
            ShockerStep s => s.Grounded(),
            _ => false
        };
    }

    private bool CombatTargetIsDead()
    {
        return currentCombatTarget switch
        {
            RobotStep r => r.eState == RobotStep.EnemyState.death,
            GoblinStep g => g.gState == GoblinStep.GoblinState.death,
            ShockerStep s => s.sState == ShockerStep.ShockerState.death,
            _ => true
        };
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
    public bool againstBarrier = false;
    private float lightningHitCooldown = 0f;
    private bool lightningWasActive = false;
    private float hitCooldown = 0f;
    public int keys = 0;
    public string keyColor1 = "nothing";
    public string keyColor2 = "nothing";
    public string keyColor3 = "nothing";
    public bool stopMove = false;


    // Calculate dash speed based on distance to a transform
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

    // Pick a random attack animation
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

    // Pick a random counter animation
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

    private void PlayAttackSounds()
    {
        AudioClip[] clips = { sndAttack, sndAttack2, sndAttack3 };
        int index = UnityEngine.Random.Range(0, clips.Length + 1);
        if (index < clips.Length) audioSrc.PlayOneShot(clips[index]);

        AudioClip[] clips2 = { sndSwipe, sndSwipe2, sndSwipe3 };
        int index2 = UnityEngine.Random.Range(0, clips2.Length);
        audioSrc.PlayOneShot(clips2[index2]);
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<BoxCollider2D>();
        collCircle = GetComponent<CircleCollider2D>();
        pState = PlayerState.normal;
        direction = 1;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        healthbar.UpdateHealthBar(health, maxHealth);
        boss = FindObjectOfType<GoblinStep>();
        shocker = FindObjectOfType<ShockerStep>();
        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Enemy"), true);
    }

    void FixedUpdate()
    {
        if (pState == PlayerState.swing)
        {
            rb.MovePosition(new Vector2(ropeX, ropeY));
        }
    }

    void Update()
    {
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
        _prevPState = pState;

        UpdateBarrierContact();
        UpdateEnemyTopBlock();

        if (senseSoundTimer > 0) senseSoundTimer -= Time.deltaTime;
        if (attackCooldown > 0f) attackCooldown -= Time.deltaTime;

        Debug.Log(1 / Time.unscaledDeltaTime); // FPS counter
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

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
                AudioClip[] clips = { sndWebTension, sndWebTension2, sndWebTension3 };
                audioSrc.PlayOneShot(clips[UnityEngine.Random.Range(0, clips.Length)]);
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
                    #if UNITY_EDITOR
                        EditorApplication.isPlaying = false;
                    #else
                            Application.Quit();
                    #endif
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

        if (senseSoundTimer > 0) senseSoundTimer -= Time.deltaTime;

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

        if (!countering) currentCounter = closestCounter;

        // Spider sense
        if ((trigger || currentCounter != null) && !spiderSense && pState != PlayerState.death)
        {
            Instantiate(sensePrefab, transform.position, Quaternion.identity);

            if (senseSoundTimer <= 0f)
            {
                audioSrc.PlayOneShot(sndSpiderSense);
                senseSoundTimer = sndSpiderSense.length;
            }

            spiderSense = true;
        }
        else if (currentCounter == null || !trigger)
        {
            spiderSense = false;
        }

        if (health <= 0)
            pState = PlayerState.death;

        if (health > maxHealth)
            health = maxHealth;

        comboText.text = combo == 0 ? "" : "x" + combo;

        if (pState != PlayerState.quickzip && pState != PlayerState.swing)
            ReturnAllRopeSegmentsToPool();

        keys = Mathf.Clamp(keys, 0, 3);

        // Gravity scale can get stuck at 0 after an attack ends, this is a safety net to catch that
        if (rb.gravityScale == 0f)
        {
            bool gravityLegitZero = pState == PlayerState.swing || pState == PlayerState.crawl || pState == PlayerState.quickzip || (pState == PlayerState.dashenemy && (attacking || countering));
            if (!gravityLegitZero) rb.gravityScale = 1f;
        }

        if (stopMove) pState = PlayerState.normal;

        switch (pState)
        {
            case PlayerState.normal:
                {
                    visual.rotation = Quaternion.Euler(0, 0, 0);

                    bool movementKey = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow);
                    bool otherKeyPressed = Input.anyKey && !movementKey;

                    if (!otherKeyPressed)
                    {
                        coll.size = new Vector2(0.8397379f, 1.615343f);
                        coll.offset = new Vector2(-0.03511286f, -0.03012538f);
                    }

                    if (!stopMove)
                    {
                        dirX = Input.GetAxisRaw("Horizontal");
                        dirY = -Input.GetAxisRaw("Vertical");
                    }

                    float moveX = dirX * hsp;

                    // Block movement into a barrier the player is pressed against
                    if (barrierContactDir == 1 && moveX > 0) moveX = 0f;
                    if (barrierContactDir == -1 && moveX < 0) moveX = 0f;
                    rb.velocity = new Vector2(moveX, rb.velocity.y);

                    // Jump
                    if (Input.GetKeyDown("space") && Grounded() && !shoot && !stopMove)
                    {
                        AudioClip[] clips = { sndJump, sndJump2 };
                        int index = UnityEngine.Random.Range(0, clips.Length + 1);
                        if (index < clips.Length) audioSrc.PlayOneShot(clips[index]);
                        rb.velocity = new Vector2(rb.velocity.x, jspd);
                    }

                    // Swing
                    if (Input.GetKeyDown("space") && !Grounded() && !shoot && !stopMove)
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

                        foreach (Collider2D hit in hits)
                        {
                            bool isSwingPoint = ((1 << hit.gameObject.layer) & swingPoint) != 0;
                            Vector2 point = isSwingPoint ? (Vector2)hit.transform.position : hit.ClosestPoint(searchOrigin);

                            if (point.y <= playerPos.y) continue;
                            if (dirX > 0 && point.x <= playerPos.x) continue;
                            if (dirX < 0 && point.x >= playerPos.x) continue;

                            float dist = Vector2.Distance(playerPos, point);
                            bool shouldReplace = !found || (isSwingPoint && !bestIsSwingPoint) || (isSwingPoint == bestIsSwingPoint && dist < closestDistance);

                            if (shouldReplace)
                            {
                                closestDistance = dist;
                                bestAttachPoint = point;
                                bestIsSwingPoint = isSwingPoint;
                                swingPointSelected = bestIsSwingPoint;
                                found = true;
                            }
                        }

                        if (found)
                        {
                            rb.gravityScale = 0;
                            grappleX = bestAttachPoint.x;
                            grappleY = bestAttachPoint.y;
                            ropeX = transform.position.x;
                            ropeY = transform.position.y;
                            ropeAngleVelocity = 0f;
                            ropeAngle = Mathf.Atan2(ropeY - grappleY, ropeX - grappleX) * Mathf.Rad2Deg;
                            ropeLength = Vector2.Distance(new Vector2(grappleX, grappleY), new Vector2(ropeX, ropeY));
                            coll.size = new Vector2(1.339648f, 1.561783f);
                            coll.offset = new Vector2(-0.6135812f, -0.6907219f);
                            AudioClip[] clips = { sndSwing, sndSwing2, sndSwing3 };
                            audioSrc.PlayOneShot(clips[UnityEngine.Random.Range(0, clips.Length)]);
                            alarm1 = 400;
                            swingEnd = false;
                            pState = PlayerState.swing;
                        }
                    }

                    if (CanStartCrawling())
                    {
                        pState = PlayerState.crawl;
                        rb.gravityScale = 0;
                    }

                    // Quick zip to the nearest exposed tile corner
                    if (Input.GetKeyDown(KeyCode.I) && !stopMove)
                    {
                        if (bestCorner.HasValue)
                        {
                            moveTarget = bestCorner.Value;
                            coll.size = new Vector2(0.7719507f, 1.863027f);
                            coll.offset = new Vector2(-0.3766563f, -0.968719f);
                            AudioClip[] clips = { sndSwing, sndSwing2, sndSwing3 };
                            audioSrc.PlayOneShot(clips[UnityEngine.Random.Range(0, clips.Length)]);

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
                    if (Input.GetKey(KeyCode.U) && !stopMove)
                    {
                        rb.velocity = new Vector2(0f, rb.velocity.y);
                        shoot = true;

                        if (Grounded())
                        {
                            if ((dirX != 0) && dirY >= 0) anim.Play("Player_Ground_Shoot", 0, 0.33f);
                            else if ((dirX != 0) && dirY < 0) anim.Play("Player_Ground_Shoot", 0, 0.66f);
                            else if (dirX == 0 && dirY < 0) anim.Play("Player_Ground_Shoot", 0, 0.99f);
                        }
                        else
                        {
                            if ((dirX != 0) && dirY >= 0) anim.Play("Player_Air_Shoot", 0, 0.33f);
                            else if ((dirX != 0) && dirY < 0) anim.Play("Player_Air_Shoot", 0, 0.66f);
                            else if (dirX == 0 && dirY < 0) anim.Play("Player_Air_Shoot", 0, 0.99f);
                        }

                        Vector2 playerPos = transform.position;
                        Vector2 exactDir = new Vector2(dirX, -dirY).normalized;
                        float maxZipRange = 12f;

                        RaycastHit2D rayHit = Physics2D.Raycast(playerPos, exactDir, maxZipRange, jumpableGround);
                        Vector2 bestAttachPoint = Vector2.zero;
                        bool found = false;

                        if (rayHit.collider != null)
                        {
                            bestAttachPoint = rayHit.point;
                            found = true;
                        }

                        if (found && Input.GetKeyDown("space"))
                        {
                            rb.gravityScale = 0;
                            moveTarget = bestAttachPoint;
                            coll.size = new Vector2(0.7719507f, 1.863027f);
                            coll.offset = new Vector2(-0.3766563f, -0.968719f);
                            Vector2 zipNudgeDir = (moveTarget.Value - (Vector2)transform.position).normalized;
                            TeleportRigidbody((Vector2)transform.position + zipNudgeDir * 0.3f);
                            AudioClip[] clips = { sndSwing, sndSwing2, sndSwing3 };
                            audioSrc.PlayOneShot(clips[UnityEngine.Random.Range(0, clips.Length)]);

                            pState = PlayerState.quickzip;
                            zipTravelDist = 0f;
                        }
                    }
                    else
                    {
                        if (shoot && !stopMove)
                        {
                            if (Input.GetKeyUp(KeyCode.U) && (anim.GetCurrentAnimatorStateInfo(0).IsName("Player_Ground_Shoot") || anim.GetCurrentAnimatorStateInfo(0).IsName("Player_Air_Shoot")))
                            {
                                float hRaw = Input.GetAxisRaw("Horizontal");
                                float vRaw = Input.GetAxisRaw("Vertical");
                                Quaternion rot = transform.rotation;

                                if (hRaw > 0 && vRaw == 0) rot = transform.rotation;
                                else if (hRaw > 0 && vRaw > 0) rot = transform.rotation * Quaternion.Euler(0f, 0f, 45f);
                                else if (hRaw == 0 && vRaw > 0) rot = transform.rotation * Quaternion.Euler(0f, 0f, 90f);
                                else if (hRaw < 0 && vRaw > 0) rot = transform.rotation * Quaternion.Euler(0f, 0f, 135f);
                                else if (hRaw < 0 && vRaw == 0) rot = transform.rotation * Quaternion.Euler(0f, 0f, 180f);
                                else rot = sprite.flipX ? transform.rotation * Quaternion.Euler(0f, 0f, 180f) : transform.rotation;

                                Instantiate(webPrefab, transform.position, rot);
                                audioSrc.PlayOneShot(sndWebShoot);
                            }

                            shoot = false;
                        }
                    }

                    // Landing sound
                    if (!wasGrounded && Grounded() && pState == PlayerState.normal)
                    {
                        if (rb.velocity.y < -10f)
                        {
                            AudioClip[] clips = { sndHardLand, sndHardLand2 };
                            audioSrc.PlayOneShot(clips[UnityEngine.Random.Range(0, clips.Length)]);
                        }
                        else
                        {
                            AudioClip[] clips = { sndLand, sndLand2 };
                            audioSrc.PlayOneShot(clips[UnityEngine.Random.Range(0, clips.Length)]);
                        }
                    }
                    wasGrounded = Grounded();

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
                    currentCombatTarget = ResolveCombatTarget(origin, facingLeft, closestEnemy, closestEDistance);

                    // Attack
                    if (Input.GetKey(KeyCode.O) && currentCombatTarget != null && !stopMove && attackCooldown <= 0f)
                    {
                        StartAttackTowardTarget(CombatTargetTransform, attacking: true);
                    }
                    else if (Input.GetKey(KeyCode.O) && currentCombatTarget == null && !stopMove && attackCooldown <= 0f)
                    {
                        StartAttackTowardTarget(null, attacking: false);
                    }

                    // Uppercut
                    if (Input.GetKey(KeyCode.L) && Grounded() && !stopMove)
                    {
                        bool targetClose = currentCombatTarget != null && Mathf.Abs(CombatTargetTransform.position.x - origin.x) <= 1f;
                        dash_spd = targetClose ? CalcDashSpeed(CombatTargetTransform) : 0f;
                        attacking = targetClose;
                        uppercut = targetClose;
                        pState = PlayerState.dashenemy;
                        anim.speed = 2f;
                        PlayAttackAnimation(MovementState.uppercut);
                        rb.gravityScale = targetClose ? 0 : 1;
                        PlayAttackSounds();
                    }

                    // Counter
                    if (Input.GetKey(KeyCode.P) && Grounded() && !stopMove)
                    {
                        if (currentCounter != null)
                        {
                            dash_spd = CalcDashSpeed(currentCounter.transform, isCounter: true);
                            countering = true;
                            currentCounter.anim.speed = 0f;
                            pState = PlayerState.dashenemy;
                            sprite.flipX = currentCounter.transform.position.x < transform.position.x;
                            anim.speed = 2f;
                            anim.SetInteger("mstate", (int)PickCounterAnimation());
                            rb.gravityScale = 0;
                            PlayAttackSounds();
                        }
                        else
                        {
                            dash_spd = 0f;
                            countering = false;
                            pState = PlayerState.dashenemy;
                            anim.speed = 1.5f;
                            anim.SetInteger("mstate", (int)PickCounterAnimation());
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
                        grappleX = swingPointObj.transform.position.x;
                        grappleY = swingPointObj.transform.position.y;
                    }

                    float ropeAngleAcceleration = accelerationRate * Mathf.Cos(ropeAngle * Mathf.Deg2Rad);
                    dirX = Input.GetAxisRaw("Horizontal");
                    dirY = -Input.GetAxisRaw("Vertical");
                    ropeAngleAcceleration += dirX * 0.04f;
                    ropeLength += dirY * 0.01f;
                    ropeLength = Mathf.Max(ropeLength, 0f);
                    ropeAngleVelocity += ropeAngleAcceleration;
                    ropeAngle += ropeAngleVelocity;
                    ropeAngleVelocity *= 0.99f;
                    ropeX = grappleX + Mathf.Cos(ropeAngle * Mathf.Deg2Rad) * ropeLength;
                    ropeY = grappleY + Mathf.Sin(ropeAngle * Mathf.Deg2Rad) * ropeLength;

                    if (swingKickCooldown > 0f) swingKickCooldown -= Time.deltaTime;

                    // Trigger a swing kick once angular speed is high enough to know we're actually swinging, not idling
                    if (!swingKickTriggered && swingKickCooldown <= 0f && Mathf.Abs(ropeAngleVelocity) >= (0.65f * SwingKickVelocityThreshold))
                    {
                        List<Component> arcEnemies = ScanSwingArc();

                        if (arcEnemies.Count > 0)
                        {
                            swingKickTargets = arcEnemies;
                            swingKickTriggered = true;

                            if (!anim.GetCurrentAnimatorStateInfo(0).IsName("Player_Swing_Kick"))
                            {
                                AudioClip[] clips = { sndSwipe, sndSwipe2, sndSwipe3 };
                                audioSrc.PlayOneShot(clips[UnityEngine.Random.Range(0, clips.Length)]);
                            }

                            anim.Play("Player_Swing_Kick", 0, 0f);
                        }
                    }

                    // Reset kick state once the animation finishes
                    if (swingKickTriggered)
                    {
                        AnimatorStateInfo kickState = anim.GetCurrentAnimatorStateInfo(0);

                        if (kickState.IsName("Player_Swing_Kick") && kickState.normalizedTime >= 1f)
                        {
                            swingKickTriggered = false;
                            swingKickTargets.Clear();
                            swingKickCooldown = 0.4f;
                        }
                    }

                    Vector2 ropeDirection = new Vector2(ropeX - grappleX, ropeY - grappleY).normalized;
                    float ropeAngleDeg = Mathf.Atan2(ropeDirection.y, ropeDirection.x) * Mathf.Rad2Deg;
                    visual.rotation = Quaternion.Euler(0, 0, ropeAngleDeg + 90);

                    if (Input.GetKeyUp("space"))
                    {
                        rb.velocity = new Vector2(rb.velocity.x, jspd);
                        rb.gravityScale = 1;
                        anim.SetInteger("mstate", (int)MovementState.endswing);
                        coll.size = new Vector2(0.8397379f, 1.615343f);
                        coll.offset = new Vector2(-0.03511286f, -0.03012538f);
                        audioSrc.PlayOneShot(sndWebRelease);
                        swingPointSelected = false;
                        pState = PlayerState.normal;
                        swingEnd = true;
                        ReturnAllRopeSegmentsToPool();
                        swingKickTriggered = false;
                        swingKickTargets.Clear();
                        swingKickCooldown = 0f;
                    }

                    float dirOff = sprite.flipX ? -1f : 1f;
                    bool nearWall = Physics2D.Raycast(new Vector2(wallPositionChecker.position.x - 0.315f, wallPositionChecker.position.y - 0.372f), transform.right * dirX, wallCheckDistance, jumpableGround);
                    bool nearCeiling = Physics2D.Raycast(new Vector2(ceilingPositionChecker.position.x - 0.53f, ceilingPositionChecker.position.y - 0.68f), transform.up, ceilingCheckDistance, jumpableGround);
                    bool onGround = Grounded();

                    if (onGround)
                    {
                        visual.rotation = Quaternion.Euler(0, 0, 0);
                        coll.size = new Vector2(0.8397379f, 1.615343f);
                        coll.offset = new Vector2(-0.03511286f, -0.03012538f);
                        audioSrc.PlayOneShot(sndWebSnap);
                        swingPointSelected = false;
                        pState = PlayerState.normal;
                        ReturnAllRopeSegmentsToPool();
                        rb.gravityScale = 1;
                        swingKickTriggered = false;
                        swingKickTargets.Clear();
                        swingKickCooldown = 0f;
                    }
                    else if (nearWall && dirOff > 0)
                    {
                        hasTurn = false;
                        visual.rotation = Quaternion.Euler(0, 0, 0);
                        StartCoroutine(RotateAroundCorner(new Vector3(-0.1f, 0.1f, 0), 90f, 4));
                    }
                    else if (nearWall && dirOff < 0)
                    {
                        hasTurn = false;
                        visual.rotation = Quaternion.Euler(0, 0, 0);
                        StartCoroutine(RotateAroundCorner(new Vector3(0.1f, 0.1f, 0), -90f, 2));
                    }
                    else if (nearCeiling)
                    {
                        hasTurn = false;
                        visual.rotation = Quaternion.Euler(0, 0, 0);
                        StartCoroutine(RotateAroundCorner(new Vector3(0f, 0.15f, 0), 180f, 3));
                    }

                    if (nearWall || nearCeiling)
                    {
                        coll.size = new Vector2(0.8397379f, 1.615343f);
                        coll.offset = new Vector2(-0.03511286f, -0.03012538f);
                        audioSrc.PlayOneShot(sndWebSnap);
                        swingPointSelected = false;
                        pState = PlayerState.crawl;
                        ReturnAllRopeSegmentsToPool();
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
                    wasGrounded = true;
                    dirX = Input.GetAxisRaw("Horizontal");

                    float rawInput = (direction == 1 || direction == 3) ? Input.GetAxisRaw("Horizontal") : Input.GetAxisRaw("Vertical");

                    Vector2 worldAxis = (direction == 1 || direction == 3) ? Vector2.right : Vector2.up;

                    float rightAlignment = Vector2.Dot(transform.right, worldAxis);
                    float crawlSign = rightAlignment >= 0f ? 1f : -1f;

                    crawlDir = rawInput * crawlSign * 2.75f;

                    Vector2 crawlVel = transform.right * crawlDir;
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
                                ZaxisAdd += 90;

                                switch (direction)
                                {
                                    case 1: StartCoroutine(RotateAroundCornerInner(new Vector3(-0.1f, 0.1f, 0), 90f, 4)); break;
                                    case 2: StartCoroutine(RotateAroundCornerInner(new Vector3(0.3f, 0.1f, 0), 90f, 1)); break;
                                    case 3: StartCoroutine(RotateAroundCornerInner(new Vector3(0.3f, -0.3f, 0), 90f, 2)); break;
                                    case 4: StartCoroutine(RotateAroundCornerInner(new Vector3(-0.3f, -0.3f, 0), 90f, 3)); break;
                                }
                            }
                            else
                            {
                                ZaxisAdd -= 90;

                                switch (direction)
                                {
                                    case 1: StartCoroutine(RotateAroundCornerInner(new Vector3(0.1f, 0.1f, 0), -90f, 2)); break;
                                    case 2: StartCoroutine(RotateAroundCornerInner(new Vector3(-0.3f, -0.1f, 0), -90f, 3)); break;
                                    case 3: StartCoroutine(RotateAroundCornerInner(new Vector3(-0.3f, 0.3f, 0), -90f, 4)); break;
                                    case 4: StartCoroutine(RotateAroundCornerInner(new Vector3(-0.1f, 0.1f, 0), -90f, 1)); break;
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
                    if (Input.GetKeyDown(KeyCode.I) && !stopMove && bestCorner.HasValue)
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

                        AudioClip[] clips = { sndSwing, sndSwing2, sndSwing3 };
                        audioSrc.PlayOneShot(clips[UnityEngine.Random.Range(0, clips.Length)]);

                        shoot = false;
                        anim.speed = 1f;
                        zipFromCrawlGrace = 0.3f;
                        pState = PlayerState.quickzip;
                        zipTravelDist = 0f;
                        rb.gravityScale = 0;
                    }

                    if (pState == PlayerState.quickzip) break;

                    if (Input.GetKeyDown("space"))
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
                    if (Input.GetKey(KeyCode.U) && !stopMove)
                    {
                        rb.velocity = Vector2.zero;
                        shoot = true;

                        // Build an aim direction in surface local space
                        float hRaw = Input.GetAxisRaw("Horizontal");
                        float vRaw = Input.GetAxisRaw("Vertical");

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

                        if (logicalH != 0 && logicalAway == 0) anim.Play("Player_Crawl_Shoot", 0, 0.33f);
                        else if (logicalH != 0 && logicalAway > 0) anim.Play("Player_Crawl_Shoot", 0, 0.66f);
                        else if (logicalH == 0 && logicalAway > 0) anim.Play("Player_Crawl_Shoot", 0, 0.99f);
                        else anim.Play("Player_Crawl_Shoot", 0, 0.33f);

                        float maxZipRange = 12f;
                        RaycastHit2D crawlRayHit = Physics2D.Raycast(rb.position, surfaceAimDir, maxZipRange, jumpableGround);

                        if (crawlRayHit.collider != null && Input.GetKeyDown("space"))
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

                            AudioClip[] clips = { sndSwing, sndSwing2, sndSwing3 };
                            audioSrc.PlayOneShot(clips[UnityEngine.Random.Range(0, clips.Length)]);

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
                            if (Input.GetKeyUp(KeyCode.U) && anim.GetCurrentAnimatorStateInfo(0).IsName("Player_Crawl_Shoot"))
                            {
                                float angle = Mathf.Atan2(crawlShootAimDir.y, crawlShootAimDir.x) * Mathf.Rad2Deg;
                                Quaternion rot = Quaternion.Euler(0f, 0f, angle - 90f);

                                float facingSign = sprite.flipX ? -1f : 1f;
                                AnimatorStateInfo crawlShootInfo = anim.GetCurrentAnimatorStateInfo(0);
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

                                audioSrc.PlayOneShot(sndWebShoot);
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
                                ReturnAllRopeSegmentsToPool();
                                transform.rotation = Quaternion.Euler(0f, 0f, 180f);
                                TeleportRigidbody(ceilingEarly.point - Vector2.up * GetGroundOffsetMagnitude());
                                direction = 3;
                                pState = PlayerState.crawl;
                                crawlEntryGrace = 0.1f;
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
                            ReturnAllRopeSegmentsToPool();

                            float surfaceAngle = Mathf.Atan2(wallBlock.normal.y, wallBlock.normal.x) * Mathf.Rad2Deg;
                            transform.rotation = Quaternion.Euler(0f, 0f, surfaceAngle - 90f);

                            direction = GetDirectionFromRotation();
                            SnapRotationToDirection();

                            TeleportRigidbody(wallBlock.point + wallBlock.normal * GetGroundOffsetMagnitude());

                            pState = PlayerState.crawl;
                            crawlEntryGrace = 0.1f;
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
                                ReturnAllRopeSegmentsToPool();

                                float surfaceAngle = Mathf.Atan2(groundedMidZip.normal.y, groundedMidZip.normal.x) * Mathf.Rad2Deg;
                                transform.rotation = Quaternion.Euler(0f, 0f, surfaceAngle - 90f);

                                // Ceiling undersides have a downward facing normal, so flip it to snap toward the surface
                                Vector2 snapNormal = groundedMidZip.normal.y < 0f ? -groundedMidZip.normal : groundedMidZip.normal;
                                TeleportRigidbody(groundedMidZip.point + snapNormal * GetGroundOffsetMagnitude());

                                direction = GetDirectionFromRotation();
                                SnapRotationToDirection();
                                pState = PlayerState.crawl;
                                crawlEntryGrace = 0.1f;
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
                                ReturnAllRopeSegmentsToPool();

                                // Rotate to ceiling crawl orientation and snap up toward the surface
                                transform.rotation = Quaternion.Euler(0f, 0f, 180f);
                                TeleportRigidbody(ceilingMidZip.point - Vector2.up * GetGroundOffsetMagnitude());
                                direction = 3;
                                pState = PlayerState.crawl;
                                crawlEntryGrace = 0.1f;
                                rb.gravityScale = 0f;
                                break;
                            }
                        }

                        if (distToTarget <= stopDist)
                        {
                            moveTarget = null;

                            coll.size = new Vector2(0.8397379f, 1.615343f);
                            coll.offset = new Vector2(-0.03511286f, -0.03012538f);
                            ReturnAllRopeSegmentsToPool();

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
                                hasTurn = false;
                                freezeRotation = true;
                                float surfaceAngle = Mathf.Atan2(surfHit.normal.y, surfHit.normal.x) * Mathf.Rad2Deg;
                                transform.rotation = Quaternion.Euler(0f, 0f, surfaceAngle - 90f);
                                moveTarget = null;
                                coll.size = new Vector2(0.8397379f, 1.615343f);
                                coll.offset = new Vector2(-0.03511286f, -0.03012538f);
                                coll.enabled = true;
                                collCircle.enabled = false;
                                ReturnAllRopeSegmentsToPool();
                                rb.gravityScale = 0;

                                direction = GetDirectionFromRotation();
                                SnapRotationToDirection();
                                pState = PlayerState.crawl;
                                crawlEntryGrace = 0.1f;
                            }
                        }
                    }
                }
                break;




            case PlayerState.dashenemy:
                {
                    // If we lost the target mid attack, stop attacking
                    if (attacking && currentCombatTarget == null)
                        attacking = false;

                    if (waitingToHit && currentCombatTarget == null && !countering)
                    {
                        waitingToHit = false;
                        anim.speed = 1f;
                        rb.gravityScale = 1f;
                    }

                    if (waitingToHit && countering && currentCounter == null)
                    {
                        waitingToHit = false;
                        anim.speed = 1f;
                        rb.gravityScale = 1f;
                    }

                    if (attacking)
                    {
                        if (currentCombatTarget != null)
                            Attacking(currentCombatTarget.gameObject);

                        if (pastHitEvent)
                        {
                            // Re-evaluate targets after landing a hit
                            bool facingLeft = sprite.flipX;

                            if (Input.GetAxisRaw("Horizontal") > 0) facingLeft = false;
                            else if (Input.GetAxisRaw("Horizontal") < 0) facingLeft = true;

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
                            currentCombatTarget = ResolveCombatTarget(origin, facingLeft, closestEnemy2, closestEDistance2);

                            // Re-evaluate counter
                            float ceDist2 = Mathf.Infinity;
                            RobotStep ctr2 = null;

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

                            currentCounter = ctr2;

                            // Next attack input
                            if (Input.GetKey(KeyCode.O) && currentCombatTarget != null && attackCooldown <= 0f)
                            {
                                dash_spd = CalcDashSpeed(CombatTargetTransform);
                                attacking = true;
                                pState = PlayerState.dashenemy;
                                anim.speed = 2f;
                                PlayAttackAnimation(PickAttackAnimation());
                                rb.gravityScale = 0;
                                pastHitEvent = false;
                            }
                            else if (Input.GetKey(KeyCode.O) && currentCombatTarget == null && attackCooldown <= 0f)
                            {
                                dash_spd = 0f;
                                attacking = false;
                                pState = PlayerState.dashenemy;
                                anim.speed = 1.5f;
                                PlayAttackAnimation(PickAttackAnimation());
                                pastHitEvent = false;
                            }

                            if (Input.GetKey(KeyCode.L) && Grounded())
                            {
                                bool targetClose = currentCombatTarget != null && Mathf.Abs(CombatTargetTransform.position.x - origin.x) <= 1f;
                                dash_spd = targetClose ? CalcDashSpeed(CombatTargetTransform) : 0f;
                                attacking = targetClose;
                                uppercut = targetClose;
                                pState = PlayerState.dashenemy;
                                anim.speed = 2f;
                                PlayAttackAnimation(MovementState.uppercut);
                                rb.gravityScale = targetClose ? 0 : 1;
                                pastHitEvent = false;
                            }

                            if (Input.GetKey(KeyCode.P) && Grounded())
                            {
                                if (currentCounter != null)
                                {
                                    dash_spd = CalcDashSpeed(currentCounter.transform, isCounter: true);
                                    countering = true;
                                    currentCounter.anim.speed = 0f;
                                    pState = PlayerState.dashenemy;
                                    sprite.flipX = currentCounter.transform.position.x < transform.position.x;
                                    anim.speed = 2f;
                                    anim.SetInteger("mstate", (int)PickCounterAnimation());
                                    rb.gravityScale = 0;
                                    pastHitEvent = false;
                                }
                                else
                                {
                                    dash_spd = 0f;
                                    countering = false;
                                    pState = PlayerState.dashenemy;
                                    anim.speed = 1.5f;
                                    anim.SetInteger("mstate", (int)PickCounterAnimation());
                                    pastHitEvent = false;
                                }
                            }
                        }
                    }
                    else if (countering)
                    {
                        currentCounter.rb.velocity = new Vector2(0f, currentCounter.rb.velocity.y);
                        rb.velocity = Vector2.zero;

                        float counterDist = Mathf.Abs(currentCounter.transform.position.x - transform.position.x);

                        if (counterDist >= 0.45f)
                        {
                            attackTimeoutTimer += Time.deltaTime;

                            if (attackTimeoutTimer > 1.2f)
                            {
                                pastHitEvent = false;
                                pState = PlayerState.normal;
                                postAttackBuffer = 0.1f;
                                countering = false;
                                uppercut = false;
                                waitingToHit = false;
                                currentCounter.anim.speed = 1f;
                                currentCounter.rb.gravityScale = 1;
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
                        currentCounter.alarm4 = r == 0 ? 300 : r == 1 ? 400 : 500;
                        currentCounter.kick = false;
                        currentCounter.rb.gravityScale = 1;

                        if (Mathf.Abs(currentCounter.transform.position.x - transform.position.x) >= 0.45f && !waitingToHit && IsCounterMoveWindow(stateInfo))
                        {
                            float step = dash_spd * Time.deltaTime;
                            transform.position = Vector2.MoveTowards(transform.position, currentCounter.transform.position, step);
                        }

                        if (waitingToHit)
                        {
                            float dist = Mathf.Abs(currentCounter.transform.position.x - transform.position.x);

                            if (dist < 0.45f)
                            {
                                anim.speed = 1;
                                waitingToHit = false;
                            }
                            else
                            {
                                float step = dash_spd * Time.deltaTime;
                                transform.position = Vector2.MoveTowards(transform.position, currentCounter.transform.position, step);
                            }
                        }

                        if (stateInfo.normalizedTime >= 1f)
                        {
                            pastHitEvent = false;
                            pState = PlayerState.normal;
                            postAttackBuffer = 0.1f;
                            countering = false;
                            uppercut = false;
                            currentCounter.rb.gravityScale = 1;
                            currentCounter.hsp = 1f;
                            rb.gravityScale = 1;
                        }
                    }
                    else
                    {
                        rb.gravityScale = 1f;
                        if (Grounded()) rb.velocity = Vector2.zero;

                        if (stateInfo.normalizedTime >= 1f)
                        {
                            pastHitEvent = false;
                            pState = PlayerState.normal;
                            postAttackBuffer = 0.1f;
                            countering = false;
                            anim.speed = 1;
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
                        if (stateInfo.normalizedTime >= 1f)
                            anim.speed = 0f;

                        if (Grounded())
                        {
                            anim.speed = 1f;
                            pState = PlayerState.normal;
                        }
                    }
                    else
                    {
                        anim.speed = 1f;

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
            DrawRope(new Vector2(grappleX, grappleY), rb.position);

        if (pState == PlayerState.quickzip && moveTarget.HasValue)
            DrawRope(moveTarget.Value, new Vector2(transform.position.x, transform.position.y));

        UpdateAnimationState();
    }

    // Samples points along the rope's upcoming swing arc and collects every enemy found along the way
    private List<Component> ScanSwingArc()
    {
        int steps = 6;
        float scanDeg = 15f * Mathf.Sign(ropeAngleVelocity); // scan in the direction of travel
        float scanRadius = 0.55f;

        var seen = new HashSet<Component>();
        var results = new List<Component>();

        for (int i = 1; i <= steps; i++)
        {
            float futureAngleDeg = ropeAngle + scanDeg * (i / (float)steps);
            Vector2 samplePos = new Vector2(grappleX + Mathf.Cos(futureAngleDeg * Mathf.Deg2Rad) * ropeLength, grappleY + Mathf.Sin(futureAngleDeg * Mathf.Deg2Rad) * ropeLength);

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

    // Animation event on the swing kick clip's impact frame, hits every enemy collected by ScanSwingArc
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

            switch (target)
            {
                case RobotStep rb2: OnHit.Invoke(rb2); break;
                case GoblinStep g: OnHitG.Invoke(g); break;
                case ShockerStep s: OnHitS.Invoke(s); break;
            }

            combo++;
            alarm3 = 300;
            SpawnHitEffect(targetMB.transform.position);
        }

        uppercut = savedUppercut;
    }

    // Resolve which combat target to use: robot, shocker, or goblin
    private Component ResolveCombatTarget(Vector2 origin, bool facingLeft, RobotStep closestRobot, float closestRobotDist)
    {
        // Boss scene always targets goblin
        if (SceneManager.GetActiveScene().name == "Boss")
            return boss;

        // Mission scene compares the shocker against the closest robot
        if (SceneManager.GetActiveScene().name == "Mission" && shocker != null && shocker.sState != ShockerStep.ShockerState.death)
        {
            float shockerDx = shocker.transform.position.x - origin.x;
            bool inFront = !((facingLeft && shockerDx > 0) || (!facingLeft && shockerDx < 0));
            float shockerDist = Mathf.Abs(shockerDx);

            if (inFront && shockerDist <= 5.2f && shocker.sState != ShockerStep.ShockerState.chase)
            {
                // Prefer whichever is closer
                if (closestRobot == null || shockerDist < closestRobotDist)
                    return shocker;
            }
        }

        return closestRobot;
    }

    // Begin an attack toward the unified combat target
    private void StartAttackTowardTarget(Transform targetTransform, bool attacking)
    {
        rb.velocity = Vector2.zero;

        if (targetTransform != null)
            dash_spd = CalcDashSpeed(targetTransform);
        else
            dash_spd = 0f;

        this.attacking = attacking;
        attackCooldown = attackCooldownDuration;

        if (pState != PlayerState.dashenemy)
        {
            PlayAttackSounds();
            pState = PlayerState.dashenemy;
        }

        anim.speed = attacking ? 2f : 1.5f;
        PlayAttackAnimation(PickAttackAnimation());
        rb.gravityScale = attacking ? 0 : 1;
    }

    // Called by an enemy the moment it becomes untargetable
    public void ReleaseTargetIfCurrent(Component target)
    {
        if (currentCombatTarget == target)
            currentCombatTarget = null;

        if (currentTarget == (target as RobotStep))
            currentTarget = null;
    }

    // Check whether we're in the move window for a counter animation
    private bool IsCounterMoveWindow(AnimatorStateInfo si)
    {
        return (si.IsName("Player_Block1") && si.normalizedTime <= 0.28f)
            || (si.IsName("Player_Block2") && si.normalizedTime <= 0.30f)
            || (si.IsName("Player_Block3") && si.normalizedTime <= 0.32f)
            || (si.IsName("Player_Block4") && si.normalizedTime <= 0.38f);
    }

    // Check whether we're in the move window for an attack animation
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

    private void UpdateAnimationState()
    {
        if (postAttackBuffer > 0f) postAttackBuffer -= Time.deltaTime;

        if (pState == PlayerState.hurt) return;

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
        if (pState == PlayerState.dashenemy) return;

        // Don't override the swing kick animation while it's playing
        if (swingKickTriggered && anim.GetCurrentAnimatorStateInfo(0).IsName("Player_Swing_Kick")) return;

        MovementState mstate = MovementState.idle;

        if (pState == PlayerState.normal)
        {
            if (shoot)
            {
                anim.speed = 0f;
                mstate = Grounded() ? MovementState.groundshoot : MovementState.airshoot;
            }
            else
            {
                anim.speed = 1f;

                bool pushingIntoBarrier = (barrierContactDir == 1 && dirX > 0) || (barrierContactDir == -1 && dirX < 0);

                if (Grounded())
                {
                    mstate = (dirX != 0f && !pushingIntoBarrier) ? MovementState.running : MovementState.idle;
                }
                else if (postAttackBuffer > 0f)
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
            }
            else
            {
                mstate = MovementState.swinging;
            }

            anim.speed = 1f;
        }
        else if (pState == PlayerState.crawl)
        {
            if (shoot)
            {
                anim.speed = 0f;
                mstate = MovementState.crawlshoot;
            }
            else
            {
                mstate = MovementState.crawling;
                bool crawlPushingIntoBarrier = (barrierContactDir == 1 && crawlDir > 0) || (barrierContactDir == -1 && crawlDir < 0);
                anim.speed = (Mathf.Abs(crawlDir) > 0 && !crawlPushingIntoBarrier) ? 1f : 0f;
            }
        }
        else if (pState == PlayerState.quickzip)
        {
            mstate = MovementState.zip;
        }
        else if (pState == PlayerState.death)
        {
            anim.speed = 1f;
            mstate = MovementState.death;
        }

        AnimatorStateInfo si = anim.GetCurrentAnimatorStateInfo(0);
        float nt = si.normalizedTime % 1f;

        if (mstate == MovementState.running)
        {
            if (nt >= 0.35f && nt <= 0.38f) audioSrc.PlayOneShot(sndStep2);
            if (nt >= 0.83f && nt <= 0.86f) audioSrc.PlayOneShot(sndStep);
        }

        if (pState == PlayerState.crawl && Mathf.Abs(crawlDir) > 0)
        {
            if (nt >= 0.41f && nt <= 0.44f) audioSrc.PlayOneShot(sndCrawlStep);
            if (nt >= 0.82f && nt <= 0.85f) audioSrc.PlayOneShot(sndCrawlStep2);
        }

        if (mstate == MovementState.death)
        {
            if (nt >= 0.44f && nt <= 0.46f)
            {
                if (Grounded())
                    audioSrc.PlayOneShot(sndHardLand);

                if (!startAlarm2)
                {
                    alarm2 = 240;
                    startAlarm2 = true;
                }
            }

            if (nt >= 1f) anim.speed = 0f;
        }

        anim.SetInteger("mstate", (int)mstate);
    }

    public bool Grounded()
    {
        return Physics2D.BoxCast(coll.bounds.center, coll.bounds.size, 0f, Vector2.down, 0.1f, jumpableGround);
    }

    public bool IsPhysicallyPassable()
    {
        if (pState == PlayerState.dashenemy && (attacking || countering)) return true;
        if (pState == PlayerState.hurt) return true;
        if (pState == PlayerState.death) return true;
        return false;
    }

    // True physical distance from the player's pivot to the collider's bottom edge
    private float GetGroundOffsetMagnitude()
    {
        float colliderHalfHeight = coll.size.y * 0.5f;
        float colliderBottomLocalY = coll.offset.y - colliderHalfHeight;
        return Mathf.Abs(colliderBottomLocalY) * transform.localScale.y;
    }

    private void TeleportRigidbody(Vector2 newPos)
    {
        rb.interpolation = RigidbodyInterpolation2D.None;
        rb.position = newPos;
        StartCoroutine(RestoreInterpolationNextFixedUpdate());
    }

    public void BeginForcedQuickZip(Vector2 target)
    {
        moveTarget = target;
        zipTravelDist = 0f;
        Vector2 zipNudgeDir = (target - (Vector2)transform.position).normalized;
        TeleportRigidbody((Vector2)transform.position + zipNudgeDir * 0.3f);
        rb.gravityScale = 0;
        pState = PlayerState.quickzip;
    }

    private IEnumerator RestoreInterpolationNextFixedUpdate()
    {
        yield return new WaitForFixedUpdate();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void DrawRope(Vector2 start, Vector2 end)
    {
        ReturnAllRopeSegmentsToPool();
        Vector2 direction = (end - start).normalized;
        float distance = Vector2.Distance(start, end);
        int segmentCount = Mathf.CeilToInt(distance / ropeSegmentLength);

        for (int i = 0; i < segmentCount; i++)
        {
            Vector2 position = start + direction * ropeSegmentLength * i;
            GameObject seg = GetRopeSegment(position);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            seg.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    GameObject GetRopeSegment(Vector2 position)
    {
        GameObject segment;

        if (ropeSegmentPool.Count > 0)
        {
            segment = ropeSegmentPool.Dequeue();
            segment.SetActive(true);
        }
        else
        {
            segment = Instantiate(ropeSegmentPrefab);
        }

        segment.transform.position = position;
        ropeSegments.Add(segment);
        return segment;
    }

    public void ReturnAllRopeSegmentsToPool()
    {
        foreach (var seg in ropeSegments)
        {
            if (ropeSegmentPool.Count < maxPoolSize)
            {
                seg.SetActive(false);
                ropeSegmentPool.Enqueue(seg);
            }
            else
            {
                Destroy(seg);
            }
        }

        ropeSegments.Clear();
    }

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
    }

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
    }

    // Used only by swing, quickzip, and CanStartCrawling
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
    }

    private Vector2 RotateVector2(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(cos * v.x - sin * v.y, sin * v.x + cos * v.y);
    }

    private int GetNextDirection(int currentDir, int turnSign)
    {
        int[] cwCycle = { 1, 2, 3, 4 };
        int idx = System.Array.IndexOf(cwCycle, currentDir);
        int nextIdx = (idx + turnSign + 4) % 4;
        return cwCycle[nextIdx];
    }

    private int GetDirectionFromRotation()
    {
        float z = transform.eulerAngles.z % 360f;
        if (z < 0) z += 360f;
        if (z >= 315f || z < 45f) return 1;
        if (z >= 45f && z < 135f) return 4;
        if (z >= 135f && z < 225f) return 3;
        return 2;
    }

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
            hasTurn = false;
            StartCoroutine(RotateAroundCorner(new Vector3(-0.1f, 0.1f, 0), 90f, 4));
        }
        else if (nearWall && dirX < 0)
        {
            hasTurn = false;
            StartCoroutine(RotateAroundCorner(new Vector3(0.1f, 0.1f, 0), -90f, 2));
        }
        else if (nearCeiling)
        {
            hasTurn = false;
            StartCoroutine(RotateAroundCorner(new Vector3(0f, 0.15f, 0), 180f, 3));
        }

        return (dirY > 0 && onGround) || nearWall || nearCeiling;
    }

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


            float dist = Vector2.Distance(playerPos, corner);

            if (dist < closestDistance)
            {
                closestDistance = dist;
                bestCorner = corner;
                found = true;
            }
        }
    }

    bool IsExposedCorner(Vector2 corner, bool isRightCorner)
    {
        Vector2 dir = isRightCorner ? Vector2.right : Vector2.left;
        float sideOffset = tilemap.cellSize.x * 0.3f;
        return Physics2D.OverlapPoint(corner + dir * sideOffset, jumpableGround) == null;
    }

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

    BoundsInt GetCameraTileBounds(Tilemap tilemap, Camera cam)
    {
        Vector3 min = cam.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 max = cam.ViewportToWorldPoint(new Vector3(1, 1, 0));
        Vector3Int cellMin = tilemap.WorldToCell(min);
        Vector3Int cellMax = tilemap.WorldToCell(max);
        return new BoundsInt(cellMin.x - 2, cellMin.y - 2, 0, (cellMax.x - cellMin.x) + 4, (cellMax.y - cellMin.y) + 4, 1);
    }



    // combat methods
    private void Attacking(GameObject target)
    {
        Rigidbody2D rb_target = target.GetComponent<Rigidbody2D>();
        AnimatorStateInfo si = anim.GetCurrentAnimatorStateInfo(0);

        // Resolve grounded and engaged through the component type
        bool grounded = CombatTargetGrounded();
        bool targetEngaged = CombatTargetIsEngaged();

        // Stop the target if it's engaged
        if (targetEngaged && !pastHitEvent)
            rb_target.velocity = Vector2.zero;

        // Face the target
        sprite.flipX = target.transform.position.x <= transform.position.x;

        // Aerial attacks use 2D distance, ground attacks use x only
        float attackDist = grounded ? 0.45f : 0.2f;

        // Goblin on a glider needs a tighter distance
        if (target.TryGetComponent<GoblinStep>(out var g) && g.gState == GoblinStep.GoblinState.on_glider)
            attackDist = 0.15f;

        rb.gravityScale = 0;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0;

        float dist = grounded ? Mathf.Abs(target.transform.position.x - transform.position.x) : Vector2.Distance(transform.position, target.transform.position);

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
                attacking = false;
                waitingToHit = false;
                uppercut = false;
                anim.speed = 1f;
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

        if (dist >= attackDist && !waitingToHit && inMoveWindow)
        {
            float step = dash_spd * Time.deltaTime;
            float actualStep = grounded ? step : Mathf.Min(step, dist - 0.05f);

            if (actualStep > 0)
                transform.position = Vector2.MoveTowards(transform.position, target.transform.position, actualStep);
        }

        if (waitingToHit)
        {
            if (dist < attackDist)
            {
                anim.speed = 1;
                waitingToHit = false;
            }
            else
            {
                float step = dash_spd * Time.deltaTime;
                float actualStep = grounded ? step : Mathf.Min(step, dist - 0.05f);

                if (actualStep > 0)
                    transform.position = Vector2.MoveTowards(transform.position, target.transform.position, actualStep);
            }
        }

        if (si.normalizedTime >= 1f)
        {
            pastHitEvent = false;
            pState = PlayerState.normal;
            postAttackBuffer = 0.1f;
            attacking = false;
            uppercut = false;
            rb.gravityScale = 1;

            // Only restore target gravity if it was engaged
            if (targetEngaged)
                rb_target.gravityScale = 1;
        }
    }

    // Called from an animation event on the attack impact frame
    public void HitEvent()
    {
        if (currentCombatTarget == null) return;

        float groundDist = 0.45f;
        float airDist = 0.9f;

        bool landed = Grounded() ? Vector3.Distance(CombatTargetTransform.position, transform.position) <= groundDist : Vector3.Distance(CombatTargetTransform.position, transform.position) <= airDist;

        if (attacking && landed)
        {
            switch (currentCombatTarget)
            {
                case GoblinStep gb: OnHitG.Invoke(gb); break;
                case ShockerStep sh: OnHitS.Invoke(sh); break;
                case RobotStep rb2: OnHit.Invoke(rb2); break;
            }

            if (!pastHitEvent) pastHitEvent = true;
            combo++;
            alarm3 = 300;
        }

        if (countering && landed)
        {
            switch (currentCombatTarget)
            {
                case GoblinStep gb: OnHitG.Invoke(gb); break;
                case ShockerStep sh: OnHitS.Invoke(sh); break;
                case RobotStep rb2: OnHit.Invoke(rb2); break;
            }

            combo++;
            alarm3 = 300;
        }

        // Also hit currentCounter if countering a robot
        if (countering && currentCounter != null)
        {
            float cDist = Vector3.Distance(currentCounter.transform.position, transform.position);

            if (cDist <= (Grounded() ? groundDist : airDist))
            {
                OnHit.Invoke(currentCounter);
                combo++;
                alarm3 = 300;
            }
        }
    }

    public void PauseBeforeHit()
    {
        Animator tAnim = CombatTargetAnim;
        Rigidbody2D tRB = CombatTargetRB;

        if (attacking && tAnim != null)
        {
            anim.speed = 0;

            if (CombatTargetIsEngaged())
            {
                tAnim.speed = 0;
                tRB.velocity = Vector2.zero;
            }

            waitingToHit = true;
        }

        if (countering && currentCounter != null)
        {
            anim.speed = 0;
            currentCounter.anim.speed = 0;
            currentCounter.rb.velocity = Vector2.zero;
            waitingToHit = true;
        }
    }

    public void SpawnHitEffect(Vector2 impactPoint) { Instantiate(hitParticlePrefab, impactPoint, Quaternion.identity); }
    public void SpawnHurtEffect(Vector2 impactPoint) { Instantiate(hurtParticlePrefab, impactPoint, Quaternion.identity); }

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
    };

    private void PlayAttackAnimation(MovementState state)
    {
        anim.SetInteger("mstate", (int)state);

        if (AttackAnimNames.TryGetValue(state, out string clipName))
        {
            anim.Play(clipName, 0, 0f);
        }
    }


    // Damage methods, called by enemies
    public void Damage(RobotStep target)
    {
        if (pState == PlayerState.death) return;
        if (!countering) ApplyHurtFromEnemy(target.sprite.flipX, target.kick, target.transform.position);
        else currentCounter?.OnPlayerHit(currentCounter);
    }

    public void DamageGoblin(GoblinStep target)
    {
        if (pState == PlayerState.death) return;
        if (!countering) ApplyHurtFromEnemy(target.sprite.flipX, false, target.transform.position);
        else target.OnPlayerHit(target);
    }

    public void DamageShocker(ShockerStep target)
    {
        if (pState == PlayerState.death) return;
        if (!countering) ApplyHurtFromEnemy(target.sprite.flipX, target.kick, target.transform.position);
        else target.OnPlayerHit(target);
    }

    private void ApplyHurtFromEnemy(bool enemyFacingLeft, bool isKick, Vector3 enemyPos)
    {
        float dir = enemyFacingLeft ? -1f : 1f;
        dirX = dir;

        rb.velocity = isKick ? new Vector2(dir * 2f, 5f) : new Vector2(dir, 0f);
        anim.speed = 1f;
        combo = 0;
        pState = PlayerState.hurt;

        MovementState mstate;

        if (isKick)
        {
            mstate = MovementState.launched;
            AudioClip[] clips = { sndStrongHit, sndStrongHit2 };
            audioSrc.PlayOneShot(clips[UnityEngine.Random.Range(0, clips.Length)]);
        }
        else
        {
            mstate = UnityEngine.Random.Range(0, 2) == 0 ? MovementState.hurt1 : MovementState.hurt2;
            AudioClip[] clips = { sndQuickHit, sndQuickHit2 };
            audioSrc.PlayOneShot(clips[UnityEngine.Random.Range(0, clips.Length)]);
        }

        anim.SetInteger("mstate", (int)mstate);
        enemyHitSpawn = enemyPos;
        SpawnHurtEffect(enemyPos);

        if (health > 0)
        {
            health -= isKick ? 5 : 4;
            healthbar.UpdateHealthBar(health, maxHealth);
        }

        AudioClip[] hurtClips = { sndHurt, sndHurt2, sndHurt3 };
        audioSrc.PlayOneShot(hurtClips[UnityEngine.Random.Range(0, hurtClips.Length)]);
    }




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

    private void UpdateBarrierContact()
    {
        Bounds b = coll.bounds;
        Vector2 probeSize = new Vector2(b.size.x + 0.1f, b.size.y * 0.9f);
        Collider2D[] hits = Physics2D.OverlapBoxAll(b.center, probeSize, 0f);

        barrierContactDir = 0;

        foreach (var hit in hits)
        {
            if (hit.gameObject == this.gameObject) continue;
            if (!hit.CompareTag("Barrier")) continue;

            float dx = hit.transform.position.x - transform.position.x;
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
                barrierContactDir = 1;

            if (barrierContactDir == 0)
            {
                RaycastHit2D lHit = Physics2D.BoxCast(b.center, castSize, 0f, Vector2.left, castDist, enemyMask);

                if (lHit.collider != null && IsEnemySolid(lHit.collider))
                    barrierContactDir = -1;
            }
        }
    }

    private bool IsEnemySolid(Collider2D enemyColl)
    {
        IEnemyBarrier barrier = enemyColl.GetComponent<IEnemyBarrier>();
        return barrier != null && barrier.IsSolidToPlayer;
    }

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
            audioSrc.PlayOneShot(sndHealth);
            collision.GetComponent<Animator>().Play("HealthCollect");
            collision.GetComponent<SpriteRenderer>().material = noOutlineMaterial;
            Destroy(collision.gameObject, 0.1f);
        }

        if (pState != PlayerState.death && collision.gameObject.CompareTag("Arrow"))
        {
            audioSrc.PlayOneShot(sndLevelComplete);
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
                audioSrc.PlayOneShot(sndWarning);
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
            AnimatorStateInfo si = anim.GetCurrentAnimatorStateInfo(0);

            bool hittingToward = (transform.position.x > collision.transform.position.x && sprite.flipX) || (transform.position.x < collision.transform.position.x && !sprite.flipX);

            if (carNormal && pState == PlayerState.dashenemy && hittingToward && IsAttackMoveWindow(si))
            {
                rb.WakeUp();
                rb.position = rb.position;
                audioSrc.PlayOneShot(sndCarBreak);
                collision.GetComponent<Animator>().Play("CarBreak");
            }
        }

        if (collision.gameObject.CompareTag("Door"))
        {
            rb.WakeUp();
            AnimatorStateInfo si = anim.GetCurrentAnimatorStateInfo(0);

            if (collision.gameObject.GetComponent<BreakableDoor>().phase == 0 && pState == PlayerState.dashenemy && IsAttackMoveWindow(si))
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
            AnimatorStateInfo si = anim.GetCurrentAnimatorStateInfo(0);

            if (collision.gameObject.GetComponent<SwitchScript>().phase == 0 && pState == PlayerState.dashenemy && IsAttackMoveWindow(si))
            {
                rb.WakeUp();
                rb.position = rb.position;
                collision.gameObject.GetComponent<SwitchScript>().phase = 1;
            }
        }

        if (collision.gameObject.CompareTag("Explosive"))
        {
            rb.WakeUp();
            AnimatorStateInfo si = anim.GetCurrentAnimatorStateInfo(0);

            if (collision.gameObject.GetComponent<ExplosiveScript>().phase == 0 && pState == PlayerState.dashenemy && IsAttackMoveWindow(si))
            {
                rb.WakeUp();
                rb.position = rb.position;
                collision.gameObject.GetComponent<ExplosiveScript>().phase = 1;
            }
        }

        if (collision.gameObject.CompareTag("Generator"))
        {
            rb.WakeUp();
            AnimatorStateInfo si = anim.GetCurrentAnimatorStateInfo(0);

            if (collision.gameObject.GetComponent<GeneratorScript>().phase == 0 && pState == PlayerState.dashenemy && IsAttackMoveWindow(si))
            {
                rb.WakeUp();
                rb.position = rb.position;
                collision.gameObject.GetComponent<GeneratorScript>().phase = 1;
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
            rb.velocity = new Vector2(dir * 1.5f, 5f);

            anim.speed = 1f;
            combo = 0;
            pState = PlayerState.hurt;

            anim.SetInteger("mstate", (int)MovementState.launched);
            enemyHitSpawn = collision.transform.position;
            SpawnHurtEffect(transform.position);

            health -= 8;
            healthbar.UpdateHealthBar(health, maxHealth);

            AudioClip[] c2 = { sndStrongHit, sndStrongHit2 };
            audioSrc.PlayOneShot(c2[UnityEngine.Random.Range(0, c2.Length)]);
            AudioClip[] c = { sndHurt, sndHurt2, sndHurt3 };
            audioSrc.PlayOneShot(c[UnityEngine.Random.Range(0, c.Length)]);
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
            rb.velocity = new Vector2(dir * 2f, 5f);

            anim.speed = 1f;
            combo = 0;
            pState = PlayerState.hurt;

            MovementState mstate = UnityEngine.Random.Range(0, 2) == 0 ? MovementState.hurt1 : MovementState.hurt2;
            anim.SetInteger("mstate", (int)mstate);
            enemyHitSpawn = collision.transform.position;
            SpawnHurtEffect(transform.position);

            health -= 8;
            healthbar.UpdateHealthBar(health, maxHealth);

            AudioClip[] c2 = { sndQuickHit, sndQuickHit2 };
            audioSrc.PlayOneShot(c2[UnityEngine.Random.Range(0, c2.Length)]);
            AudioClip[] c = { sndHurt, sndHurt2, sndHurt3 };
            audioSrc.PlayOneShot(c[UnityEngine.Random.Range(0, c.Length)]);
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
            float dY = collision.transform.position.y > transform.position.y ? -0.7f : 1f;
            rb.velocity = new Vector2(dir * 2f, 5f * dY);

            anim.speed = 1f;
            combo = 0;
            pState = PlayerState.hurt;

            anim.SetInteger("mstate", (int)MovementState.launched);
            enemyHitSpawn = collision.transform.position;
            SpawnHurtEffect(transform.position);

            health -= 8;
            healthbar.UpdateHealthBar(health, maxHealth);

            AudioClip[] c2 = { sndStrongHit, sndStrongHit2 };
            audioSrc.PlayOneShot(c2[UnityEngine.Random.Range(0, c2.Length)]);
            AudioClip[] c = { sndHurt, sndHurt2, sndHurt3 };
            audioSrc.PlayOneShot(c[UnityEngine.Random.Range(0, c.Length)]);
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
            float dY = collision.transform.position.y > transform.position.y ? -0.7f : 1f;
            rb.velocity = new Vector2(dir * 2f, 5f * dY);
            anim.speed = 1f;

            combo = 0;
            pState = PlayerState.hurt;

            anim.SetInteger("mstate", (int)MovementState.launched);
            enemyHitSpawn = collision.transform.position;
            SpawnHurtEffect(transform.position);

            health -= 8;
            healthbar.UpdateHealthBar(health, maxHealth);

            AudioClip[] c2 = { sndStrongHit, sndStrongHit2 };
            audioSrc.PlayOneShot(c2[UnityEngine.Random.Range(0, c2.Length)]);
            AudioClip[] c = { sndHurt, sndHurt2, sndHurt3 };
            audioSrc.PlayOneShot(c[UnityEngine.Random.Range(0, c.Length)]);
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
            float dY = collision.transform.position.y > transform.position.y ? -0.7f : 1f;
            rb.velocity = new Vector2(dir * 2f, 5f * dY);

            anim.speed = 1f;
            combo = 0;
            pState = PlayerState.hurt;

            anim.SetInteger("mstate", (int)MovementState.launched);
            enemyHitSpawn = collision.transform.position;
            SpawnHurtEffect(transform.position);

            health -= 3;
            healthbar.UpdateHealthBar(health, maxHealth);

            AudioClip[] c2 = { sndStrongHit, sndStrongHit2 };
            audioSrc.PlayOneShot(c2[UnityEngine.Random.Range(0, c2.Length)]);
            AudioClip[] c = { sndHurt, sndHurt2, sndHurt3 };
            audioSrc.PlayOneShot(c[UnityEngine.Random.Range(0, c.Length)]);
        }
    }
}