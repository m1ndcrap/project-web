using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GliderScript : MonoBehaviour
{
    public enum GState
    {
        Shooting,
        Throwing,
        Zooming,
        GroundFight,
        AirFight
    }

    [SerializeField] public PlayerStep player;
    [SerializeField] public GoblinStep goblin;
    [SerializeField] public GameObject bulletPrefab;
    [SerializeField] public AudioSource bgm;
    [SerializeField] public AudioSource sfx;
    [SerializeField] public AudioClip sndGLaugh2;
    [SerializeField] public AudioClip sndGLaugh3;
    public float screenLeft = -18f;
    public float screenRight = 7f;

    public GState state = GState.Shooting;

    private float seconds;
    private bool moving;
    private bool shot;
    private bool startedPath;

    private float targetX, targetY;
    private float iniX, iniY;
    [SerializeField] private float i = 0f;
    private float yChange;
    private float xOff;
    private float ptSpeed;

    private float alarm0Timer;
    private float alarm1Timer;

    public SpriteRenderer sr;

    public GoblinPath[] paths;

    private GoblinPath currentPath;
    private int index;
    private float speed;
    private bool active;

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
        HandleAirPathing();
        HandleAlarms();

        switch (state)
        {
            case GState.Shooting: Shooting(); break;
            case GState.Throwing: Throwing(); break;
            case GState.Zooming: Zooming(); break;
            case GState.GroundFight: GroundFight(); break;
            case GState.AirFight: AirFight(); break;
        }
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
            return;
        }

        if (!startedPath)
        {
            StartRandomPath(ptSpeed);
            startedPath = true;
        }

        float dist = Vector2.Distance(transform.position, player.transform.position);
        ptSpeed = Mathf.Lerp(0.02f, 0.16f, (1f - (dist / 1110f)) * 0.08f);

        bool playerDanger = player.GetComponent<PlayerStep>().attacking; // || (player.GetComponent<PlayerStep>().attacking && Vector3.Distance(player.transform.position, transform.position) <= 5.2f)

        if (playerDanger) // && !goblin.blocking
            SetSpeed(0f);
        else
            SetSpeed(ptSpeed);

        //transform.position = Vector2.MoveTowards(transform.position, player.transform.position, ptSpeed * Time.deltaTime * 60f);
    }

    void Shooting()
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
            iniY = transform.position.y;
            i = iniX - targetX;
            moving = true;
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

    void FireBullet(float dir)
    {
        Instantiate(bulletPrefab, transform.position + new Vector3(dir * 0.12f, -0.05f), Quaternion.identity);
        AudioClip[] clips = { sndGLaugh2, sndGLaugh3 };
        int index = Random.Range(0, clips.Length);
        if (index < clips.Length) { sfx.PlayOneShot(clips[index]); }
        alarm1Timer = 15f;
        shot = true;
    }

    void Throwing()
    {
        moving = false;
        float spd = Mathf.Lerp(0, 6, Mathf.Abs(transform.position.x - player.transform.position.x) / 150f);
        transform.position = Vector2.MoveTowards(transform.position, new Vector2(player.transform.position.x + xOff, player.transform.position.y + 1.2f), spd * Time.deltaTime * 60f);
        sr.flipX = player.transform.position.x < transform.position.x;
    }

    void Zooming()
    {
        float amount = (goblin.gState == GoblinStep.GoblinState.on_glider || goblin.gState == GoblinStep.GoblinState.on_glider_hit) ? 0.44f : 0.15f;

        if (!moving)
        {
            if (transform.position.x > screenLeft && transform.position.x < screenRight)
            {
                float target = Mathf.Abs(transform.position.x - screenRight) < Mathf.Abs(transform.position.x - screenLeft) ? screenRight : screenLeft;
                transform.position = Vector2.MoveTowards(transform.position, new Vector2(target, transform.position.y), 0.1f * Time.deltaTime * 60f);
            }
            else
            {
                int index = Random.Range(0, 2);
                switch (index)
                {
                    case 0: { transform.position = new Vector2(screenLeft, player.transform.position.y); } break;
                    case 1: { transform.position = new Vector2(screenRight, player.transform.position.y); } break;
                }

                player.trigger = true;
                player.alarm4 = 60;
                moving = true;
            }
        }

        if (moving)
        {
            float dir = transform.position.x > screenLeft ? -1 : 1;
            sr.flipX = dir < 0;

            transform.position += Vector3.right * dir * amount * Time.deltaTime * 60f;

            if (transform.position.x <= screenLeft || transform.position.x >= screenRight)
                moving = false;
        }
    }

    void GroundFight()
    {
        if (transform.position.x > screenLeft && transform.position.x < screenRight)
        {
            float target = Mathf.Abs(transform.position.x - screenRight) < Mathf.Abs(transform.position.x - screenLeft) ? screenRight : screenLeft;
            transform.position = Vector2.MoveTowards(transform.position, new Vector2(target, transform.position.y), 0.1f * Time.deltaTime * 60f);
        }
        else
        {
            moving = true;
        }
    }

    void AirFight()
    {
        if (goblin.gState != GoblinStep.GoblinState.on_glider && goblin.gState != GoblinStep.GoblinState.air_hit && goblin.gState != GoblinStep.GoblinState.on_glider_hit && goblin.health > 0)
            goblin.gState = GoblinStep.GoblinState.on_glider;

        sr.flipX = transform.position.x > player.transform.position.x;

        if (player.GetComponent<PlayerStep>().attacking)
            return;

        if (!active || currentPath == null) return;
        Transform target = currentPath.points[index];
        transform.position = Vector2.MoveTowards(transform.position, target.position, speed * Time.deltaTime * 60f);

        if (Vector2.Distance(transform.position, target.position) < 0.05f)
        {
            index++;
            if (index >= currentPath.points.Length)
                index = 0;
        }
    }

    void HandleAlarms()
    {
        alarm0Timer -= Time.deltaTime * 60f;
        alarm1Timer -= Time.deltaTime * 60f;

        if (alarm0Timer <= 0)
        {
            xOff = Random.Range(-0.956f, 0.956f);
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
}