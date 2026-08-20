using UnityEngine;


/// <summary>
/// A door that plays a break animation and destroys itself once <see cref="phase"/> is externally
/// set to 1 (for example, by the player attacking it), unlike <see cref="KeyDoors"/>, this door
/// doesn't require a key and doesn't leave a permanent "open" state behind; it's gone once broken.
/// </summary>


public class BreakableDoor : MonoBehaviour
{
    /// <summary>0 = intact, 1 = about to break, 2 = playing the break animation, 3 = fully broken (destroys itself).</summary>
    [Tooltip("0 = intact. Set this to 1 from another script (e.g. when the player attacks it) to break the door.")]
    public int phase = 0;
    private bool destroyed = false;
    private Animator anim;
    [Tooltip("Sound played the moment the door starts breaking.")]
    [SerializeField] private AudioClip sndBreak;
    [Tooltip("Empty placeholder/anchor for the door, if used by your door prefab setup.")]
    [SerializeField] private GameObject doorEmpty;
    [Tooltip("The solid wall piece blocking the doorway, destroyed along with this whole object once broken.")]
    [SerializeField] private GameObject doorWall;
    [Tooltip("The Animator state to play while intact (phase 0).")]
    [SerializeField] private string normalAnim;
    [Tooltip("The Animator state to play while breaking (phase 1 -> 2).")]
    [SerializeField] private string breakAnim;
    private int alarm1 = 0;


    private PlayerStep player;
    private Collider2D wallCollider;




    void Start()
    {
        anim = GetComponent<Animator>();
        player = FindObjectOfType<PlayerStep>();
        wallCollider = doorWall != null ? doorWall.GetComponentInChildren<Collider2D>() : null;
    }




    void Update()
    {
        if (phase == 0) { anim.Play(normalAnim); }

        if (phase == 1 && !destroyed)
        {
            alarm1 = 10;
            destroyed = true;
        }

        if (alarm1 > 0)
        {
            alarm1 -= 1;
        }
        else
        {
            if (phase == 1)
            {
                anim.Play(breakAnim);
                SfxPlayer.Instance.PlayClipAtPointMatched(sndBreak, transform.position);
                player?.ForceExitCrawlIfOn(wallCollider);
                phase = 2;
            }
        }

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

        if (phase == 2 && stateInfo.IsName(breakAnim) && stateInfo.normalizedTime >= 1f)
        {
            phase = 3;
        }

        if (phase == 3)
        {
            Destroy(doorWall);
            Destroy(doorEmpty);
            Destroy(gameObject);
        }
    }
}