using UnityEngine;


/// <summary>
/// A breakable environmental object (a switch, generator, or similar) that plays an "active" idle
/// animation, and once <see cref="phase"/> is externally set to 1 (for example, by the player
/// attacking it), waits briefly, plays a "break" animation with an explosion sound, then settles
/// into a permanent "inactive" animation once the break animation finishes.
/// </summary>


public class BreakableSwitch : MonoBehaviour
{
    /// <summary>
    /// Drives this object's break sequence: 0 = active/idle, 1 = triggered (set this from another
    /// script to start breaking it), 2 = playing the break animation, 3 = broken/inactive.
    /// </summary>
    [Tooltip("0 = active/idle. Set this to 1 from another script (e.g. on a hit) to trigger the break sequence; it advances to 2 and 3 automatically from there.")]
    public int phase = 0;


    [Header("Animation States")]
    [Tooltip("The Animator state to play while phase is 0 (idle/active).")]
    [SerializeField] private string activeStateName;
    [Tooltip("The Animator state to play once triggered (phase 1 -> 2).")]
    [SerializeField] private string breakStateName;
    [Tooltip("The Animator state to play once the break animation finishes (phase 3, permanent).")]
    [SerializeField] private string inactiveStateName;


    [Header("Audio")]
    [Tooltip("One of these two clips is chosen at random and played when the break animation starts.")]
    [SerializeField] private AudioClip sndExplosion1;
    [SerializeField] private AudioClip sndExplosion2;


    [Tooltip("Frames to wait after phase becomes 1 before actually starting the break animation. Keeps the break from feeling instantaneous.")]
    [SerializeField, Range(0, 60)] private int breakDelayFrames = 10;


    private Animator anim;
    private AudioSource audioSrc;
    private bool triggered = false;
    private int delayTimer = 0;




    private void Start()
    {
        anim = GetComponent<Animator>();
        audioSrc = GetComponent<AudioSource>();
    }




    private void Update()
    {
        if (phase == 0)
        {
            anim.Play(activeStateName);
        }

        if (phase == 1 && !triggered)
        {
            delayTimer = breakDelayFrames;
            triggered = true;
        }

        if (delayTimer > 0)
        {
            delayTimer -= 1;
        }
        else if (phase == 1)
        {
            anim.Play(breakStateName);
            audioSrc.PlayOneShot(Random.value < 0.5f ? sndExplosion1 : sndExplosion2);
            phase = 2;
        }

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

        if (phase == 2 && stateInfo.IsName(breakStateName) && stateInfo.normalizedTime >= 1f)
        {
            phase = 3;
        }

        if (phase == 3)
        {
            anim.Play(inactiveStateName);
        }
    }
}