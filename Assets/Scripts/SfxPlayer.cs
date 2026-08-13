using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SfxPlayer : MonoBehaviour
{
    [SerializeField] public AudioSource referenceSource;

    public static SfxPlayer Instance;

    void Awake()
    {
        Instance = this;
    }

    public void PlayClipAtPointMatched(AudioClip clip, Vector3 position)
    {
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
        src.volume = referenceSource.volume;
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