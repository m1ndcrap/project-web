using Framework.Core;
using UnityEngine;


/// <summary>
/// Plays a sound at a world position with full 3D spatial falloff (distance-based volume,
/// rolloff curve, doppler, and so on) by spinning up a temporary AudioSource that copies its
/// settings from <see cref="referenceSource"/>, rather than requiring every object that wants a
/// positioned one-shot sound to carry its own pre-configured AudioSource around.
/// </summary>


public class SfxPlayer : Singleton<SfxPlayer>
{
    [Tooltip("A template AudioSource whose spatial/rolloff/mixer settings are copied onto every temporary AudioSource this player creates. Configure 3D sound settings here once, rather than on every individual sound-playing object.")]
    [SerializeField] private AudioSource referenceSource;




    // Each scene has its own SfxPlayer with its own reference source, so this must not survive a
    // scene load. A surviving copy would keep pointing at the destroyed scene's AudioSource.
    protected override bool ShouldPersistAcrossScenes => false;




    /// <summary>
    /// Plays a clip at a world position, copying spatial blend, rolloff, distance, and mixer
    /// routing from <see cref="referenceSource"/> so the sound falls off with distance the same
    /// way every other sound in the project does. The temporary AudioSource this creates cleans
    /// itself up automatically once the clip finishes.
    /// </summary>
    /// <param name="clip">The clip to play.</param>
    /// <param name="position">The world position to play it at.</param>
    /// <param name="volumeScale">A multiplier applied on top of the reference source's volume.</param>
    public void PlayClipAtPointMatched(AudioClip clip, Vector3 position, float volumeScale = 1f)
    {
        if (clip == null) return;


        // Bail out instead of throwing. Callers often sit in Update, so one bad reference would
        // otherwise throw every frame and could stop the caller finishing what it was doing.
        if (referenceSource == null)
        {
            Debug.LogWarning("[SfxPlayer] No reference source assigned, skipping sound. Assign one in the inspector.", this);
            return;
        }


        GameObject tempGO = new GameObject("TempAudio_" + clip.name);
        tempGO.transform.position = position;
        AudioSource src = tempGO.AddComponent<AudioSource>();
        src.clip = clip;


        // Copy everything relevant straight from the reference source
        src.spatialBlend = referenceSource.spatialBlend;
        src.rolloffMode = referenceSource.rolloffMode;
        src.minDistance = referenceSource.minDistance;
        src.maxDistance = referenceSource.maxDistance;
        src.priority = referenceSource.priority;
        src.volume = referenceSource.volume * volumeScale;
        src.pitch = referenceSource.pitch;
        src.spread = referenceSource.spread;
        src.dopplerLevel = referenceSource.dopplerLevel;
        src.outputAudioMixerGroup = referenceSource.outputAudioMixerGroup;


        if (src.rolloffMode == AudioRolloffMode.Custom)
        {
            AnimationCurve curve = referenceSource.GetCustomCurve(AudioSourceCurveType.CustomRolloff);
            src.SetCustomCurve(AudioSourceCurveType.CustomRolloff, curve);
        }


        src.Play();
        Destroy(tempGO, clip.length / src.pitch);
    }
}