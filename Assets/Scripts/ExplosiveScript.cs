using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosiveScript : MonoBehaviour
{
    public int phase = 0;
    private bool destroyed = false;
    private Animator anim;
    private AudioSource audioSrc;
    [SerializeField] private AudioClip sndExplosion1;
    [SerializeField] private AudioClip sndExplosion2;
    private int alarm1 = 0;
    [SerializeField] private bool createAnotherTrigger = false;
    [SerializeField] private GameObject nextTrigger;

    void Start()
    {
        anim = GetComponent<Animator>();
        audioSrc = GetComponent<AudioSource>();

        if (createAnotherTrigger)
        {
            nextTrigger.SetActive(false);
        }
    }

    void Update()
    {
        if (phase == 0) { anim.Play("ExplosiveActive"); }

        if (phase == 1 && !destroyed)
        {
            alarm1 = 10;
            destroyed = true;
        }

        if (alarm1 > 0)
            alarm1 -= 1;
        else
        {
            if (phase == 1)
            {
                AudioClip[] clips = { sndExplosion1, sndExplosion2 };
                audioSrc.PlayOneShot(clips[Random.Range(0, clips.Length)]);

                if (createAnotherTrigger)
                {
                    nextTrigger.SetActive(true);
                    createAnotherTrigger = false;
                }   

                phase = 2;
            }
        }

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

        if (phase == 2) { anim.Play("ExplosiveInactive"); }
    }
}