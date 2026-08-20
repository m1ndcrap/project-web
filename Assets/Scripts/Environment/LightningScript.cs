using UnityEngine;


/// <summary>
/// An electrical hazard that stays active (and dangerous to touch) until its power source. An
/// optional <see cref="BreakableSwitch"/>-tagged "Switch" or "Generator" object, is broken, at
/// which point it plays a breaking animation and goes permanently inactive.
/// </summary>


public class LightningScript : MonoBehaviour
{
    [SerializeField] GameObject trigger;
    public int phase = 0;
    private bool destroyed = false;
    private int alarm1 = 0;
    public AudioSource audioSrc;
    [SerializeField] private AudioClip sndElectric;
    private Animator anim;
    private BreakableSwitch powerSource;




    void Start()
    {
        audioSrc = GetComponent<AudioSource>();
        anim = GetComponent<Animator>();

        if (trigger != null && (trigger.CompareTag("Switch") || trigger.CompareTag("Generator")))
        {
            powerSource = trigger.GetComponent<BreakableSwitch>();
        }
    }




    void Update()
    {
        if (phase == 0)
        {
            anim.Play("LightningActive");

            if (!audioSrc.isPlaying)
                audioSrc.PlayOneShot(sndElectric);
        }

        if (alarm1 > 0)
        {
            alarm1 -= 1;
        }
        else
        {
            if (phase == 1)
            {
                anim.Play("LightningTurnOff");
                audioSrc.Stop();
                phase = 2;
            }
        }

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        if (phase == 2 && stateInfo.IsName("LightningTurnOff") && stateInfo.normalizedTime >= 1f) { phase = 3; }

        if (powerSource != null && powerSource.phase == 2)
        {
            phase = 1;
        }

        if (phase == 1 && !destroyed)
        {
            alarm1 = 10;
            destroyed = true;
        }

        if (phase == 3) { anim.Play("LightningInactive"); }
    }
}