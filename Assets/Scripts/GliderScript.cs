using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GliderScript : MonoBehaviour
{
    public enum GState {Shooting, Throwing, Zooming, GroundFight, AirFight}

    [SerializeField] public PlayerStep player;
    [SerializeField] public GoblinStep goblin;
    [SerializeField] public GameObject bulletPrefab;
    [SerializeField] public AudioSource bgm;
    [SerializeField] public AudioSource sfx;
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
    private float xOff = 4.6f;
    private float xOffDir = 1f;
    private float ptSpeed;

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

                        switch(index)
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
                    float spd = Mathf.Lerp(0, 6, Mathf.Abs(transform.position.x - player.transform.position.x) / 150f);
                    xOff = 1.56f;

                    float horizDist = Mathf.Abs(transform.position.x - player.transform.position.x);
                    float crossBoost = Mathf.Lerp(2.5f, 0f, Mathf.Clamp01(horizDist / Mathf.Abs(xOff)));

                    Vector2 throwTarget = new Vector2(player.transform.position.x + (xOff * xOffDir), player.transform.position.y + 1.2f + crossBoost);
                    transform.position = Vector2.MoveTowards(transform.position, throwTarget, spd * Time.deltaTime * 60f);
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

    void HandleAirPathing()
    {
        if (state != GState.AirFight)
        {
            startedPath = false;
            StopPath();
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
            SetSpeed(0f);
        else
            SetSpeed(ptSpeed);
    }

    void FireBullet(float dir)
    {
        Instantiate(bulletPrefab, transform.position + new Vector3(dir * 0.12f, -0.05f), Quaternion.identity);
        AudioClip[] clips = { sndGLaugh2, sndGLaugh3 };
        int index = Random.Range(0, clips.Length);
        if (index < clips.Length) { sfx.PlayOneShot(clips[index]); }
        alarm1Timer = 15f;
        shot = true;
    }

    void HandleAlarms()
    {
        alarm0Timer -= Time.deltaTime * 60f;
        alarm1Timer -= Time.deltaTime * 60f;

        if (alarm0Timer <= 0)
        {
            xOffDir = Random.Range(0, 2) == 0 ? -1f : 1f;
            alarm0Timer = 180f;
        }

        if (alarm1Timer <= 0)
        {
            shot = false;
        }
    }

    public void StartRandomPath(float startSpeed)
    {
        currentPath = paths[Random.Range(0, paths.Length)];
        index = 0;
        speed = startSpeed;
        active = true;
    }

    public void StopPath()
    {
        active = false;
    }

    public void SetSpeed(float s)
    {
        speed = s;
    }



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

    void StartHoverLoop()
    {
        if (hoverSource == null) return;
        hoverSource.clip = sndGliderHover;
        hoverSource.loop = true;
        hoverSource.volume = 0.5f;
        if (!hoverSource.isPlaying) hoverSource.Play();
    }

    void StopHoverLoop()
    {
        if (hoverSource == null) return;
        hoverSource.Stop();
    }

    void StartFlyLoop()
    {
        if (flySource == null) return;
        flySource.clip = sndGliderFly;
        flySource.loop = true;
        flySource.volume = 0.5f;
        if (!flySource.isPlaying) flySource.Play();
    }

    void StopFly()
    {
        if (flySource == null) return;
        flySource.Stop();
        flySource.loop = false;
    }

    void PlayRandomWhoosh()
    {
        AudioClip[] clips = { sndGliderWhoosh1, sndGliderWhoosh2 };
        sfx.PlayOneShot(clips[Random.Range(0, clips.Length)], 0.5f);
    }
}