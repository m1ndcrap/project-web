using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
using static GliderScript;
using static RobotStep;

public class GoblinStep : MonoBehaviour
{
    public Rigidbody2D rb;
    [SerializeField] public Animator anim;
    [SerializeField] public SpriteRenderer sprite;
    private BoxCollider2D coll;
    [SerializeField] private float dirX = 0f;
    [SerializeField] private bool setCustomStartingDir = false;
    [SerializeField] private LayerMask jumpableGround;
    [SerializeField] private LayerMask playerMask;
    [SerializeField] public float hsp = 1f; // Horizontal speed
    [SerializeField] private int waitTime = 120;
    private Camera cam;

    public enum MovementState { crouching, throwing, jump, idle, falling, hurt1, hurt2, sprinting, punch1, punch2, death, throwingprojectile }
    public enum GoblinState { on_glider, engaged, attack, getting_hit, web_hit, light_hit, on_glider_hit, heavy_hurt, launched, air_plummet, death, jump_to_platform, fight_glider, blocking, air_hit }
    public GoblinState gState;

    // Sound Files
    [SerializeField] private AudioSource audioSrc;
    [SerializeField] private AudioClip sndAttack;
    [SerializeField] private AudioClip sndAttack2;
    [SerializeField] private AudioClip sndHit;
    [SerializeField] private AudioClip sndHit2;
    [SerializeField] private AudioClip sndHit3;
    [SerializeField] private AudioClip sndLand;
    [SerializeField] private AudioClip sndStep;
    [SerializeField] private AudioClip sndGSpinner1;
    [SerializeField] private AudioClip sndGSpinner2;
    [SerializeField] private AudioClip sndGLaugh1;
    [SerializeField] private AudioClip sndGLaugh2;
    [SerializeField] private AudioClip sndGLaugh3;
    [SerializeField] private AudioClip sndGAction1;
    [SerializeField] private AudioClip sndGAction2;
    private AudioClip sndQuickHit;
    private AudioClip sndQuickHit2;
    private AudioClip sndStrongHit;
    private AudioClip sndStrongHit2;
    private bool wasGrounded = false;
    private bool hasPlayedStep1;
    private bool hasPlayedStep2;

    // Alarms
    private int alarm1;
    [SerializeField] private int alarm3 = 0;
    [SerializeField] public int alarm4 = 0;
    public int alarm5 = 0;
    [SerializeField] private int alarm6 = 0;
    private bool startAlarm6 = false;
    [SerializeField] private float distanceFromPlayer = 0f;
    public int alarm7 = 0;
    [SerializeField] private int alarm11 = 0;
    [SerializeField] private bool startAlarm11 = false;
    private int alarm12 = 0;

    // Combat
    private Material outline;
    [SerializeField] private PlayerStep player;
    [SerializeField] private bool noHitWall;
    [SerializeField] private bool shocked = false;
    public UnityEvent<PlayerStep> OnAttack;
    public bool kick = false;
    public bool attacking = false;
    public bool collidedWithPlayer = false;
    private bool backstep = false;
    [SerializeField] private GameObject hitParticlePrefab;

    // health bar
    public int health = 25;
    private int maxHealth = 25;
    HealthBar healthbar;

    public bool blocking = false;
    [SerializeField] private GliderScript glider;
    private int platDir = 0;
    [SerializeField] private bool throwing = false;
    [SerializeField] private bool threw = false;
    [SerializeField] private GameObject goblinBombPrefab;
    private bool canThrow = true;
    private bool spinners = false;
    private bool gliderActive = false;
    [SerializeField] private GameObject goblinSpinnerPrefab;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<BoxCollider2D>();
        gState = GoblinState.on_glider;
        if (!setCustomStartingDir) { dirX = UnityEngine.Random.Range(0, 2) == 0 ? 1 : -1; }
        alarm1 = waitTime;
        outline = sprite.material;
        player.OnHitG.AddListener((x) => OnPlayerHit(x));
        sndQuickHit = player.sndQuickHit;
        sndQuickHit2 = player.sndQuickHit2;
        sndStrongHit = player.sndStrongHit;
        sndStrongHit2 = player.sndStrongHit2;
        healthbar = GetComponentInChildren<HealthBar>();
        healthbar.UpdateHealthBar(health, maxHealth);
        cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if (glider.state != GState.GroundFight && glider.state != GState.AirFight && glider.state != GState.Zooming) { blocking = false; }
        if ((glider.state == GState.GroundFight || glider.state == GState.Zooming) && player.uppercut && Vector3.Distance(player.transform.position, transform.position) <= 1f) { blocking = false; }

        // Outline Shader Color Control
        if (gState == GoblinState.attack) { outline.color = Color.red; }
        else if (player.currentTarget == this) { outline.color = Color.white; }
        else { outline.color = Color.black; }

        collidedWithPlayer = Physics2D.Raycast(transform.position, transform.right * -dirX, 0.65f, playerMask);

        if (gState == GoblinState.getting_hit || gState == GoblinState.on_glider || player.pState == PlayerStep.PlayerState.dashenemy || gState == GoblinState.attack || (player.transform.position.y - transform.position.y > 0.015f && Grounded()))
            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Enemy"), LayerMask.NameToLayer("Player"), true);
        else
            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Enemy"), LayerMask.NameToLayer("Player"), false);

        if (gState == GoblinState.jump_to_platform)
            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Enemy"), LayerMask.NameToLayer("Ground"), true);
        else
            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Enemy"), LayerMask.NameToLayer("Ground"), false);

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        distanceFromPlayer = Vector3.Distance(player.transform.position, transform.position);
        noHitWall = !Physics2D.Raycast(transform.position, (player.transform.position - transform.position).normalized, distanceFromPlayer, jumpableGround);

        if (glider.state != GState.Throwing && glider.state != GState.AirFight && gState != GoblinState.engaged) { throwing = false; }
        if (glider.state == GState.AirFight && gState != GoblinState.on_glider && health > 0) { canThrow = true; gState = GoblinState.on_glider; }
        if (glider.state == GState.AirFight) { gliderActive = true; } else { gliderActive = false; }
        if (gliderActive && health == 0) { transform.position = new Vector2(glider.transform.position.x, glider.transform.position.y + 1.86f); rb.velocity = new Vector2(0f, 0f); }

        Vector2 start = transform.position;
        Vector2 end = player.transform.position;
        RaycastHit2D[] hits = Physics2D.LinecastAll(start, end);

        if (alarm4 > 0)
            alarm4 -= 4;
        else
        {
            if (gState == GoblinState.engaged)
            {
                if ((!player.isEnemyAttacking) && (Vector3.Distance(player.transform.position, transform.position) <= 2.05f) && ((!sprite.flipX && transform.position.x < player.transform.position.x) || (sprite.flipX && transform.position.x > player.transform.position.x)) && noHitWall)
                {
                    gState = GoblinState.attack;
                    AudioClip[] clips = { sndAttack, sndAttack2 };
                    int index = UnityEngine.Random.Range(0, clips.Length);
                    if (index < clips.Length) { audioSrc.PlayOneShot(clips[index]); }
                    rb.gravityScale = 0;
                    int hitIndex = UnityEngine.Random.Range(0, 3); // random number 0-6
                    MovementState mstate = MovementState.idle;

                    switch (hitIndex)
                    {
                        case 0: { mstate = MovementState.punch1; } break;
                        case 1: { mstate = MovementState.punch2; } break;
                    }

                    anim.SetInteger("mstate", (int)mstate);
                    player.isEnemyAttacking = true;
                }
                else
                {
                    int hitIndex = UnityEngine.Random.Range(0, 3);

                    switch (hitIndex)
                    {
                        case 0: { alarm4 = 300; } break;
                        case 1: { alarm4 = 400; } break;
                        case 2: { alarm4 = 500; } break;
                    }
                }
            }
        }

        if (startAlarm11)
        {
            if (alarm11 > 0)
                alarm11 -= 1;
            else
            {
                if (glider.state == GState.Throwing || gState == GoblinState.engaged || glider.state == GState.AirFight) { if (throwing) { threw = true; } }
                if (player.isEnemyAttacking) { player.isEnemyAttacking = false; }
                //if state == ggState.death { game_restart(); }
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

        switch (gState)
        {
            case GoblinState.on_glider:
            {
                Vector2 pos = glider.transform.position;
                transform.position = new Vector2(pos.x, pos.y + 0.54f);
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
                        if (!startAlarm11) { alarm11 = 60; startAlarm11 = true; }
                        throwing = true;
                    }

                    if (threw)
                    {
                        if (normalizedTime >= 0.5f && normalizedTime <= 0.53f && FindObjectsOfType<PumpkinProjectile>().Length == 0)
                        {
                            GameObject bomb = Instantiate(goblinBombPrefab, transform.position + Vector3.up * 0.48f, Quaternion.identity);
                            int dir = sprite.flipX ? -1 : 1;
                            bomb.GetComponent<PumpkinProjectile>().dir = dir;
                            bomb.GetComponent<PumpkinProjectile>().airborne = true;
                            AudioClip[] clips = { sndGAction1, sndGAction2 };
                            audioSrc.PlayOneShot(clips[UnityEngine.Random.Range(0, clips.Length)]);
                        }
                    }
                }

                if (glider.state == GState.AirFight)
                {
                    if (!throwing)
                    {
                        StartThrowCooldown();
                        throwing = true;
                    }

                    if (threw)
                    {
                        if (normalizedTime >= 0.5f && normalizedTime <= 0.53f && FindObjectsOfType<PumpkinSpinner>().Length < 3)
                        {
                            int dir = sprite.flipX ? -1 : 1;

                            SpawnSpinner(dir, RandomChoice(2, 4, 6, 8));
                            SpawnSpinner(dir, RandomChoice(4, 8, 12, 16));
                            SpawnSpinner(dir, RandomChoice(3, 6, 9, 12));

                            PlayGoblinActionSound();
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

                            switch (alarmIndex)
                            {
                                case 0: { alarm11 = 60; startAlarm11 = true; } break;
                                case 1: { alarm11 = 120; startAlarm11 = true; } break;
                                case 2: { alarm11 = 180; startAlarm11 = true; } break;
                            }
                        }

					    threw = false;
				    }
		        }
            }
            break;

            case GoblinState.jump_to_platform:
            {
                rb.velocity = new Vector2(0f, 0f);

	            if (platDir == -1)
	            {
		            if (Vector2.Distance(transform.position, new Vector2(-10.27f, 4.24f)) > 0.3f)
		            {
                        rb.gravityScale = 0;
                        transform.position = Vector2.MoveTowards(transform.position, new Vector2(-10.27f, 4.24f), 0.1f * Time.deltaTime * 60f);
		            }else{
                        rb.gravityScale = 1;
			            gState = GoblinState.engaged;
		            }
	            }else{
		            if (Vector2.Distance(transform.position, new Vector2(-1.27f, 4.24f)) > 0.3f)
		            {
                        rb.gravityScale = 0;
                        transform.position = Vector2.MoveTowards(transform.position, new Vector2(-1.27f, 4.24f), 0.1f * Time.deltaTime * 60f);
		            }else{
                        rb.gravityScale = 1;
                        gState = GoblinState.engaged;
		            }
	            }
            }
            break;

            case GoblinState.engaged:
            {
                rb.velocity = new Vector2(dirX * (hsp * 2.5f), rb.velocity.y);
                
                /*
                if ((((Math.Abs(transform.position.x - player.transform.position.x) <= 3f) && ((!sprite.flipX && transform.position.x < player.transform.position.x) || (sprite.flipX && transform.position.x > player.transform.position.x))) || collidedWithPlayer) && !shocked && Grounded() && noHitWall && noHitHazard)
                {
                    eState = EnemyState.shocked;
                    AudioClip[] clips = { sndAlert, sndAlert2, sndAlert3 };
                    int index = UnityEngine.Random.Range(0, clips.Length);
                    if (index < clips.Length) { audioSrc.PlayOneShot(clips[index]); }
                    MovementState mstate = MovementState.shocked;
                    anim.SetInteger("mstate", (int)mstate);
                    rb.velocity = new Vector2(0f, rb.velocity.y);
                    anim.speed = 1f;
                    shocked = true;
                    alarm3 = 300;
                    collidedWithPlayer = false;
                }*/

	            //var dir = 0;
	
	            //if !flash {dir = spd; hit = 0;}
	
	            if (transform.position.x > player.transform.position.x) {dirX = -1;} else {dirX = 1;}
	            if (dirX > 0) sprite.flipX = false; else if (dirX < 0) sprite.flipX = true;

		        if ((Vector3.Distance(player.transform.position, transform.position) <= 2.05f) && !player.isEnemyAttacking && Grounded() && ((!sprite.flipX && transform.position.x < player.transform.position.x) || (sprite.flipX && transform.position.x > player.transform.position.x)))
                {
                    gState = GoblinState.attack;
                    AudioClip[] clips = { sndAttack, sndAttack2 };
                    int index = UnityEngine.Random.Range(0, clips.Length);
                    if (index < clips.Length) { audioSrc.PlayOneShot(clips[index]); }
                    rb.gravityScale = 0;
                    int hitIndex = UnityEngine.Random.Range(0, 2);
                    MovementState mstate = MovementState.idle;

                    switch (hitIndex)
                    {
                        case 0: { mstate = MovementState.punch1; anim.speed = 1f; } break;
                        case 1: { mstate = MovementState.punch2; anim.speed = 1f; } break;
                    }

                    anim.SetInteger("mstate", (int)mstate);
                    player.isEnemyAttacking = true;
                }
		
		        //if (gState == GoblinState.engaged && (Math.Abs(transform.position.x - player.transform.position.x) > 1.9f) && transform.position.x > -10.27f && transform.position.x < -1.27f) {hsp = 1;} else if state != ggState.engaged && x > 480 && x < 950 {hsp += dir * walk_acc;}
		        if (gState == GoblinState.engaged && Math.Abs(transform.position.x - player.transform.position.x) < 1.9f) {dirX = 0;}
		        if (gState == GoblinState.engaged && (Math.Abs(transform.position.x - player.transform.position.x) > 1.9f) && transform.position.x < -10.27f) {dirX = 1;}
		        if (gState == GoblinState.engaged && (Math.Abs(transform.position.x - player.transform.position.x) > 1.9f) && transform.position.x > -1.27f) {dirX = -1;}
	
	            if (Vector3.Distance(player.transform.position, transform.position) >= 5.13f && canThrow)
	            {
		            throwing = true;
		            canThrow = false;

                    if (!startAlarm11) { alarm11 = 5; startAlarm11 = true; }
	            }
	
	            if (throwing) { dirX = 0; }
	            if (!throwing && threw) {alarm12 = 90; threw = false;}
	            if (!throwing) {spinners = false;}
	            if (threw) {spinners = false;}
	
	            if (player.transform.position.y + 2.59f < transform.position.y) {throwing = true; canThrow = false; spinners = true;}
			
	            if (spinners)
	            {
		            hsp = 0;
		
		            if (!stateInfo.IsName("Goblin_Throw"))
		            {
                        anim.SetInteger("mstate", (int)MovementState.throwingprojectile);
                    }
			
		            if (stateInfo.IsName("Goblin_Throw") && (stateInfo.normalizedTime >= 0.95f))
		            {
			            alarm12 = 90;
			            spinners = false;
			            throwing = false;
                        anim.SetInteger("mstate", (int)MovementState.idle);
                    }
		
		            if ((stateInfo.normalizedTime >= 0.5f) && (stateInfo.normalizedTime <= 0.53f) && FindObjectsOfType<PumpkinSpinner>().Length < 2)
		            {
                        int dirS = sprite.flipX ? -1 : 1;

                        SpawnSpinner(dirS, RandomChoice(2, 4, 6, 8));
                        SpawnSpinner(dirS, RandomChoice(4, 8, 12, 16));

                        AudioClip clip = UnityEngine.Random.value < 0.5f ? sndGSpinner1 : sndGSpinner1;
                        audioSrc.PlayOneShot(clip);
		            }
	            }
			
	            if (threw)
	            {
		            dirX = 0;
		
		            if (!stateInfo.IsName("Goblin_Throw"))
                    {
			            anim.SetInteger("mstate", (int)MovementState.throwingprojectile);
		            }

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
                        anim.SetInteger("mstate", (int)MovementState.idle);
                    }
		
		            if ((stateInfo.normalizedTime >= 0.5f) && (stateInfo.normalizedTime <= 0.53f) && FindObjectsOfType<PumpkinProjectile>().Length == 0)
		            {
                        GameObject bomb = Instantiate(
                            goblinBombPrefab,
                            transform.position + Vector3.down * 25f,
                            Quaternion.identity
                        );

                        int dirP = sprite.flipX ? -1 : 1;
                        bomb.GetComponent<PumpkinProjectile>().dir = dirP;
                        bomb.GetComponent<PumpkinProjectile>().airborne = false;

                        AudioClip[] clips = { sndGLaugh1, sndGLaugh2, sndGLaugh3 };
                        audioSrc.PlayOneShot(clips[UnityEngine.Random.Range(0, clips.Length)]);
		            }
	            }

                if (!wasGrounded && Grounded() && gState == GoblinState.engaged)
                    audioSrc.PlayOneShot(sndLand);

                wasGrounded = Grounded();
            }
            break;

            case GoblinState.attack:
            {
                rb.velocity = new Vector2(0f, 0f);

                if (Math.Abs(player.transform.position.x - transform.position.x) >= 0.45f && ((stateInfo.IsName("Goblin_Punch1") && stateInfo.normalizedTime <= 0.24f) || (stateInfo.IsName("Goblin_Punch2") && stateInfo.normalizedTime <= 0.38f)))
                {
                    float step = 4f * Time.deltaTime;
                    Vector2 targetPosition = new Vector2(player.transform.position.x, transform.position.y);
                    transform.position = Vector2.MoveTowards(transform.position, targetPosition, step);
                    if (targetPosition.x < transform.position.x) { sprite.flipX = true; } else { sprite.flipX = false; }
                }

                if ((stateInfo.IsName("Enemy_Punch1") && stateInfo.normalizedTime >= 1f) || (stateInfo.IsName("Enemy_Punch2") && stateInfo.normalizedTime >= 1f))
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
                    anim.speed = 1f;
                    rb.gravityScale = 1;
                }
            }
            break;

            case GoblinState.getting_hit:
            {
                anim.speed = 1f;
                if ((stateInfo.IsName("Goblin_Hit1") && stateInfo.normalizedTime >= 1f) || (stateInfo.IsName("Goblin_Hit2") && stateInfo.normalizedTime >= 1f)) { gState = GoblinState.engaged; }
            }
            break;
        }

        UpdateAnimationState();
    }

    private void UpdateAnimationState()
    {
        if (gState != GoblinState.attack)
        {
            if (dirX > 0f)
                sprite.flipX = false;
            else if (dirX < 0f)
                sprite.flipX = true;
        }

        if (spinners) return;
        if (gState == GoblinState.getting_hit) return;
        if (gState == GoblinState.attack) return;
        MovementState mstate = MovementState.idle;

        if (gState == GoblinState.on_glider)
        {
            sprite.flipX = glider.sr.flipX;

            if (threw)
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
            if (dirX > 0f)
                mstate = MovementState.sprinting;
            else if (dirX < 0f)
                mstate = MovementState.sprinting;
            else
                mstate = MovementState.idle;

            if (rb.velocity.y < -0.1f) { mstate = MovementState.falling; }
        }

        if (gState == GoblinState.death)
        {
            anim.speed = 1f;
            mstate = MovementState.death;
        }

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        float normalizedTime = stateInfo.normalizedTime % 1f;

        if (mstate == MovementState.sprinting)
        {
            if (normalizedTime >= 0.45f && normalizedTime <= 0.55f && !hasPlayedStep1)
            {
                audioSrc.PlayOneShot(sndStep);
                hasPlayedStep1 = true;
            }
            else if (normalizedTime >= 0.90f && normalizedTime <= 1.00f && !hasPlayedStep2)
            {
                audioSrc.PlayOneShot(sndStep);
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
                if (Grounded()) audioSrc.PlayOneShot(sndLand);

                if (!startAlarm6)
                {
                    alarm6 = 240;
                    startAlarm6 = true;
                }
            }

            if (normalizedTime == 1f)
            {
                anim.speed = 0f;
                normalizedTime = 1f;
            }
        }

        anim.SetInteger("mstate", (int)mstate);
    }

    public bool Grounded()
    {
        return Physics2D.BoxCast(coll.bounds.center, coll.bounds.size, 0f, Vector2.down, 0.1f, jumpableGround);
    }

    public void OnPlayerHit(GoblinStep target)
    {
        player.isEnemyAttacking = false;

        if (target == this)
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

            if (player.uppercut)
                rb.velocity = new Vector2(dir, 5f);
            else if ((player.combo - 4) % 5 == 0)
                rb.velocity = new Vector2(2.5f * dir, 0f);
            else
                rb.velocity = new Vector2(dir, 0f);

            anim.speed = 1f;
            gState = GoblinState.getting_hit;

            int attackTime = UnityEngine.Random.Range(0, 3);

            switch (attackTime)
            {
                case 0: { alarm4 = 300; } break;
                case 1: { alarm4 = 400; } break;
                case 2: { alarm4 = 500; } break;
            }

            MovementState mstate;

            int hitIndex = UnityEngine.Random.Range(0, 2); // 0 or 1

            if (hitIndex == 0)
                mstate = MovementState.hurt1;
            else
                mstate = MovementState.hurt2;

            if ((player.combo - 4) % 5 == 0)
            {
                AudioClip[] clips2 = { sndStrongHit, sndStrongHit2, };
                int index2 = UnityEngine.Random.Range(0, clips2.Length);
                if (index2 < clips2.Length) { audioSrc.PlayOneShot(clips2[index2]); }
            }
            else
            {
                AudioClip[] clips2 = { sndQuickHit, sndQuickHit2 };
                int index2 = UnityEngine.Random.Range(0, clips2.Length);
                if (index2 < clips2.Length) { audioSrc.PlayOneShot(clips2[index2]); }
            }

            anim.SetInteger("mstate", (int)mstate);
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

            AudioClip[] clips = { sndHit, sndHit2, sndHit3 };
            int index = UnityEngine.Random.Range(0, clips.Length);
            if (index < clips.Length) { audioSrc.PlayOneShot(clips[index]); }
        }
    }

    public void AttackEvent()
    {
        Debug.Log(Vector3.Distance(player.transform.position, transform.position));
        //if (Vector3.Distance(player.transform.position, transform.position) <= 0.45f) { player.Damage(this); }
    }
    public bool IsOnScreen(Camera cam)
    {
        Vector3 viewportPos = cam.WorldToViewportPoint(transform.position);
        return viewportPos.x > 0 && viewportPos.x < 1 &&
               viewportPos.y > 0 && viewportPos.y < 1 &&
               viewportPos.z > 0;
    }

    public void SpawnObjectHitEffect(Vector2 impactPoint, GameObject other)
    {
        Vector3 hitPosition = (transform.position + other.transform.position) / 2f;
        GameObject hitFX = Instantiate(hitParticlePrefab, impactPoint, Quaternion.identity);
    }

    void SpawnSpinner(int dir, int speed)
    {
        GameObject spinner = Instantiate(
            goblinSpinnerPrefab,
            transform.position + Vector3.down * 25f,
            Quaternion.identity
        );

        var s = spinner.GetComponent<PumpkinSpinner>();
        s.dir = dir;
        s.airborne = true;
        s.hspeed = speed * dir;
    }

    void StartThrowCooldown()
    {
        StartCoroutine(ThrowCooldown());
    }

    IEnumerator ThrowCooldown()
    {
        yield return new WaitForSeconds(1f); // 60 frames
        threw = true;
    }

    void PlayGoblinActionSound()
    {
        AudioClip clip = UnityEngine.Random.value < 0.5f ? sndGAction1 : sndGAction2;
        audioSrc.PlayOneShot(clip);
    }

    int RandomChoice(params int[] values)
    {
        return values[UnityEngine.Random.Range(0, values.Length)];
    }
}