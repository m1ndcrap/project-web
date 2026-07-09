using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreakableDoor : MonoBehaviour
{
    public int phase = 0;
    private bool destroyed = false;
    private Animator anim;
    [SerializeField] private AudioClip sndBreak;
    [SerializeField] private GameObject doorEmpty;
    [SerializeField] private GameObject doorWall;
    [SerializeField] private string normalAnim;
    [SerializeField] private string breakAnim;
    private int alarm1 = 0;

    void Start()
    {
        anim = GetComponent<Animator>();
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
                AudioSource.PlayClipAtPoint(sndBreak, transform.position, 1f);
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