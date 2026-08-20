using UnityEngine;


/// <summary>
/// Drives adaptive background music by crossfading between three pre-mixed intensity layers
/// (light, medium, intense) rather than switching tracks abruptly. Set <see cref="intensity"/> from
/// gameplay code (e.g. when the player enters combat) and this component smoothly fades the
/// corresponding layer up while fading the others down.
/// </summary>


public class BGMController : MonoBehaviour
{
    [Header("Music Layers")]
    [Tooltip("The calm/exploration music layer.")]
    [SerializeField] public AudioSource lightSource;
    [Tooltip("The moderate-tension music layer.")]
    [SerializeField] public AudioSource mediumSource;
    [Tooltip("The high-intensity/combat music layer.")]
    [SerializeField] public AudioSource intenseSource;


    [Tooltip("Which layer should currently be fading up: 0 = light, 1 = medium, 2 = intense. Set this from gameplay code to change the music's intensity.")]
    public int intensity = 0;     // 0 = light, 1 = medium, 2 = intense


    private float lightVol = 1f;
    private float mediumVol = 0f;
    private float intenseVol = 0f;


    [Header("Layer Volume Caps")]
    [Tooltip("The maximum volume the light layer fades up to.")]
    [SerializeField] private float lightVolMax = 0.6f;
    [Tooltip("The maximum volume the medium layer fades up to.")]
    [SerializeField] private float mediumVolMax = 0.65f;
    [Tooltip("The maximum volume the intense layer fades up to.")]
    [SerializeField] private float intenseVolMax = 0.45f;




    private void Start()
    {
        // Ensure looping and start all tracks.
        lightSource.loop = true;
        mediumSource.loop = true;
        intenseSource.loop = true;

        lightSource.volume = lightVol;
        mediumSource.volume = mediumVol;
        intenseSource.volume = intenseVol;

        lightSource.Play();
        mediumSource.Play();
        intenseSource.Play();
    }




    private void Update()
    {
        // If any stopped, play them again
        if (!intenseSource.isPlaying)
        {
            lightSource.Play();
            mediumSource.Play();
            intenseSource.Play();
        }


        switch (intensity)
        {
            case 0:
                {
                    // Light fades up, others fade down
                    lightVol = Mathf.MoveTowards(lightVol, lightVolMax, lightVolMax / 10f * Time.deltaTime * 60f);
                    mediumVol = Mathf.MoveTowards(mediumVol, 0f, mediumVolMax / 10f * Time.deltaTime * 60f);
                    intenseVol = Mathf.MoveTowards(intenseVol, 0f, 0.045f * Time.deltaTime * 60f);
                }
                break;


            case 1:
                {
                    // Medium fades up
                    lightVol = Mathf.MoveTowards(lightVol, 0f, lightVolMax / 10f * Time.deltaTime * 60f);
                    mediumVol = Mathf.MoveTowards(mediumVol, mediumVolMax, mediumVolMax / 10f * Time.deltaTime * 60f);
                    intenseVol = Mathf.MoveTowards(intenseVol, 0f, 0.045f * Time.deltaTime * 60f);
                }
                break;


            case 2:
                {
                    // Intense fades up
                    lightVol = Mathf.MoveTowards(lightVol, 0f, lightVolMax / 10f * Time.deltaTime * 60f);
                    mediumVol = Mathf.MoveTowards(mediumVol, 0f, mediumVolMax / 10f * Time.deltaTime * 60f);
                    intenseVol = Mathf.MoveTowards(intenseVol, intenseVolMax, 0.045f * Time.deltaTime * 60f);
                }
                break;
        }


        // Apply volume changes
        lightSource.volume = lightVol;
        mediumSource.volume = mediumVol;
        intenseSource.volume = intenseVol;
    }
}