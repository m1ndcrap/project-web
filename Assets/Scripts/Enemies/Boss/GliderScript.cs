using Framework.Core;
using UnityEngine;


/// <summary>
/// Controls the behavior, movement, and state transitions of the goblin's glider.
/// </summary>


public class GliderScript : MonoBehaviour
{
    public enum GState { Shooting, Throwing, Zooming, GroundFight, AirFight }


    [SerializeField] public PlayerStep player;
    [SerializeField] public GoblinStep goblin;
    [SerializeField] public GameObject bulletPrefab;
    [SerializeField] public AudioSource bgm;
    [SerializeField] public AudioSource sfx;
    private AudioController sfxController;
    [SerializeField] public AudioClip sndGLaugh2;
    [SerializeField] public AudioClip sndGLaugh3;
    [SerializeField] public AudioClip sndGliderAccelerate;
    [SerializeField] public AudioClip sndGliderDeaccelerate;
    [SerializeField] public AudioClip sndGliderHover;
    [SerializeField] public AudioClip sndGliderWhoosh1;
    [SerializeField] public AudioClip sndGliderWhoosh2;
    [SerializeField] public AudioClip sndGliderFly;
    [SerializeField] public AudioSource hoverSource;
    [SerializeField] public AudioSource flySource;


    private GState previousState;


    public float screenLeft = -18f;
    public float screenRight = 7f;


    public GState state = GState.Shooting;


    private float seconds;
    private bool moving;
    private bool zoomMoving;
    private bool shot;
    private bool startedPath;


    private float targetX, targetY;
    private float iniX;
    [SerializeField] private float i = 0f;
    private float xOffDir = 1f;
    private float ptSpeed;


    [Header("Throwing Position")]
    [Tooltip("How far to the side of the player the glider hovers while throwing. It never parks directly overhead, so bombs always have a sideways arc to travel.")]
    [SerializeField] private float throwSideOffset = 1.56f;

    [Tooltip("How high above the player the glider hovers while throwing.")]
    [SerializeField] private float throwHeightOffset = 1.2f;

    [Tooltip("Extra height added while crossing over the player, so the glider arcs above them instead of through them.")]
    [SerializeField] private float throwCrossBoost = 2.5f;

    [Tooltip("Slowest the glider repositions while throwing. This is what stops it stalling directly over the player's head.")]
    [SerializeField] private float throwMinSpeed = 0.06f;

    [Tooltip("Fastest the glider repositions while throwing.")]
    [SerializeField] private float throwMaxSpeed = 0.18f;

    [Tooltip("Distance from its target at which the glider is moving at full repositioning speed.")]
    [SerializeField] private float throwSpeedRange = 4f;

    // Where the glider is currently heading while throwing. Used to tell whether it has arrived,
    // so it does not pick a new side while still crossing over to the current one.
    private Vector2 currentThrowTarget;


    private float alarm0Timer;
    private float alarm1Timer;


    public SpriteRenderer sr;


    public GoblinPath[] paths;


    private GoblinPath currentPath;
    private int index;
    private float speed;
    private bool active;


    private float zoomDir = 1f;

    private enum AirTransition { None, MovingToStart, WaitingForJump, Active }
    private AirTransition airTransition = AirTransition.None;
    [SerializeField] private float airTransitionSpeed = 0.15f;




    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        sfxController = new AudioController(sfx);
        transform.localScale = Vector3.one * 0.45f;
        alarm0Timer = 240f;
    }




    void Update()
    {
        seconds = bgm.time;

        HandleMusicStates();
        HandleStateAudioTransitions(previousState, state);
        HandleAirPathing();
        HandleAlarms();

        switch (state)
        {
            case GState.Shooting:
                {
                    if (!moving)
                    {
                        targetX = player.transform.position.x;
                        targetY = player.transform.position.y;
                        int index = Random.Range(0, 2);

                        switch (index)
                        {
                            case 0: { transform.position = new Vector2(screenLeft, 7.59f); } break;
                            case 1: { transform.position = new Vector2(screenRight, 7.59f); } break;
                        }

                        iniX = transform.position.x;
                        i = iniX - targetX;
                        moving = true;

                        if (flySource != null)
                        {
                            flySource.clip = sndGliderFly;
                            flySource.volume = 0.5f;
                            flySource.Play();
                        }
                    }

                    if (moving)
                    {
                        if (iniX > screenLeft)
                        {
                            sr.flipX = true;

                            if (transform.position.x > screenLeft)
                            {
                                i -= 0.1f;
                                transform.position += Vector3.right * -0.1f * Time.deltaTime * 60f;
                            }
                            else
                            {
                                transform.position = new Vector2(screenLeft, transform.position.y);
                                moving = false;
                            }

                            if (transform.position.x - player.transform.position.x > 0 && transform.position.x - player.transform.position.x < 3.73f && !shot)
                            {
                                FireBullet(-1f);
                            }

                            transform.position = new Vector2(transform.position.x, targetY + (0.05f * i * i));
                        }
                        else
                        {
                            sr.flipX = false;

                            if (transform.position.x < screenRight)
                            {
                                i += 0.1f;
                                transform.position += Vector3.right * 0.1f * Time.deltaTime * 60f;
                            }
                            else
                            {
                                transform.position = new Vector2(screenRight, transform.position.y);
                                moving = false;
                            }

                            if (player.transform.position.x - transform.position.x > 0 && player.transform.position.x - transform.position.x < 3.73f && !shot)
                            {
                                FireBullet(1f);
                            }

                            transform.position = new Vector2(transform.position.x, targetY + (0.05f * i * i));
                        }
                    }
                }
                break;




            case GState.Throwing:
                {
                    moving = false;

                    float horizDist = Mathf.Abs(transform.position.x - player.transform.position.x);

                    // Ride higher while crossing over the player, then settle back down once off to the side.
                    float crossBoost = Mathf.Lerp(throwCrossBoost, 0f, Mathf.Clamp01(horizDist / Mathf.Abs(throwSideOffset)));

                    currentThrowTarget = new Vector2(
                        player.transform.position.x + (throwSideOffset * xOffDir),
                        player.transform.position.y + throwHeightOffset + crossBoost);

                    // Speed comes from how far there is left to travel, not from how far the player is.
                    // Measuring against the player made this hit zero exactly as the glider passed
                    // overhead, parking it directly above their head instead of moving through.
                    float distToTarget = Vector2.Distance(transform.position, currentThrowTarget);
                    float spd = Mathf.Lerp(throwMinSpeed, throwMaxSpeed, Mathf.Clamp01(distToTarget / throwSpeedRange));

                    transform.position = Vector2.MoveTowards(transform.position, currentThrowTarget, spd * Time.deltaTime * 60f);
                    sr.flipX = player.transform.position.x < transform.position.x;
                }
                break;




            case GState.Zooming:
                {
                    float amount = (goblin.gState == GoblinStep.GoblinState.on_glider) ? 0.44f : 0.15f;

                    if (!zoomMoving)
                    {
                        if (transform.position.x > screenLeft && transform.position.x < screenRight)
                        {
                            float target = Mathf.Abs(transform.position.x - screenRight) < Mathf.Abs(transform.position.x - screenLeft) ? screenRight : screenLeft;
                            transform.position = Vector2.MoveTowards(transform.position, new Vector2(target, transform.position.y), 0.075f * Time.deltaTime * 60f);
                        }
                        else
                        {
                            int index = Random.Range(0, 2);

                            switch (index)
                            {
                                case 0: { transform.position = new Vector2(screenLeft, player.transform.position.y); zoomDir = 1f; } break;
                                case 1: { transform.position = new Vector2(screenRight, player.transform.position.y); zoomDir = -1f; } break;
                            }

                            player.trigger = true;
                            player.alarm4 = 60;
                            zoomMoving = true;
                            PlayRandomWhoosh();
                        }
                    }

                    if (zoomMoving)
                    {
                        sr.flipX = zoomDir < 0;

                        transform.position += Vector3.right * zoomDir * amount * Time.deltaTime * 60f;

                        if (transform.position.x <= screenLeft || transform.position.x >= screenRight)
                            zoomMoving = false;
                    }
                }
                break;




            case GState.GroundFight:
                {
                    if (transform.position.x > screenLeft && transform.position.x < screenRight)
                    {
                        float target = Mathf.Abs(transform.position.x - screenRight) < Mathf.Abs(transform.position.x - screenLeft) ? screenRight : screenLeft;
                        transform.position = Vector2.MoveTowards(transform.position, new Vector2(target, transform.position.y), 0.1f * Time.deltaTime * 60f);
                    }
                }
                break;




            case GState.AirFight:
                {
                    sr.flipX = transform.position.x > player.transform.position.x;

                    if (player.GetComponent<PlayerStep>().attacking) return;

                    if (!active || currentPath == null) return;

                    Transform target = currentPath.points[index];
                    transform.position = Vector2.MoveTowards(transform.position, target.position, speed * Time.deltaTime * 60f);

                    if (Vector2.Distance(transform.position, target.position) < 0.05f)
                    {
                        index++;
                        if (index >= currentPath.points.Length) { index = 0; }
                    }
                }
                break;
        }

        previousState = state;
    }




    /// <summary>
    /// Updates the current state based on the elapsed BGM time in seconds.
    /// </summary>
    void HandleMusicStates()
    {
        if (seconds >= 28 && seconds < 41) state = GState.Throwing;
        else if (seconds >= 41 && seconds < 55) state = GState.Zooming;
        else if (seconds >= 55 && seconds < 90) state = GState.GroundFight;
        else if (seconds >= 90 && seconds < 120) state = GState.Zooming;
        else if (seconds >= 120 && seconds < 148) state = GState.AirFight;
        else if (seconds >= 148 && seconds < 189) state = GState.Shooting;
        else if (seconds >= 189 && seconds < 202) state = GState.Throwing;
        else if (seconds >= 202 && seconds < 216) state = GState.Zooming;
        else if (seconds >= 216 && seconds < 251) state = GState.GroundFight;
        else if (seconds >= 251 && seconds < 281) state = GState.Zooming;
        else if (seconds >= 281 && seconds < 309) state = GState.AirFight;
        else if (seconds >= 309) state = GState.Shooting;
    }




    /// <summary>
    /// Handles the air pathing logic for the object, including transitioning between air states and updating movement
    /// along a predefined path.
    /// </summary>
    void HandleAirPathing()
    {
        if (state != GState.AirFight)
        {
            startedPath = false;
            active = false;
            ptSpeed = 0;
            airTransition = AirTransition.None;
            return;
        }

        if (!startedPath)
        {
            currentPath = paths[Random.Range(0, paths.Length)];
            index = 0;
            startedPath = true;

            if (goblin.gState == GoblinStep.GoblinState.on_glider)
            {
                // goblin never left the glider; no jump needed
                airTransition = AirTransition.Active;
                speed = ptSpeed;
                active = true;
            }
            else
            {
                airTransition = AirTransition.MovingToStart;
                active = false;
            }
        }

        if (airTransition == AirTransition.MovingToStart)
        {
            Transform startPoint = currentPath.points[0];
            transform.position = Vector2.MoveTowards(transform.position, startPoint.position, airTransitionSpeed * Time.deltaTime * 60f);
            sr.flipX = transform.position.x > player.transform.position.x;

            if (Vector2.Distance(transform.position, startPoint.position) < 0.05f)
            {
                transform.position = startPoint.position;
                airTransition = AirTransition.WaitingForJump;
                goblin.BeginJumpToGlider();
            }

            return;
        }

        if (airTransition == AirTransition.WaitingForJump)
        {
            sr.flipX = transform.position.x > player.transform.position.x;

            if (goblin.gState == GoblinStep.GoblinState.on_glider)
            {
                airTransition = AirTransition.Active;
                speed = ptSpeed;
                active = true;
            }

            return;
        }

        float dist = Vector2.Distance(transform.position, player.transform.position);
        ptSpeed = Mathf.Lerp(0.02f, 0.16f, (1f - (dist / 1110f)) * 0.08f);

        bool playerDanger = player.GetComponent<PlayerStep>().attacking;

        if (playerDanger)
            speed = 0f;
        else
            speed = ptSpeed;
    }




    /// <summary>
    /// Fires a bullet in the specified horizontal direction from the current position.
    /// </summary>
    /// <param name="dir">The horizontal direction in which to fire the bullet. Positive values fire to the right; negative values fire to
    /// the left.</param>
    void FireBullet(float dir)
    {
        Instantiate(bulletPrefab, transform.position + new Vector3(dir * 0.12f, -0.05f), Quaternion.identity);
        sfxController.PlayRandom(sndGLaugh2, sndGLaugh3);
        alarm1Timer = 15f;
        shot = true;
    }




    /// <summary>
    /// Processes alarm timers and updates related state based on elapsed time.
    /// </summary>
    void HandleAlarms()
    {
        alarm0Timer -= Time.deltaTime * 60f;
        alarm1Timer -= Time.deltaTime * 60f;

        if (alarm0Timer <= 0)
        {
            // Only switch sides once the glider has actually arrived. Flipping mid-crossing turns it
            // around over the player's head, which leaves it hovering in the one place it should not be.
            if (state != GState.Throwing || HasReachedThrowTarget())
            {
                xOffDir = Random.Range(0, 2) == 0 ? -1f : 1f;
                alarm0Timer = 180f;
            }
            else
            {
                alarm0Timer = 30f;
            }
        }

        if (alarm1Timer <= 0)
        {
            shot = false;
        }
    }




    /// <summary>True once the glider is close enough to its throwing position to count as settled there.</summary>
    private bool HasReachedThrowTarget()
    {
        return Vector2.Distance(transform.position, currentThrowTarget) < 0.3f;
    }




    /// <summary>
    /// Initializes and starts movement along a randomly selected path at the specified starting speed.
    /// </summary>
    /// <param name="startSpeed">The initial speed at which to begin moving along the selected path.</param>
    public void StartRandomPath(float startSpeed)
    {
        currentPath = paths[Random.Range(0, paths.Length)];
        index = 0;
        speed = startSpeed;
        active = true;
    }




    /// <summary>
    /// Handles audio transitions and sound effects when the game state changes between specified states.
    /// </summary>
    /// <param name="prev">The previous game state before the transition.</param>
    /// <param name="current">The current game state after the transition.</param>
    void HandleStateAudioTransitions(GState prev, GState current)
    {
        if (prev == current) return;

        if (prev == GState.Shooting && current == GState.Throwing)
        {
            sfx.PlayOneShot(sndGliderDeaccelerate, 0.5f);
        }
        else if (prev == GState.Throwing && current == GState.Zooming)
        {
            sfx.PlayOneShot(sndGliderAccelerate, 0.5f);
        }

        if (prev == GState.Shooting && current != GState.Shooting)
        {
            StopFly();
        }

        if (current == GState.Throwing)
        {
            StartHoverLoop();
        }
        else if (prev == GState.Throwing)
        {
            StopHoverLoop();
        }

        if (current == GState.AirFight)
        {
            StartFlyLoop();
        }
        else if (prev == GState.AirFight)
        {
            StopFly();
        }
    }




    /// <summary>
    /// Starts playing the hover sound effect in a continuous loop if it is not already playing.
    /// </summary>
    void StartHoverLoop()
    {
        if (hoverSource == null) return;
        hoverSource.clip = sndGliderHover;
        hoverSource.loop = true;
        hoverSource.volume = 0.5f;
        if (!hoverSource.isPlaying) hoverSource.Play();
    }




    /// <summary>
    /// Stops the current hover loop if it is active.
    /// </summary>
    void StopHoverLoop()
    {
        if (hoverSource == null) return;
        hoverSource.Stop();
    }




    /// <summary>
    /// Starts playing the glider fly sound effect in a continuous loop if it is not already playing.
    /// </summary>
    void StartFlyLoop()
    {
        if (flySource == null) return;
        flySource.clip = sndGliderFly;
        flySource.loop = true;
        flySource.volume = 0.5f;
        if (!flySource.isPlaying) flySource.Play();
    }




    /// <summary>
    /// Stops the fly sound effect if it is currently playing.
    /// </summary>
    void StopFly()
    {
        if (flySource == null) return;
        flySource.Stop();
        flySource.loop = false;
    }




    /// <summary>
    /// Plays a randomly selected whoosh sound effect at a preset volume.
    /// </summary>
    void PlayRandomWhoosh()
    {
        AudioClip[] clips = { sndGliderWhoosh1, sndGliderWhoosh2 };
        sfx.PlayOneShot(clips[Random.Range(0, clips.Length)], 0.5f);
    }
}