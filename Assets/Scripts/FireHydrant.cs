using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class FireHydrant : MonoBehaviour
{
    [SerializeField] public bool webbed = false;
    [SerializeField] public FireHydrant nearby;
    private AudioSource audioSrc;
    [SerializeField] private AudioClip sndHydrantLoop;
    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
        audioSrc = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (webbed)
        {
            anim.Play("FireHydrantWebbed");
        }
        else
        {
            if (!anim.GetCurrentAnimatorStateInfo(0).IsName("FireHydrantActive"))
            {
                anim.Play("FireHydrantActive");
            }

            if (!audioSrc.isPlaying)
                audioSrc.PlayOneShot(sndHydrantLoop);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

        if (other.CompareTag("Web"))
        {
            webbed = true;

            if (nearby != null)
            {
                nearby.webbed = false;
            }

            other.GetComponent<ShootScript>().audioSrc.PlayOneShot(other.GetComponent<ShootScript>().sndWebDestroy);
            if (!other.GetComponent<ShootScript>().anim.GetCurrentAnimatorStateInfo(0).IsName("WebDestroy")) { other.GetComponent<ShootScript>().anim.Play("WebDestroy"); }
        }
    }
}