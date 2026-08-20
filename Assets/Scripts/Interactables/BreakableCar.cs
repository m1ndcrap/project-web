using UnityEngine;


/// <summary>
/// Watches for this car's Animator finishing its "CarBreak" clip, then transitions it into a
/// permanent "CarBroken" pose and plays the crash sound.
/// </summary>


public class BreakableCar : MonoBehaviour
{
    private Animator anim;
    private AudioSource audioSrc;




    private void Start()
    {
        anim = GetComponent<Animator>();
        audioSrc = GetComponent<AudioSource>();
    }




    private void Update()
    {
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.IsName("CarBreak") && stateInfo.normalizedTime >= 1f)
        {
            anim.Play("CarBroken");
            audioSrc.Play();
        }
    }
}