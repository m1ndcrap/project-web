using UnityEngine;

/// <summary>
/// A color-coded door that stays shut until the player collides with it while holding a matching
/// key (handled in PlayerStep's collision logic, which calls <see cref="RemoveKeyByColor"/> and
/// sets <see cref="phase"/> to 1 to trigger this door opening). Also supports paired doors that
/// open together, if <see cref="doorPair"/>'s phase changes first, this door opens automatically too.
/// </summary>
public class KeyDoors : MonoBehaviour
{
    /// <summary>0 = closed, 1 = about to open, 2 = playing the opening animation, 3 = fully open (wall removed).</summary>
    [Tooltip("0 = closed. Set this to 1 from another script (e.g. when the player uses a matching key) to open the door.")]
    public int phase = 0;


    [Header("Door Parts")]
    [Tooltip("The solid wall piece blocking the doorway, destroyed once the door finishes opening.")]
    [SerializeField] private GameObject doorWall;


    [Header("Configuration")]
    [Tooltip("Which key color opens this door. Case-insensitive, so \"Red\", \"red\", and \"RED\" all work.")]
    [SerializeField] private string doorColor = "nothing";
    [Tooltip("Optional. A second door that opens together with this one, if the pair's phase changes first, this door follows automatically.")]
    [SerializeField] private GameObject doorPair;


    [Header("Audio")]
    [SerializeField] private AudioSource audioSrc;
    [SerializeField] private AudioClip sndDoorOpen;


    [Tooltip("Frames to wait after phase becomes 1 before actually starting the opening animation.")]
    [SerializeField, Range(0, 60)] private int openDelayFrames = 10;


    private Animator anim;
    private bool openTriggered = false;
    private int delayTimer = 0;
    private bool matchedPair = false;


    private PlayerStep player;
    private Collider2D wallCollider;
    private KeyDoors pairedDoor;




    private void Start()
    {
        anim = GetComponent<Animator>();
        player = FindObjectOfType<PlayerStep>();
        wallCollider = doorWall != null ? doorWall.GetComponentInChildren<Collider2D>() : null;
        pairedDoor = doorPair != null ? doorPair.GetComponent<KeyDoors>() : null;
    }




    private void Update()
    {
        if (pairedDoor != null && pairedDoor.phase != 0 && !matchedPair)
        {
            phase = 1;
            matchedPair = true;
        }

        if (phase == 0)
        {
            PlayDoorAnimation("Closed");
        }

        if (phase == 1 && !openTriggered)
        {
            delayTimer = openDelayFrames;
            openTriggered = true;
        }

        if (delayTimer > 0)
        {
            delayTimer -= 1;
        }
        else if (phase == 1)
        {
            audioSrc.PlayOneShot(sndDoorOpen);
            PlayDoorAnimation("Opening");
            player?.ForceExitCrawlIfOn(wallCollider);
            phase = 2;
        }

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

        if (phase == 2 && stateInfo.IsName(ColoredStateName("Opening")) && stateInfo.normalizedTime >= 1f)
        {
            phase = 3;
        }

        if (phase == 3)
        {
            Destroy(doorWall);
            PlayDoorAnimation("Open");
        }
    }




    /// <summary>
    /// Plays this door's Animator state for the given suffix ("Closed", "Opening", or "Open"), using whichever color this door is configured for.
    /// </summary>
    /// <param name="stateSuffix">The state suffix to play, matches the "Closed"/"Opening"/"Open" clip naming convention.</param>
    private void PlayDoorAnimation(string stateSuffix)
    {
        string stateName = ColoredStateName(stateSuffix);

        if (stateName != null)
        {
            anim.Play(stateName);
        }
    }




    /// <summary>
    /// Builds the Animator state name for this door's color and the given suffix (e.g. "Red" + "Opening" = "RedDoorOpening"), or null if <see cref="doorColor"/> doesn't match a known color.
    /// </summary>
    private string ColoredStateName(string stateSuffix)
    {
        if (string.Equals(doorColor, "red", System.StringComparison.OrdinalIgnoreCase)) return "RedDoor" + stateSuffix;
        if (string.Equals(doorColor, "blue", System.StringComparison.OrdinalIgnoreCase)) return "BlueDoor" + stateSuffix;
        if (string.Equals(doorColor, "yellow", System.StringComparison.OrdinalIgnoreCase)) return "YellowDoor" + stateSuffix;
        if (string.Equals(doorColor, "gray", System.StringComparison.OrdinalIgnoreCase)) return "GrayDoor" + stateSuffix;

        return null;
    }
}