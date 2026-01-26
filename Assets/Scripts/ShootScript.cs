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

    private Animator anim;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        player = FindObjectOfType<PlayerStep>();
        Destroy(gameObject, lifeTime);
    }

    private void FixedUpdate()
    {
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        if (!stateInfo.IsName("WebDestroy")) { rb.velocity = transform.right * speed; } else { rb.velocity = Vector2.zero; }
        if (stateInfo.IsName("WebDestroy") && stateInfo.normalizedTime >= 0.8f) { Destroy(gameObject); }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

        if (other.CompareTag("Ground") || other.CompareTag("CarSolid"))
        {
            audioSrc.PlayOneShot(sndWebDestroy);
            if (!stateInfo.IsName("WebDestroy")) { anim.Play("WebDestroy"); }
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
            if (!stateInfo.IsName("WebDestroy")) { anim.Play("WebDestroy"); }
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
                    goblin.rb.velocity = new Vector2(0f, 0f);
                    goblin.anim.speed = 1;
                    goblin.gState = GoblinStep.GoblinState.getting_hit;

                    GoblinStep.MovementState mstate;
                    int hitIndex = Random.Range(0, 2); // 0 or 1

                    if (hitIndex == 0)
                        mstate = GoblinStep.MovementState.breakweb1;
                    else
                        mstate = GoblinStep.MovementState.breakweb1;

                    goblin.anim.SetInteger("mstate", (int)mstate);
                }
            }

            audioSrc.PlayOneShot(sndWebDestroy);
            if (!stateInfo.IsName("WebDestroy")) { anim.Play("WebDestroy"); }
        }
    }
}