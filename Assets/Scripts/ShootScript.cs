using Unity.Burst.CompilerServices;
using UnityEngine;
using static RobotStep;
using UnityEngine.U2D;
using static UnityEditorInternal.VersionControl.ListControl;

public class ShootScript : MonoBehaviour
{
    [Range(1, 10)]
    [SerializeField] private float speed = 10f;

    [Range(1, 10)]
    [SerializeField] private float lifeTime = 3f;

    private Rigidbody2D rb;
    [SerializeField] private AudioSource audioSrc;
    [SerializeField] private AudioClip sndWebDestroy;
    [SerializeField] private PlayerStep player;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        player = FindObjectOfType<PlayerStep>();
        Destroy(gameObject, lifeTime);
    }

    private void FixedUpdate()
    {
        rb.velocity = transform.right * speed; 
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ground") || other.CompareTag("CarSolid"))
        {
            audioSrc.PlayOneShot(sndWebDestroy);
            Destroy(gameObject);
        }
        
        if (other.CompareTag("Enemy"))
        {
            RobotStep enemy = other.GetComponent<RobotStep>();
            enemy.eState = RobotStep.EnemyState.webbed;
            enemy.alarm7 = 30;
            enemy.anim.SetInteger("mstate", 13);
            enemy.alarm5 = 240;
            audioSrc.PlayOneShot(sndWebDestroy);
            player.alarm3 = 300;
            Destroy(gameObject);
        }

        if (other.CompareTag("Goblin"))
        {
            GoblinStep goblin = other.GetComponent<GoblinStep>();
            goblin.blocking = false;

            if (goblin.gState != GoblinStep.GoblinState.death)
            {
                float dir = transform.position.x < other.transform.position.x ? 1f : -1f;

                if (goblin.gState != GoblinStep.GoblinState.on_glider)
                {
                    goblin.rb.velocity = new Vector2(dir * 1f, 0f);
                    goblin.anim.speed = 1;
                    goblin.gState = GoblinStep.GoblinState.getting_hit;

                    GoblinStep.MovementState mstate;
                    int hitIndex = Random.Range(0, 2); // 0 or 1

                    if (hitIndex == 0)
                        mstate = GoblinStep.MovementState.hurt1;
                    else
                        mstate = GoblinStep.MovementState.hurt2;

                    goblin.anim.SetInteger("mstate", (int)mstate);
                }
            }

            audioSrc.PlayOneShot(sndWebDestroy);
            Destroy(gameObject);
        }
    }
}