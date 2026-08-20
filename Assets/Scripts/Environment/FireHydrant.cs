using UnityEngine;


/// <summary>
/// For a fire hydrant object in the game world that can be affected by webbing and interact with nearby
/// hydrants.
/// </summary>


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

            if (!audioSrc.isPlaying) { audioSrc.PlayOneShot(sndHydrantLoop); }
        }
    }




    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Web") && !webbed)
        {
            webbed = true;

            if (nearby != null)
            {
                nearby.webbed = false;
            }

            ShootScript shot = other.GetComponent<ShootScript>();

            if (!shot.audioSrc.isPlaying) { shot.audioSrc.PlayOneShot(shot.sndWebDestroy); }
            if (!shot.anim.GetCurrentAnimatorStateInfo(0).IsName("WebDestroy")) { shot.anim.Play("WebDestroy"); }
        }
    }
}