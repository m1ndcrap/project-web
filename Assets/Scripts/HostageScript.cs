using System;
using UnityEngine;

public class HostageScript : MonoBehaviour
{
    private int civ = 1;
    private string civScaredAnim = "Civ1_Scared";
    private string civThanksAnim = "Civ1_Thanks1";
    [SerializeField] GameObject trigger;
    public int phase = 0;
    private bool rescued = false;
    private int alarm1 = 0;
    private int alarm2 = 300;
    private int alarm3 = 0;
    private SpriteRenderer sr;
    private bool fading = false;
    private float fadeSpeed = 1.5f;
    private AudioSource audioSrc;
    [SerializeField] private AudioClip sndHelp;
    [SerializeField] private AudioClip sndThanks1;
    [SerializeField] private AudioClip sndThanks2;
    [SerializeField] private AudioClip sndThanks3;
    [SerializeField] private AudioClip sndThanks4;
    [SerializeField] private AudioClip sndThanks5;
    [SerializeField] private AudioClip sndThanks6;
    [SerializeField] private AudioClip sndThanks7;
    [SerializeField] private AudioClip sndThanks8;
    private Animator anim;
    [SerializeField] private PlayerStep player;

    void Start()
    {
        civ = UnityEngine.Random.Range(1, 6);
        sr = GetComponent<SpriteRenderer>();
        audioSrc = GetComponent<AudioSource>();
        anim = GetComponent<Animator>();

        switch (civ)
        {
            case 1:
                {
                    civScaredAnim = "Civ1_Scared";
                    civThanksAnim = new[] { "Civ1_Thanks1", "Civ1_Thanks2" }[UnityEngine.Random.Range(0, 2)];
                }
                break;

            case 2:
                {
                    civScaredAnim = "Civ2_Scared";
                    civThanksAnim = new[] { "Civ2_Thanks", "Civ2_Thanks2" }[UnityEngine.Random.Range(0, 2)];
                }
                break;

            case 3:
                {
                    civScaredAnim = "Civ3_Scared";
                    civThanksAnim = new[] { "Civ3_Thanks1", "Civ3_Thanks2" }[UnityEngine.Random.Range(0, 2)];
                }
                break;

            case 4:
                {
                    civScaredAnim = "Civ4_Scared";
                    civThanksAnim = new[] { "Civ4_Thanks1", "Civ4_Thanks2" }[UnityEngine.Random.Range(0, 2)];
                }
                break;

            case 5:
                {
                    civScaredAnim = "Civ5_Scared";
                    civThanksAnim = new[] { "Civ5_Thanks1", "Civ5_Thanks2" }[UnityEngine.Random.Range(0, 2)];
                }
                break;
        }
    }

    void Update()
    {
        if (phase == 0) { anim.Play(civScaredAnim); }

        if (alarm1 > 0)
        {
            alarm1 -= 1;
        }
        else
        {
            if (phase == 1)
            {
                anim.Play(civThanksAnim);
                AudioClip[] clips = { sndThanks1, sndThanks2, sndThanks3, sndThanks4, sndThanks5, sndThanks6, sndThanks7, sndThanks8 };
                SfxPlayer.Instance.PlayClipAtPointMatched(clips[UnityEngine.Random.Range(0, clips.Length)], transform.position);

                float clipLength = 1f;

                foreach (AnimationClip clip in anim.runtimeAnimatorController.animationClips)
                {
                    if (clip.name == civThanksAnim)
                    {
                        clipLength = clip.length;
                        break;
                    }
                }
                alarm3 = Mathf.CeilToInt(clipLength / Time.deltaTime);

                phase = 2;
            }
        }


        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

        if (phase == 2)
        {
            if (alarm3 > 0)
                alarm3 -= 1;
            else
                phase = 3;
        }


        if (alarm2 > 0)
        {
            alarm2 -= 1;
        }
        else
        {
            if ((Math.Abs(transform.position.x - player.transform.position.x) <= 5f) && phase == 0)
            {
                AudioClip[] clips = { sndHelp };
                int index = UnityEngine.Random.Range(0, clips.Length + 1);

                if (index < clips.Length)
                {
                    audioSrc.clip = clips[index];
                    audioSrc.Play();
                }
            }

            alarm2 = 300;
        }

        if (trigger != null)
        {
            if (trigger.CompareTag("Enemy"))
            {
                if (phase == 0)
                {
                    if (trigger.GetComponent<RobotStep>().eState == RobotStep.EnemyState.death)
                    {
                        RescueHostage();
                    }
                }
            }

            if (trigger.CompareTag("Lightning"))
            {
                if (phase == 0)
                {
                    if (trigger.GetComponent<LightningScript>().phase == 3)
                    {
                        RescueHostage();
                    }
                }
            }

            if (trigger.CompareTag("Door"))
            {
                if (phase == 0)
                {
                    if (trigger.GetComponent<BreakableDoor>().phase == 2)
                    {
                        RescueHostage();
                    }
                }
            }
        }

        if (phase == 3 && !fading)
        {
            fading = true;
        }

        if (fading)
        {
            Color c = sr.color;
            c.a -= Time.deltaTime / fadeSpeed;
            sr.color = c;

            if (c.a <= 0f)
            {
                c.a = 0f;
                sr.color = c;
                fading = false;
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (trigger.CompareTag("Player"))
        {
            if (collision.gameObject.CompareTag("Player") && phase == 0)
            {
                RescueHostage();
            }
        }
    }

    private void RescueHostage()
    {
        if (rescued) return;

        rescued = true;
        audioSrc.Stop();
        alarm1 = 10;
        phase = 1;
    }
}