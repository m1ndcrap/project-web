using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.XR;
using static RobotStep;
using static Unity.Collections.AllocatorManager;

public class ShockerStep : MonoBehaviour
{
    public Rigidbody2D rb;
    [SerializeField] public Animator anim;
    [SerializeField] public SpriteRenderer sprite;
    private BoxCollider2D coll;
    [SerializeField] private float dirX = 0f;
    [SerializeField] private bool setCustomStartingDir = false;
    [SerializeField] private LayerMask jumpableGround;
    [SerializeField] private LayerMask playerMask;

    public enum MovementState { jump, idle, falling, hurt1, hurt2, sprinting, punch1, punch2, kick, death, shoot1, shoot2 }
    public enum ShockerState { chase, engaged, attack, getting_hit, death }
    public ShockerState sState;

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
    [SerializeField] private AudioClip sndBlock;
    [SerializeField] private AudioClip sndWarning;
    [SerializeField] private AudioClip sndShockerIntro;
    [SerializeField] private AudioClip sndShockerMove;
    private AudioClip sndQuickHit;
    private AudioClip sndQuickHit2;
    private AudioClip sndStrongHit;
    private AudioClip sndStrongHit2;
    private bool wasGrounded = false;
    private bool hasPlayedStep1;
    private bool hasPlayedStep2;

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
    public bool kick = false;
    public bool attacking = false;
    public bool collidedWithPlayer = false;
    [SerializeField] private GameObject hitParticlePrefab;

    // health bar
    [SerializeField] public int health = 300;
    [SerializeField] private int maxHealth = 300;
    BossHealth healthbar;

    [SerializeField] private bool throwing = false;
    [SerializeField] private bool threw = false;
    [SerializeField] private GameObject shockwavePrefab;
    private bool canThrow = true;
    private bool canAttack = true;

    //chase
    [SerializeField] GameObject trigger;
    [SerializeField] private GameObject chaseTriggerPrefab;
    private bool chased = true;
    [SerializeField] private int phase = 0;
    private bool blast = false;
    private bool moving = false;
    private int move_dir = 1;
    [SerializeField] private GameObject bgmController;
    [SerializeField] private GameObject barrier1;
    [SerializeField] private GameObject barrier2;

    private string shootAnim = "Shocker_Shoot1";
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<BoxCollider2D>();
        if (!setCustomStartingDir) { dirX = UnityEngine.Random.Range(0, 2) == 0 ? 1 : -1; }
        outline = sprite.material;
        player.OnHitS.AddListener((x) => OnPlayerHit(x));
        sndQuickHit = player.sndQuickHit;
        sndQuickHit2 = player.sndQuickHit2;
        sndStrongHit = player.sndStrongHit;
        sndStrongHit2 = player.sndStrongHit2;
        healthbar = FindObjectOfType<BossHealth>();
        healthbar.UpdateHealthBar(health, maxHealth);

        BoxCollider2D myCollider = GetComponent<BoxCollider2D>();
        int sharedLayer = LayerMask.NameToLayer("Enemy");

        foreach (GameObject obj in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (obj == this.gameObject) continue;
            if (obj.layer != sharedLayer) continue;
            BoxCollider2D otherCollider = obj.GetComponent<BoxCollider2D>();
            if (otherCollider != null) Physics2D.IgnoreCollision(myCollider, otherCollider, true);
        }

        barrier1.SetActive(false);
        barrier2.SetActive(false);
    }

    void Update()
    {        
        distanceFromPlayer = Vector3.Distance(player.transform.position, transform.position);
        bool noHitWall = !Physics2D.Raycast(transform.position, (player.transform.position - transform.position).normalized, distanceFromPlayer, jumpableGround);

        // Outline Shader Color Control
        if (sState == ShockerState.attack) { outline.color = Color.red; }
        else if (Math.Abs(transform.position.x - player.transform.position.x) <= 5f && noHitWall && (sState == ShockerState.engaged || sState == ShockerState.getting_hit)) { outline.color = Color.white; }
        else { outline.color = Color.black; }

        collidedWithPlayer = Physics2D.Raycast(transform.position, transform.right * -dirX, 0.65f, playerMask);

        if (sState == ShockerState.getting_hit || player.pState == PlayerStep.PlayerState.dashenemy || sState == ShockerState.attack || (player.transform.position.y - transform.position.y > 0.015f && Grounded()))
            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Enemy"), LayerMask.NameToLayer("Player"), true);
        else
            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Enemy"), LayerMask.NameToLayer("Player"), false);

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

        if (sState != ShockerState.engaged) { throwing = false; }

        Vector2 start = transform.position;
        Vector2 end = player.transform.position;
        RaycastHit2D[] hits = Physics2D.LinecastAll(start, end);

        if (alarm4 > 0)
            alarm4 -= 4;
        else
        {
            canAttack = true;
        }

        if (alarm6 > 0)
            alarm6 -= 1;
        else
        {
            if (sState != ShockerState.death)
            {
                if (sState != ShockerState.chase)
                {
                    if (player.pState == PlayerStep.PlayerState.dashenemy || Vector3.Distance(player.transform.position, transform.position) <= 0.4f)
                    {
                        int hitIndex = UnityEngine.Random.Range(0, 2);
                        MovementState mstate = MovementState.idle;

                        switch (hitIndex)
                        {
                            case 0: { mstate = MovementState.punch1; anim.speed = 1f; } break;
                            case 1: { mstate = MovementState.punch2; anim.speed = 1f; } break;
                        }

                        anim.SetInteger("mstate", (int)mstate);

                        dirX = 0;
                        sState = ShockerState.attack;
                        player.trigger = true;
                        player.alarm4 = 60;
                        canAttack = false;
                        rb.gravityScale = 0;
                        player.isEnemyAttacking = true;
                    }
                }

                alarm7 = 350;
                alarm6 = 400;
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
                if (sState == ShockerState.death)
                {
                    #if UNITY_EDITOR
                        EditorApplication.isPlaying = false;
                    #else
                        Application.Quit();
                    #endif
                }
            }
        }

        if (alarm7 > 0)
            alarm7 -= 1;
        else
        {
            if (sState != ShockerState.death)
            {
                if (sState != ShockerState.chase)
                {
                    if (Vector3.Distance(player.transform.position, transform.position) <= 0.8f)
                    {
                        int hitIndex = UnityEngine.Random.Range(0, 2);
                        MovementState mstate = MovementState.idle;

                        switch (hitIndex)
                        {
                            case 0: { mstate = MovementState.punch1; anim.speed = 1f; } break;
                            case 1: { mstate = MovementState.punch2; anim.speed = 1f; } break;
                        }

                        anim.SetInteger("mstate", (int)mstate);

                        dirX = 0;

                        //shock = instance_create_depth(x, y, obj_player.depth - 1, obj_shockwave);
                        //shock.type = 0;
                        //attack = choose(spr_spunch_1, spr_spunch_2, spr_skick);
                        //audio_play_sound(snd_shockwave_blast, 10, false);
                        blast = true;

                        player.trigger = true;
                        player.alarm4 = 60;
                        canAttack = false;
                        rb.gravityScale = 0;
                        player.isEnemyAttacking = true;
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
                if (sState == ShockerState.engaged) { if (throwing) { threw = true; } }
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
            sState = ShockerState.death;
        }

        switch (sState)
        {
            case ShockerState.chase:
            {
                rb.velocity = new Vector2(dirX * 2.5f, rb.velocity.y);

                if (phase == 0)
                {
                    if (distanceFromPlayer < 2f)
                    {
                        phase = 1;
                        player.trigger = true;
                        player.alarm4 = 60;
                        audioSrc.PlayOneShot(sndWarning);
                        audioSrc.PlayOneShot(sndShockerIntro);
                        bgmController.GetComponent<BGMController>().intensity = 1;
                    }
                }

                if (phase == 1)
                {
                    if (transform.position.x < 78.798f && transform.position.y > 3.027f)
                    {
                        dirX = 1.25f;
                    }
                    else
                    {
                        dirX = 0f;

                        if (distanceFromPlayer < 1f)
                        {
                            phase = 2;
                            Destroy(trigger);
                            trigger = Instantiate(chaseTriggerPrefab, transform.position, Quaternion.identity);
                            trigger.transform.localScale = new Vector3(5f, 5f, 1f);
                            BoxCollider2D triggerCollider = trigger.GetComponent<BoxCollider2D>();
                            triggerCollider.enabled = true;
                            ObjectiveTrigger triggerScript = trigger.GetComponent<ObjectiveTrigger>();
                            triggerScript.enabled = true;
                            player.trigger = true;
                            player.alarm4 = 60;
                            audioSrc.PlayOneShot(sndWarning);
                            AudioClip[] clips = { sndShockerMove };
                            int index = UnityEngine.Random.Range(0, clips.Length + 1); // +1 to include "no sound"
                            if (index < clips.Length)
                                audioSrc.PlayOneShot(clips[index]);
                            bgmController.GetComponent<BGMController>().intensity = 1;
                        }
                    }

                    if (transform.position.x < 66.87f && transform.position.x > 66.237f && transform.position.y < 8.786f && transform.position.y > 7.086f)
                    {
                        rb.velocity = new Vector2(rb.velocity.x, 1.67f);
                    }
                }

                if (phase == 2)
                {
                    if (transform.position.x < 105.29f && transform.position.y < 2.80f)
                    {
                        dirX = 1.67f;
                    }
                    else
                    {
                        dirX = 0f;

                        if (distanceFromPlayer < 3f && transform.position.x >= 105.29f)
                        {
                            trigger.GetComponent<ObjectiveTrigger>().chasing = false;
                            trigger = Instantiate(chaseTriggerPrefab, new Vector2(97.23f, 2.97f), Quaternion.identity);
                            trigger.transform.localScale = new Vector3(5f, 5f, 1f);
                            bgmController.GetComponent<BGMController>().intensity = 0;
                            phase = 3;
                        }
                    }

                    if (transform.position.x < 97.78f && transform.position.x > 96.819f && transform.position.y < 3.377f && transform.position.y > -3f)
                    {
                        rb.velocity = new Vector2(rb.velocity.x, 10f);
                    }
                }

                if (phase == 3)
                {
                    if (transform.position.x < 112.863f)
                    {
                        dirX = 2.92f;
                    }
                    else
                    {
                        phase = 4;
                    }
                }

                if (phase == 4)
                {
                    rb.velocity = Vector2.zero;
                    dirX = 0f;
                    transform.position = new Vector2(83f, 3.013f);
                    phase = 5;
                }

                if (phase == 5)
                {
                    if (distanceFromPlayer < 2f)
                    {
                        Destroy(trigger);
                        phase = 6;
                        trigger = Instantiate(chaseTriggerPrefab, new Vector3(transform.position.x, transform.position.y, 0f), Quaternion.identity);
                        trigger.transform.localScale = new Vector3(20f, 20f, 1f);
                        trigger.GetComponent<ObjectiveTrigger>().countdown = false;
                        bgmController.GetComponent<BGMController>().intensity = 0;
                    }
                }

                if (phase == 6)
                {
                    if (transform.position.x < 83.02f && transform.position.y < 10.53f)
                    {
                        if (transform.position.y < 6.401f) 
                        {
                            dirX = -1.67f;
                        }
                        else
                        {
                            dirX = 1.67f;
                        }
                    }
                    else
                    {
                        dirX = 0f;

                        if (distanceFromPlayer < 1f && transform.position.x >= 83.02f && transform.position.y >= 8f)
                        {
                            phase = 7;
                            Destroy(trigger);
                            trigger = Instantiate(chaseTriggerPrefab, transform.position, Quaternion.identity);
                            trigger.transform.localScale = new Vector3(5f, 5f, 1f);
                            player.trigger = true;
                            player.alarm4 = 60;
                            audioSrc.PlayOneShot(sndWarning);
                            AudioClip[] clips = { sndShockerMove };
                            int index = UnityEngine.Random.Range(0, clips.Length + 1); // +1 to include "no sound"
                            if (index < clips.Length)
                                audioSrc.PlayOneShot(clips[index]);
                            bgmController.GetComponent<BGMController>().intensity = 1;
                        }
                    }

                    if (transform.position.x < 76.318f && transform.position.x > 75.541f && transform.position.y < 8.797f && transform.position.y > 2.459f)
                    {
                        rb.velocity = new Vector2(rb.velocity.x, 9f);
                    }
                    if (transform.position.x < 80.361f && transform.position.x > 79.152f && transform.position.y < 8.797f && transform.position.y > 6.401f)
                    {
                        rb.velocity = new Vector2(rb.velocity.x, 1.67f);
                    }
                }

                if (phase == 7)
                {
                    if (transform.position.x < 93.394f && transform.position.y < 10.53f)
                    {
                        dirX = 1.67f;
                    }
                    else
                    {
                        dirX = 0f;

                        if (distanceFromPlayer < 1f && transform.position.x >= 93.394f)
                        {
                            phase = 8;
                            trigger.GetComponent<ObjectiveTrigger>().chasing = false;
                            trigger = Instantiate(chaseTriggerPrefab, new Vector2(103.84f, 8.25f), Quaternion.identity);
                            trigger.transform.localScale = new Vector3(5f, 5f, 1f);
                            bgmController.GetComponent<BGMController>().intensity = 0;
                        }
                    }

                    if (transform.position.x < 91.851f && transform.position.x > 90.149f && transform.position.y < 8.797f && transform.position.y > 6.244f)
                    {
                        rb.velocity = new Vector2(rb.velocity.x, 3.33f);
                    }
                }

                if (phase == 8)
                {
                    if (transform.position.x < 102.1515f)
                    {
                        dirX = 2.92f;
                    }
                    else
                    {
                        phase = 9;
                    }
                }

                if (phase == 9)
                {
                    rb.velocity = Vector2.zero;
                    dirX = 0f;
                    transform.position = new Vector2(107.729f, 8.298f);
                    phase = 10;
                }

                if (phase == 10)
                {
                    if (distanceFromPlayer < 1f)
                    {
                        Destroy(trigger);
                        phase = 11;
                        trigger = Instantiate(chaseTriggerPrefab, transform.position, Quaternion.identity);
                        trigger.transform.localScale = new Vector3(5f, 5f, 1f);
                        player.trigger = true;
                        player.alarm4 = 60;
                        audioSrc.PlayOneShot(sndWarning);
                        AudioClip[] clips = { sndShockerMove };
                        int index = UnityEngine.Random.Range(0, clips.Length + 1); // +1 to include "no sound"
                        if (index < clips.Length)
                            audioSrc.PlayOneShot(clips[index]);
                        bgmController.GetComponent<BGMController>().intensity = 1;
                    }
                }

                if (phase == 11)
                {
                    if (transform.position.x < 115.44f && transform.position.y < 16.87f)
                    {
                        dirX = 1.67f;
                    }
                    else
                    {
                        dirX = 0f;

                        if (distanceFromPlayer < 1f && transform.position.x >= 115.44f)
                        {
                            phase = 12;
                            Destroy(trigger);
                            trigger = Instantiate(chaseTriggerPrefab, transform.position, Quaternion.identity);
                            trigger.transform.localScale = new Vector3(5f, 5f, 1f);
                            player.trigger = true;
                            player.alarm4 = 60;
                            audioSrc.PlayOneShot(sndWarning);
                            AudioClip[] clips = { sndShockerMove };
                            int index = UnityEngine.Random.Range(0, clips.Length + 1); // +1 to include "no sound"
                            if (index < clips.Length)
                                audioSrc.PlayOneShot(clips[index]);
                            bgmController.GetComponent<BGMController>().intensity = 1;
                        }
                    }

                    if (transform.position.x < 113.78f && transform.position.x > 112.384f && transform.position.y < 15.143f && transform.position.y > 7.086f)
                    {
                        rb.velocity = new Vector2(rb.velocity.x, 10f);
                    }
                }

                if (phase == 12)
                {
                    if (transform.position.x < 129.914f && transform.position.y > 7.086f)
                    {
                            dirX = 1.04f;
                    }
                    else
                    {
                        dirX = 0f;

                        if (distanceFromPlayer < 3f)
                        {
                            trigger.GetComponent<ObjectiveTrigger>().done = true;
                            chased = false;
                            bgmController.GetComponent<BGMController>().intensity = 2;
                            barrier1.SetActive(true);
                            barrier2.SetActive(true);
                            sState = ShockerState.engaged;
                        }
                    }
                }
            }
            break;

            case ShockerState.engaged:
            {
                rb.velocity = new Vector2(dirX * 2.5f, rb.velocity.y);

		        if ((Vector3.Distance(player.transform.position, transform.position) <= 0.8f) && !player.isEnemyAttacking && Grounded() && ((!sprite.flipX && transform.position.x < player.transform.position.x) || (sprite.flipX && transform.position.x > player.transform.position.x)) && canAttack)
                {
                    sState = ShockerState.attack;
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
                    canAttack = false;
                    player.isEnemyAttacking = true;
                }
		
		        if (Math.Abs(transform.position.x - player.transform.position.x) > 0.572f) {if (transform.position.x > player.transform.position.x) { dirX = -1; } else { dirX = 1; }}
		        if (Math.Abs(transform.position.x - player.transform.position.x) < 0.572f) {dirX = 0;}
		        if ((Math.Abs(transform.position.x - player.transform.position.x) > 0.572f) && transform.position.x < 125.657f) {dirX = 1;}
		        if ((Math.Abs(transform.position.x - player.transform.position.x) > 0.572f) && transform.position.x > 130.492f) {dirX = -1;}
                //if (transform.position.x < -10.27f && transform.position.y < 3.6f) { transform.position = new Vector2(-9.79f, 4.44f); }
                //if (transform.position.x > -1.27f && transform.position.y < 3.6f) { transform.position = new Vector2(-1.73f, 4.44f); }
	
	            if (Vector3.Distance(player.transform.position, transform.position) >= 2f && canThrow)
	            {
		            throwing = true;
		            canThrow = false;

                    int hitIndex = UnityEngine.Random.Range(0, 2);
                    MovementState mstate = MovementState.idle;

                    switch (hitIndex)
                    {
                        case 0: { mstate = MovementState.punch1; anim.speed = 1f; } break;
                        case 1: { mstate = MovementState.punch2; anim.speed = 1f; } break;
                    }

                    anim.SetInteger("mstate", (int)mstate);

                    if (!startAlarm11) { alarm11 = 5; startAlarm11 = true; }
	            }
	
	            if (throwing) { dirX = 0; if (player.transform.position.x > transform.position.x) { sprite.flipX = false; } else { sprite.flipX = true; } }
	            if (!throwing && threw) {alarm12 = 90; threw = false;}
			
	            if (threw)
	            {
		            dirX = 0;

		            if (stateInfo.IsName(shootAnim) && (stateInfo.normalizedTime >= 0.9f))
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
                        GameObject shockwave = Instantiate(shockwavePrefab, transform.position, Quaternion.identity);
                        //shockwave.type = 1;
                        //audioSrc.PlayOneShot(sndShockwaveBeam);
                        player.trigger = true;
                        player.alarm4 = 60;
                        player.isEnemyAttacking = true;
		            }
	            }

                if (!wasGrounded && Grounded() && sState == ShockerState.engaged)
                    audioSrc.PlayOneShot(sndLand);

                wasGrounded = Grounded();
            }
            break;

            case ShockerState.attack:
            {
                rb.velocity = new Vector2(0f, 0f);
                canAttack = false;

                if (Math.Abs(player.transform.position.x - transform.position.x) >= 0.45f && ((stateInfo.IsName("Shocker_Punch1") && stateInfo.normalizedTime <= 0.24f) || (stateInfo.IsName("Shocker_Punch2") && stateInfo.normalizedTime <= 0.38f) || (stateInfo.IsName("Shocker_Kick") && stateInfo.normalizedTime <= 0.38f)))
                {
                    float step = 4f * Time.deltaTime;
                    Vector2 targetPosition = new Vector2(player.transform.position.x, transform.position.y);
                    transform.position = Vector2.MoveTowards(transform.position, targetPosition, step);
                    if (targetPosition.x < transform.position.x) { sprite.flipX = true; } else { sprite.flipX = false; }
                }

                if ((stateInfo.IsName("Shocker_Punch1") && stateInfo.normalizedTime >= 1f) || (stateInfo.IsName("Shocker_Punch2") && stateInfo.normalizedTime >= 1f) || (stateInfo.IsName("Shocker_Kick") && stateInfo.normalizedTime >= 1f))
                {
                    int hitIndex = UnityEngine.Random.Range(0, 3);

                    switch (hitIndex)
                    {
                        case 0: { alarm4 = 300; } break;
                        case 1: { alarm4 = 400; } break;
                        case 2: { alarm4 = 500; } break;
                    }

                    sState = ShockerState.engaged;
                    player.isEnemyAttacking = false;
                    anim.speed = 1f;
                    rb.gravityScale = 1;
                }
            }
            break;

            case ShockerState.getting_hit:
            {
                anim.speed = 1f;
                if ((stateInfo.IsName("Shocker_Hit1") && stateInfo.normalizedTime >= 1f) || (stateInfo.IsName("Shocker_Hit2") && stateInfo.normalizedTime >= 1f)) { sState = ShockerState.engaged; }
            }
            break;

            case ShockerState.death:
            {
                Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Enemy"), LayerMask.NameToLayer("Player"), true);
                rb.gravityScale = 1;
                rb.velocity = Vector2.zero;
            }
            break;
        }

        UpdateAnimationState();
    }

    private void UpdateAnimationState()
    {
        if (sState != ShockerState.attack)
        {
            if (dirX > 0f)
                sprite.flipX = false;
            else if (dirX < 0f)
                sprite.flipX = true;
        }

        if (sState == ShockerState.getting_hit) return;
        if (sState == ShockerState.attack) return;
        MovementState mstate = MovementState.idle;

        if (sState == ShockerState.engaged || sState == ShockerState.chase)
        {
            if (threw)
            {
                int shootIndex = UnityEngine.Random.Range(0, 2);

                if (shootIndex == 0)
                    mstate = MovementState.shoot1;
                else
                    mstate = MovementState.shoot2;
            }
            else if (dirX > 0f)
                mstate = MovementState.sprinting;
            else if (dirX < 0f)
                mstate = MovementState.sprinting;
            else
                mstate = MovementState.idle;

            if (rb.velocity.y < -0.1f) { mstate = MovementState.falling; }

            if (sState == ShockerState.chase && rb.velocity.y > 0.1f) { mstate = MovementState.jump; }
        }

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        float normalizedTime = stateInfo.normalizedTime % 1f;

        if (sState == ShockerState.death)
        {
            anim.speed = 1f;
            mstate = MovementState.death;

            if (normalizedTime >= 0.352f && normalizedTime <= 0.389f)
            {
                if (Grounded()) audioSrc.PlayOneShot(sndLand);
            }

            if (!startAlarm2)
            {
                alarm2 = 360;
                startAlarm2 = true;
            }

            if (normalizedTime >= 0.99f)
            {
                anim.speed = 0f;
            }
        }

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

        anim.SetInteger("mstate", (int)mstate);
    }

    public bool Grounded()
    {
        return Physics2D.BoxCast(coll.bounds.center, coll.bounds.size, 0f, Vector2.down, 0.1f, jumpableGround);
    }

    public void OnPlayerHit(ShockerStep target)
    {
        player.isEnemyAttacking = false;

        if (sState == ShockerState.engaged)
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
                rb.velocity = new Vector2(2.5f * dir, 0f);
            else if ((player.combo - 4) % 5 == 0)
                rb.velocity = new Vector2(2.5f * dir, 0f);
            else
                rb.velocity = new Vector2(dir, 0f);

            MovementState mstate;
            anim.speed = 1f;

            sState = ShockerState.getting_hit;

            int hitIndex = UnityEngine.Random.Range(0, 2);

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

            AudioClip[] clips = { sndHit, sndHit2, sndHit3 };
            int index = UnityEngine.Random.Range(0, clips.Length);
            if (index < clips.Length) { audioSrc.PlayOneShot(clips[index]); }

            int attackTime = UnityEngine.Random.Range(0, 3);

            switch (attackTime)
            {
                case 0: { alarm4 = 300; } break;
                case 1: { alarm4 = 400; } break;
                case 2: { alarm4 = 500; } break;
            }

            anim.SetInteger("mstate", (int)mstate);
        }
    }

    public void AttackEvent()
    {
        if (Vector3.Distance(player.transform.position, transform.position) <= 0.45f) { player.DamageShocker(this); }
    }

    public void SpawnObjectHitEffect(Vector2 impactPoint, GameObject other)
    {
        Vector3 hitPosition = (transform.position + other.transform.position) / 2f;
        GameObject hitFX = Instantiate(hitParticlePrefab, impactPoint, Quaternion.identity);
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

    int RandomChoice(params int[] values)
    {
        return values[UnityEngine.Random.Range(0, values.Length)];
    }
}