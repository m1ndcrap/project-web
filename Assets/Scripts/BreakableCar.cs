using UnityEngine;

public class BreakableCar : MonoBehaviour
{
    private AudioSource audioSrc;
    void Start()
    {
        audioSrc = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("CarBreak"))
        {
            if (GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
            {
                GetComponent<Animator>().Play("CarBroken");
                audioSrc.Play();
            }
        }
    }
}
